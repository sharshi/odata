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
using Microsoft.Extensions.Hosting;
using Microsoft.OData.Edm;

namespace OData.Testing.Infrastructure;

/// <summary>
/// Configuration options for <see cref="ODataTestServer{TContext}"/>.
/// </summary>
public class ODataTestServerOptions<TContext> where TContext : DbContext
{
    /// <summary>
    /// Configures the DbContext options (e.g., UseInMemoryDatabase).
    /// Defaults to an in-memory database with a random name.
    /// </summary>
    public Action<DbContextOptionsBuilder>? ConfigureDbContext { get; set; }

    /// <summary>
    /// Builds the OData EDM model. Required.
    /// </summary>
    public Func<IEdmModel>? BuildEdmModel { get; set; }

    /// <summary>
    /// Seeds the database with test data. Called after the server starts.
    /// </summary>
    public Action<TContext>? SeedData { get; set; }

    /// <summary>
    /// The OData route prefix. Defaults to "odata".
    /// </summary>
    public string RoutePrefix { get; set; } = "odata";

    /// <summary>
    /// Maximum $top value. Defaults to 1000.
    /// </summary>
    public int MaxTop { get; set; } = 1000;

    /// <summary>
    /// Maximum $expand depth. Defaults to 5.
    /// </summary>
    public int MaxExpansionDepth { get; set; } = 5;

    /// <summary>
    /// Additional service configuration callback.
    /// </summary>
    public Action<IServiceCollection>? ConfigureServices { get; set; }

    /// <summary>
    /// Entity type to entity set name mappings for dynamic controller generation.
    /// If not provided, extracted from the EDM model.
    /// </summary>
    public Dictionary<Type, string>? EntitySetMappings { get; set; }
}

/// <summary>
/// Creates a self-contained ASP.NET Core test server with OData middleware,
/// in-memory database, and dynamically generated controllers for each entity set.
/// </summary>
public class ODataTestServer<TContext> : IAsyncDisposable, IDisposable
    where TContext : DbContext
{
    private readonly IHost _host;
    private readonly HttpClient _client;
    private readonly ODataTestServerOptions<TContext> _options;

    public HttpClient Client => _client;
    public IServiceProvider Services => _host.Services;
    public string RoutePrefix => _options.RoutePrefix;

    public ODataTestServer(Action<ODataTestServerOptions<TContext>> configure)
    {
        _options = new ODataTestServerOptions<TContext>();
        configure(_options);

        if (_options.BuildEdmModel == null)
            throw new InvalidOperationException("BuildEdmModel is required.");

        var edmModel = _options.BuildEdmModel();

        // Extract entity set mappings from EDM model if not provided
        var entitySetMappings = _options.EntitySetMappings ?? ExtractEntitySetMappings(edmModel);

        // Store mappings for the controller naming convention
        DynamicControllerState<TContext>.EntitySetMappings = entitySetMappings;

        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    // Register DbContext
                    var dbName = $"ODataTest_{Guid.NewGuid():N}";
                    services.AddDbContext<TContext>(opt =>
                    {
                        if (_options.ConfigureDbContext != null)
                            _options.ConfigureDbContext(opt);
                        else
                            opt.UseInMemoryDatabase(dbName);
                    });

                    // Register DbContext as DbContext too (for generic controllers)
                    services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());

                    // Register client evaluation interceptor
                    services.AddSingleton<ClientEvaluationInterceptor>();

                    // Configure MVC + OData
                    var mvcBuilder = services.AddControllers();

                    // Add the assembly containing the generic controller
                    var testingAssembly = typeof(ODataTestServer<>).Assembly;
                    mvcBuilder.AddApplicationPart(testingAssembly);

                    // Add dynamic controller feature provider
                    mvcBuilder.ConfigureApplicationPartManager(apm =>
                    {
                        apm.FeatureProviders.Add(
                            new DynamicControllerFeatureProvider(entitySetMappings.Keys.ToList()));
                    });

                    mvcBuilder.AddOData(opt =>
                    {
                        opt.AddRouteComponents(_options.RoutePrefix, edmModel);
                        opt.Select()
                           .Filter()
                           .Expand()
                           .OrderBy()
                           .Count()
                           .SetMaxTop(_options.MaxTop);
                    });

                    // Add the controller naming convention
                    services.Configure<MvcOptions>(mvcOpt =>
                    {
                        mvcOpt.Conventions.Add(
                            new GenericControllerModelConvention<TContext>());
                    });

                    _options.ConfigureServices?.Invoke(services);
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                    });
                });
            });

        _host = builder.Build();
        _host.Start();

        // Seed data
        if (_options.SeedData != null)
        {
            using var scope = _host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TContext>();
            context.Database.EnsureCreated();
            _options.SeedData(context);
        }

        _client = _host.GetTestClient();
    }

    private static Dictionary<Type, string> ExtractEntitySetMappings(IEdmModel model)
    {
        var mappings = new Dictionary<Type, string>();
        var container = model.EntityContainer;
        if (container == null) return mappings;

        foreach (var entitySet in container.EntitySets())
        {
            var entityTypeName = entitySet.EntityType().FullTypeName();
            // Try to resolve the CLR type from loaded assemblies
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
        _host.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }
}

/// <summary>
/// Static state holder for dynamic controller configuration, scoped by DbContext type.
/// </summary>
internal static class DynamicControllerState<TContext> where TContext : DbContext
{
    internal static Dictionary<Type, string> EntitySetMappings { get; set; } = new();
}

/// <summary>
/// Generic OData controller that serves any entity set using DbContext.Set&lt;TEntity&gt;().
/// Dynamically registered for each entity set in the EDM model.
/// No explicit [HttpGet] — OData convention routing handles method dispatch.
/// </summary>
[GenericControllerNameConvention]
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
/// Marker attribute for the generic controller naming convention.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal class GenericControllerNameConventionAttribute : Attribute;

/// <summary>
/// MVC convention that renames generic controllers to match OData entity set names.
/// </summary>
internal class GenericControllerModelConvention<TContext> : IControllerModelConvention
    where TContext : DbContext
{
    public void Apply(ControllerModel controller)
    {
        if (!controller.ControllerType.IsGenericType) return;
        if (controller.ControllerType.GetGenericTypeDefinition() != typeof(TestODataController<>)) return;

        var entityType = controller.ControllerType.GenericTypeArguments[0];
        if (DynamicControllerState<TContext>.EntitySetMappings.TryGetValue(entityType, out var entitySetName))
        {
            controller.ControllerName = entitySetName;
        }
    }
}

/// <summary>
/// Dynamically adds closed generic controller types for each entity set.
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
