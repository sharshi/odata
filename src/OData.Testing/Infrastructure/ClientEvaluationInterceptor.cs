using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace OData.Testing.Infrastructure;

/// <summary>
/// Tracks whether Entity Framework Core has fallen back to client-side evaluation
/// during a query. Client-side evaluation means the OData query could not be fully
/// translated to SQL, which is a performance concern and potential correctness issue.
/// </summary>
public class ClientEvaluationInterceptor
{
    private readonly object _lock = new();
    private readonly List<ClientEvaluationEvent> _events = new();

    /// <summary>
    /// Whether any client-side evaluation has occurred.
    /// </summary>
    public bool HasClientEvaluation
    {
        get { lock (_lock) { return _events.Count > 0; } }
    }

    /// <summary>
    /// All recorded client-side evaluation events.
    /// </summary>
    public IReadOnlyList<ClientEvaluationEvent> Events
    {
        get { lock (_lock) { return _events.ToList(); } }
    }

    /// <summary>
    /// Records a client-side evaluation event.
    /// </summary>
    public void RecordEvent(string message, string? queryExpression = null)
    {
        lock (_lock)
        {
            _events.Add(new ClientEvaluationEvent
            {
                Message = message,
                QueryExpression = queryExpression,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Clears all recorded events. Call before each test.
    /// </summary>
    public void Reset()
    {
        lock (_lock) { _events.Clear(); }
    }

    /// <summary>
    /// Gets a summary of all client evaluation events for assertion messages.
    /// </summary>
    public string GetSummary()
    {
        lock (_lock)
        {
            if (_events.Count == 0) return "No client-side evaluation detected.";
            return $"Client-side evaluation detected ({_events.Count} occurrences):\n" +
                   string.Join("\n", _events.Select((e, i) => $"  [{i + 1}] {e.Message}"));
        }
    }
}

/// <summary>
/// Represents a single client-side evaluation event.
/// </summary>
public class ClientEvaluationEvent
{
    public string Message { get; set; } = string.Empty;
    public string? QueryExpression { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Logger provider that intercepts EF Core diagnostic messages to detect client-side evaluation.
/// Register this in DI to automatically capture client evaluation warnings.
/// </summary>
public class ClientEvaluationLoggerProvider : ILoggerProvider
{
    private readonly ClientEvaluationInterceptor _interceptor;

    public ClientEvaluationLoggerProvider(ClientEvaluationInterceptor interceptor)
    {
        _interceptor = interceptor;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ClientEvaluationLogger(_interceptor, categoryName);
    }

    public void Dispose() { }

    private class ClientEvaluationLogger : ILogger
    {
        private readonly ClientEvaluationInterceptor _interceptor;
        private readonly string _categoryName;

        public ClientEvaluationLogger(ClientEvaluationInterceptor interceptor, string categoryName)
        {
            _interceptor = interceptor;
            _categoryName = categoryName;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) =>
            _categoryName.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // CoreEventId.QueryExecutionPlanned = 10107 (client evaluation related)
            // RelationalEventId.QueryClientEvaluationWarning is the key one
            var message = formatter(state, exception);

            if (message.Contains("client eval", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("could not be translated", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("will be evaluated locally", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("switch to client evaluation", StringComparison.OrdinalIgnoreCase))
            {
                _interceptor.RecordEvent(message);
            }
        }
    }
}
