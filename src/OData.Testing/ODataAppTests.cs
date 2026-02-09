using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OData.Testing.Assertions;
using OData.Testing.Infrastructure;
using Xunit;

namespace OData.Testing;

/// <summary>
/// Base class for OData integration tests using your real application Startup configuration.
/// Provide your Startup class as TStartup — the test server will use your actual controllers,
/// middleware pipeline, and OData route configuration.
///
/// <para>
/// This is ideal for testing your real OData endpoints with multiple route components,
/// custom controllers, and complex configurations.
/// </para>
///
/// <para>Usage:</para>
/// <code>
/// public class MyODataTests : ODataAppTests&lt;MyStartup&gt;
/// {
///     protected override void ConfigureWebHost(IWebHostBuilder builder)
///     {
///         builder.ConfigureServices(services =&gt;
///         {
///             // Replace real DB with in-memory for testing
///             services.AddDbContext&lt;MyDbContext&gt;(opt =&gt; opt.UseInMemoryDatabase("Test"));
///         });
///     }
///
///     protected override void SeedData(IServiceProvider services)
///     {
///         using var scope = services.CreateScope();
///         var db = scope.ServiceProvider.GetRequiredService&lt;MyDbContext&gt;();
///         db.Database.EnsureCreated();
///         // ... seed test data ...
///         db.SaveChanges();
///     }
///
///     [Theory]
///     [MemberData(nameof(AllCases))]
///     public Task AllQueries(QueryTestCase tc) =&gt; RunTestAsync(tc);
///
///     public static IEnumerable&lt;object[]&gt; AllCases =&gt;
///         ODataQueryGenerator.AsTheoryData(
///             ODataQueryGenerator.ForEntity&lt;Product&gt;("Products", "v1").GenerateAll()
///                 .Concat(ODataQueryGenerator.ForEntity&lt;Order&gt;("Orders", "v1/task").GenerateAll()));
/// }
/// </code>
/// </summary>
/// <typeparam name="TStartup">Your Startup class (must have ConfigureServices and Configure methods).</typeparam>
public abstract class ODataAppTests<TStartup> : IAsyncLifetime
    where TStartup : class
{
    private TestServer? _server;
    private HttpClient? _client;
    private ClientEvaluationInterceptor? _clientEvalInterceptor;

    /// <summary>
    /// The test server's HttpClient. Available after InitializeAsync.
    /// </summary>
    protected HttpClient Client => _client!;

    /// <summary>
    /// The test server's service provider. Available after InitializeAsync.
    /// </summary>
    protected IServiceProvider Services => _server!.Services;

    /// <summary>
    /// Whether to validate that no client-side evaluation occurs. Defaults to true.
    /// </summary>
    protected virtual bool ValidateNoClientEvaluation => true;

    /// <summary>
    /// Configure the web host builder before the server starts.
    /// Override to customize services (e.g., replace database providers for testing),
    /// add test-specific middleware, etc.
    /// </summary>
    protected virtual void ConfigureWebHost(IWebHostBuilder builder) { }

    /// <summary>
    /// Seed the database with test data. Called once during initialization.
    /// Use the service provider to create a scope and resolve your DbContext.
    /// </summary>
    protected virtual void SeedData(IServiceProvider services) { }

    // ── Lifecycle ──

    public virtual Task InitializeAsync()
    {
        _clientEvalInterceptor = new ClientEvaluationInterceptor();

        var builder = new WebHostBuilder()
            .UseStartup<TStartup>()
            .ConfigureServices(services =>
            {
                // Add client evaluation tracking (runs before Startup.ConfigureServices)
                services.AddSingleton(_clientEvalInterceptor);
                services.AddSingleton<ILoggerProvider>(
                    new ClientEvaluationLoggerProvider(_clientEvalInterceptor));
            });

        // Let the test class customize the web host (e.g., replace DB)
        ConfigureWebHost(builder);

        _server = new TestServer(builder);
        _client = _server.CreateClient();

        // Seed data
        SeedData(_server.Services);

        return Task.CompletedTask;
    }

    public virtual Task DisposeAsync()
    {
        _client?.Dispose();
        _server?.Dispose();
        return Task.CompletedTask;
    }

    // ── Test Execution ──

    /// <summary>
    /// Execute a single OData query test case and validate the response.
    /// This is the primary method to call from [Theory] test methods.
    /// Uses the test case's RoutePrefix to build the request URL.
    /// </summary>
    protected async Task RunTestAsync(QueryTestCase testCase)
    {
        _clientEvalInterceptor?.Reset();

        var prefix = testCase.RoutePrefix?.TrimStart('/').TrimEnd('/');
        var url = string.IsNullOrEmpty(prefix)
            ? $"/{testCase.EntitySet}?{testCase.QueryString}"
            : $"/{prefix}/{testCase.EntitySet}?{testCase.QueryString}";

        var response = await Client.GetAsync(url);

        await ODataAssert.ValidateResponseAsync(
            response,
            testCase,
            ValidateNoClientEvaluation ? _clientEvalInterceptor : null);
    }

    /// <summary>
    /// Execute a raw OData query string against an entity set and return the response.
    /// Use this for custom assertions beyond what RunTestAsync provides.
    /// </summary>
    protected async Task<HttpResponseMessage> QueryAsync(string entitySet, string queryString, string? routePrefix = null)
    {
        var prefix = routePrefix?.TrimStart('/').TrimEnd('/');
        var url = string.IsNullOrEmpty(prefix)
            ? $"/{entitySet}?{queryString}"
            : $"/{prefix}/{entitySet}?{queryString}";
        return await Client.GetAsync(url);
    }

    /// <summary>
    /// Execute a raw OData query string and assert it succeeds (2xx).
    /// </summary>
    protected async Task AssertQuerySucceeds(string entitySet, string queryString, string? routePrefix = null)
    {
        var response = await QueryAsync(entitySet, queryString, routePrefix);
        var testCase = new QueryTestCase(entitySet, "Custom", "Custom", queryString, queryString)
        {
            RoutePrefix = routePrefix ?? string.Empty
        };
        ODataAssert.AssertSuccessStatusCode(response, testCase);
    }
}
