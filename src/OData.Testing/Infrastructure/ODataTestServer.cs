using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;

namespace OData.Testing.Infrastructure;

/// <summary>
/// Configuration options for <see cref="ODataTestServer{TContext}"/>.
/// </summary>
public class ODataTestServerOptions<TContext> where TContext : DbContext
{
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }
    public Func<IEdmModel>? BuildEdmModel { get; set; }
    public Action<TContext>? SeedData { get; set; }
    public string RoutePrefix { get; set; } = "odata";
    public int MaxTop { get; set; } = 1000;
    public int MaxExpansionDepth { get; set; } = 5;
    public Action<IServiceCollection>? ConfigureServices { get; set; }
    public Dictionary<Type, string>? EntitySetMappings { get; set; }
}

/// <summary>
/// Static configuration holder passed to the Startup class.
/// Scoped by DbContext type to avoid conflicts between parallel test fixtures.
/// </summary>
internal static class ODataTestServerConfig<TContext> where TContext : DbContext
{
    internal static ODataTestServerOptions<TContext>? Options { get; set; }
    internal static IEdmModel? EdmModel { get; set; }
    internal static Dictionary<Type, string> EntitySetMappings { get; set; } = new();
}

/// <summary>
/// Startup class used by the ODataTestServer. Using a proper Startup class
/// ensures correct ASP.NET Core initialization order for services and middleware.
/// </summary>
internal class ODataTestStartup<TContext> where TContext : DbContext
{
    public void ConfigureServices(IServiceCollection services)
    {
        var options = ODataTestServerConfig<TContext>.Options!;
        var edmModel = ODataTestServerConfig<TContext>.EdmModel!;
        var entitySetMappings = ODataTestServerConfig<TContext>.EntitySetMappings;

        // Register DbContext
        var dbName = $"ODataTest_{Guid.NewGuid():N}";
        services.AddDbContext<TContext>(opt =>
        {
            if (options.ConfigureDbContext != null)
                options.ConfigureDbContext(opt);
            else
                opt.UseInMemoryDatabase(dbName);
        });

        // Register as base DbContext for generic controllers
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());

        // Client evaluation interceptor
        services.AddSingleton<ClientEvaluationInterceptor>();

        // MVC + OData setup
        var mvcBuilder = services.AddControllers();

        // CRITICAL: Add the library assembly so MVC can discover our generic controller type
        mvcBuilder.AddApplicationPart(typeof(TestODataController<>).Assembly);

        // Add dynamic controller feature provider to register closed generic controllers
        mvcBuilder.ConfigureApplicationPartManager(apm =>
        {
            apm.FeatureProviders.Add(
                new DynamicControllerFeatureProvider(entitySetMappings.Keys.ToList()));
        });

        // Add OData services and route components
        mvcBuilder.AddOData(opt =>
        {
            opt.AddRouteComponents(options.RoutePrefix, edmModel);
            opt.Select().Filter().Expand().OrderBy().Count().SetMaxTop(options.MaxTop);
        });

        // CRITICAL FIX: Use IApplicationModelProvider (Order=-200) instead of IControllerModelConvention.
        // OData 8's ODataRoutingApplicationModelProvider runs at Order=-100 and matches controller names
        // to entity set names. IControllerModelConvention runs AFTER all providers, so by the time it
        // renames "TestODataController" to "Products", OData has already processed and rejected it.
        // By using a provider at Order=-200, we rename BEFORE OData processes the controllers.
        services.AddSingleton<IApplicationModelProvider>(
            new GenericControllerRenamingProvider(entitySetMappings));

        options.ConfigureServices?.Invoke(services);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}

