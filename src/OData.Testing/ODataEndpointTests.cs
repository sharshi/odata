using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using OData.Testing.Assertions;
using OData.Testing.Infrastructure;
using Xunit;

namespace OData.Testing;

/// <summary>
/// Base class for OData endpoint integration tests.
/// Provides a self-contained test server with in-memory database,
/// query execution, and response validation.
///
/// <para>
/// Usage:
/// <code>
/// public class ProductTests : ODataEndpointTests&lt;AppDbContext&gt;
/// {
///     protected override IEdmModel BuildEdmModel() { /* ... */ }
///     protected override void SeedData(AppDbContext context) { /* ... */ }
///
///     [Theory]
///     [MemberData(nameof(FilterCases))]
///     public Task Filters(QueryTestCase tc) => RunTestAsync(tc);
///
///     public static IEnumerable&lt;object[]&gt; FilterCases =>
///         ODataQueryGenerator.ForEntity&lt;Product&gt;("Products")
///             .GenerateAllFilters()
///             .Select(tc => new object[] { tc });
/// }
/// </code>
/// </para>
/// </summary>
/// <typeparam name="TContext">The DbContext type to use for the test server.</typeparam>
public abstract class ODataEndpointTests<TContext> : IAsyncLifetime
    where TContext : DbContext
{
    private ODataTestServer<TContext>? _server;
    private ClientEvaluationInterceptor? _clientEvalInterceptor;

    /// <summary>
    /// The test server's HttpClient. Available after InitializeAsync.
    /// </summary>
    protected HttpClient Client => _server!.Client;

    /// <summary>
    /// The test server's service provider. Available after InitializeAsync.
    /// </summary>
    protected IServiceProvider Services => _server!.Services;

    /// <summary>
    /// The OData route prefix. Defaults to "odata".
    /// Override to change.
    /// </summary>
    protected virtual string RoutePrefix => "odata";

    /// <summary>
    /// Maximum $top value. Defaults to 1000.
    /// </summary>
    protected virtual int MaxTop => 1000;

    /// <summary>
    /// Maximum $expand depth. Defaults to 5.
    /// </summary>
    protected virtual int MaxExpansionDepth => 5;

    /// <summary>
    /// Whether to validate that no client-side evaluation occurs. Defaults to true.
    /// </summary>
    protected virtual bool ValidateNoClientEvaluation => true;

    // ── Abstract configuration ──

    /// <summary>
    /// Build the OData EDM model for the test server.
    /// </summary>
    protected abstract IEdmModel BuildEdmModel();

    /// <summary>
    /// Seed the database with test data.
    /// Called once during test initialization.
    /// </summary>
    protected abstract void SeedData(TContext context);

    // ── Optional overrides ──

    /// <summary>
    /// Configure the DbContext options. Defaults to in-memory database.
    /// </summary>
    protected virtual void ConfigureDbContext(DbContextOptionsBuilder options)
    {
        // Default: uses in-memory database (configured by ODataTestServer)
    }

    /// <summary>
    /// Configure additional services for the test server.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// Provide explicit entity type to entity set name mappings.
    /// If not overridden, mappings are extracted from the EDM model.
    /// </summary>
    protected virtual Dictionary<Type, string>? GetEntitySetMappings() => null;

    // ── Lifecycle ──

    public virtual Task InitializeAsync()
    {
        _clientEvalInterceptor = new ClientEvaluationInterceptor();

        _server = new ODataTestServer<TContext>(options =>
        {
            options.BuildEdmModel = BuildEdmModel;
            options.SeedData = SeedData;
            options.RoutePrefix = RoutePrefix;
            options.MaxTop = MaxTop;
            options.MaxExpansionDepth = MaxExpansionDepth;
            options.EntitySetMappings = GetEntitySetMappings();
            options.ConfigureServices = services =>
            {
                services.AddSingleton(_clientEvalInterceptor);
                ConfigureServices(services);
            };
        });

        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        if (_server != null)
            await _server.DisposeAsync();
    }

    // ── Test Execution ──

    /// <summary>
    /// Execute a single OData query test case and validate the response.
    /// This is the primary method to call from [Theory] test methods.
    /// Uses the test case's RoutePrefix if set, otherwise falls back to the class-level RoutePrefix.
    /// </summary>
    protected async Task RunTestAsync(QueryTestCase testCase)
    {
        // Reset client evaluation tracking
        _clientEvalInterceptor?.Reset();

        // Build the request URL - prefer test case RoutePrefix, fall back to class-level
        var prefix = !string.IsNullOrEmpty(testCase.RoutePrefix)
            ? testCase.RoutePrefix.TrimStart('/').TrimEnd('/')
            : RoutePrefix;
        var url = $"/{prefix}/{testCase.EntitySet}?{testCase.QueryString}";

        // Execute the query
        var response = await Client.GetAsync(url);

        // Validate the response
        await ODataAssert.ValidateResponseAsync(
            response,
            testCase,
            ValidateNoClientEvaluation ? _clientEvalInterceptor : null);
    }

    /// <summary>
    /// Execute a raw OData query string against an entity set and return the response.
    /// Use this for custom assertions beyond what RunTestAsync provides.
    /// </summary>
    protected async Task<HttpResponseMessage> QueryAsync(string entitySet, string queryString)
    {
        var url = $"/{RoutePrefix}/{entitySet}?{queryString}";
        return await Client.GetAsync(url);
    }

    /// <summary>
    /// Execute a raw OData query string and assert it succeeds (2xx).
    /// </summary>
    protected async Task AssertQuerySucceeds(string entitySet, string queryString)
    {
        var response = await QueryAsync(entitySet, queryString);
        var testCase = new QueryTestCase(entitySet, "Custom", "Custom", queryString, queryString);
        ODataAssert.AssertSuccessStatusCode(response, testCase);
    }
}