/// <summary>
/// Creates a self-contained ASP.NET Core test server with OData middleware,
/// in-memory database, and dynamically generated controllers for each entity set.
/// </summary>
public class ODataTestServer<TContext> : IAsyncDisposable, IDisposable
    where TContext : DbContext
{
    private readonly TestServer _server;
    private readonly HttpClient _client;
    private readonly ODataTestServerOptions<TContext> _options;

    public HttpClient Client => _client;
    public IServiceProvider Services => _server.Services;
    public string RoutePrefix => _options.RoutePrefix;

    public ODataTestServer(Action<ODataTestServerOptions<TContext>> configure)
    {
        _options = new ODataTestServerOptions<TContext>();
        configure(_options);

        if (_options.BuildEdmModel == null)
            throw new InvalidOperationException("BuildEdmModel is required.");

        var edmModel = _options.BuildEdmModel();
        var entitySetMappings = _options.EntitySetMappings ?? ExtractEntitySetMappings(edmModel);

        // Set static config for the Startup class
        ODataTestServerConfig<TContext>.Options = _options;
        ODataTestServerConfig<TContext>.EdmModel = edmModel;
        ODataTestServerConfig<TContext>.EntitySetMappings = entitySetMappings;

        var webHostBuilder = new WebHostBuilder()
            .UseStartup<ODataTestStartup<TContext>>();

        _server = new TestServer(webHostBuilder);

        // Seed data
        if (_options.SeedData != null)
        {
            using var scope = _server.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();
            context.Database.EnsureCreated();
            _options.SeedData(context);
        }

        _client = _server.CreateClient();
    }

    private static Dictionary<Type, string> ExtractEntitySetMappings(IEdmModel model)
    {
        var mappings = new Dictionary<Type, string>();
        var container = model.EntityContainer;
        if (container == null) return mappings;

        foreach (var entitySet in container.EntitySets())
        {
            var entityTypeName = entitySet.EntityType().FullTypeName();
            var clrType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .FirstOrDefault(t => t.FullName == entityTypeName || t.Name == entitySet.EntityType().Name);

            if (clrType != null)
                mappings[clrType] = entitySet.Name;
        }

        return mappings;
    }

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        _server.Dispose();
        await Task.CompletedTask;
    }
}

/// <summary>
/// Generic OData controller that serves any entity set using DbContext.Set&lt;TEntity&gt;().
/// Dynamically registered for each entity set in the EDM model.
/// OData convention routing handles HTTP method dispatch — no [HttpGet] needed.
/// </summary>
public class TestODataController<TEntity> : ODataController where TEntity : class
{
    private readonly DbContext _db;

    public TestODataController(DbContext db)
    {
        _db = db;
    }

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000, MaxAnyAllExpressionDepth = 5, MaxNodeCount = 200)]
    public IActionResult Get()
    {
        return Ok(_db.Set<TEntity>());
    }

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000)]
    public IActionResult Get(int key)
    {
        var entity = _db.Set<TEntity>().Find(key);
        return entity == null ? NotFound() : Ok(entity);
    }
}

/// <summary>
/// Renames closed-generic TestODataController types to match their OData entity set names
/// BEFORE OData's routing provider processes them.
///
/// OData 8's ODataRoutingApplicationModelProvider runs at Order=-100 and matches controllers
/// by name to entity sets. By running at Order=-200, this provider renames generic controllers
/// (e.g., "TestODataController`1" → "Products") so OData can correctly recognize them.
/// </summary>
internal class GenericControllerRenamingProvider : IApplicationModelProvider
{
    private readonly Dictionary<Type, string> _entitySetMappings;

    public GenericControllerRenamingProvider(Dictionary<Type, string> entitySetMappings)
    {
        _entitySetMappings = entitySetMappings;
    }

    /// <summary>
    /// Run before OData's ODataRoutingApplicationModelProvider (Order=-100).
    /// </summary>
    public int Order => -200;

    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        foreach (var controller in context.Result.Controllers)
        {
            if (!controller.ControllerType.IsGenericType) continue;
            if (controller.ControllerType.GetGenericTypeDefinition() != typeof(TestODataController<>)) continue;

            var entityType = controller.ControllerType.GenericTypeArguments[0];
            if (_entitySetMappings.TryGetValue(entityType, out var entitySetName))
            {
                controller.ControllerName = entitySetName;
            }
        }
    }

    public void OnProvidersExecuted(ApplicationModelProviderContext context) { }
}

/// <summary>
/// Dynamically adds closed generic TestODataController types for each entity set.
/// </summary>
internal class DynamicControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly IReadOnlyList<Type> _entityTypes;

    public DynamicControllerFeatureProvider(IReadOnlyList<Type> entityTypes)
    {
        _entityTypes = entityTypes;
    }

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var entityType in _entityTypes)
        {
            var controllerType = typeof(TestODataController<>)
                .MakeGenericType(entityType)
                .GetTypeInfo();

            if (!feature.Controllers.Contains(controllerType))
                feature.Controllers.Add(controllerType);
        }
    }
}
