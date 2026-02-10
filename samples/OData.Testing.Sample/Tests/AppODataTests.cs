using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.ModelBuilder;
using OData.Testing.Sample.Data;
using OData.Testing.Sample.Models;
using Xunit;

namespace OData.Testing.Sample.Tests;

/// <summary>
/// Demonstrates ODataAppTests + ODataTestSuite — provide your real Startup,
/// your EDM model(s), and let the suite auto-discover all entity sets and
/// generate hundreds of query permutations at the density you choose.
///
/// This single test class exercises:
/// - Products, Categories, Suppliers, Orders, OrderItems, Tags, ProductTags
/// - All OData query categories ($filter, $select, $expand, $orderby, etc.)
/// - At Balanced density (~3 tests per subcategory per entity)
/// </summary>
public class AppODataTests : ODataAppTests<SampleStartup>
{
    // Build the same EDM model your Startup uses
    private static readonly Microsoft.OData.Edm.IEdmModel EdmModel = BuildEdmModel();

    private static Microsoft.OData.Edm.IEdmModel BuildEdmModel()
    {
        var builder = new ODataConventionModelBuilder();
        builder.EntitySet<Product>("Products");
        builder.EntitySet<Category>("Categories");
        builder.EntitySet<Supplier>("Suppliers");
        builder.EntitySet<Order>("Orders");
        builder.EntitySet<OrderItem>("OrderItems");
        builder.EntitySet<Tag>("Tags");
        builder.EntitySet<ProductTag>("ProductTags");
        return builder.GetEdmModel();
    }

    // Create the suite — mirrors your AddRouteComponents call(s)
    private static readonly ODataTestSuite Suite = ODataTestSuite.FromModel("odata", EdmModel);

    protected override void SeedData(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
        context.Database.EnsureCreated();
        SampleDataSeeder.Seed(context);
    }

    // ── One [Theory] to exercise everything ──

    [Theory]
    [MemberData(nameof(AllCases))]
    public Task AllQueries(QueryTestCase tc) => RunTestAsync(tc);

    // Balanced density: ~3 tests per subcategory per entity set
    public static IEnumerable<object[]> AllCases => Suite.AsTheoryData(TestDensity.Balanced);

    // ── Other density options: ──
    // Minimal (~1 per subcategory): Suite.AsTheoryData(TestDensity.Minimal)
    // Thorough (~5 per subcategory): Suite.AsTheoryData(TestDensity.Thorough)
    // Comprehensive (all permutations): Suite.AsTheoryData(TestDensity.Comprehensive)
}

/// <summary>
/// Demonstrates the GenerateTheoryData convenience method.
/// Same result with the fewest possible lines.
/// </summary>
public class AppODataTestsAlt : ODataAppTests<SampleStartup>
{
    private static readonly Microsoft.OData.Edm.IEdmModel Model;

    static AppODataTestsAlt()
    {
        var b = new ODataConventionModelBuilder();
        b.EntitySet<Product>("Products");
        b.EntitySet<Category>("Categories");
        b.EntitySet<Order>("Orders");
        Model = b.GetEdmModel();
    }

    protected override void SeedData(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
        ctx.Database.EnsureCreated();
        SampleDataSeeder.Seed(ctx);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public Task Run(QueryTestCase tc) => RunTestAsync(tc);

    // Using the convenience method — one line
    public static IEnumerable<object[]> Cases => GenerateTheoryData(
        rc => rc.Add("odata", Model),
        TestDensity.Minimal);
}

/// <summary>
/// Demonstrates multi-route-component setup — for apps like:
///   .AddRouteComponents("v1/task", taskModel)
///   .AddRouteComponents("v1/user", userModel)
///   .AddRouteComponents("v1", mainModel)
/// </summary>
public class MultiRouteODataTests : ODataAppTests<SampleStartup>
{
    // In a real app, each route component might have a different EDM model.
    // Here we use the same model to demonstrate the pattern.
    private static readonly Microsoft.OData.Edm.IEdmModel ProductModel;
    private static readonly Microsoft.OData.Edm.IEdmModel OrderModel;

    static MultiRouteODataTests()
    {
        var b1 = new ODataConventionModelBuilder();
        b1.EntitySet<Product>("Products");
        b1.EntitySet<Category>("Categories");
        ProductModel = b1.GetEdmModel();

        var b2 = new ODataConventionModelBuilder();
        b2.EntitySet<Order>("Orders");
        b2.EntitySet<OrderItem>("OrderItems");
        OrderModel = b2.GetEdmModel();
    }

    private static readonly ODataTestSuite Suite = ODataTestSuite.FromRouteComponents(rc =>
    {
        // Each rc.Add corresponds to one .AddRouteComponents in your OData setup
        rc.Add("v1/product", ProductModel);
        rc.Add("v1/order", OrderModel);
    });

    protected override void SeedData(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
        ctx.Database.EnsureCreated();
        SampleDataSeeder.Seed(ctx);
    }

    [Theory]
    [MemberData(nameof(AllCases))]
    public Task AllQueries(QueryTestCase tc) => RunTestAsync(tc);

    // Generates URLs like:
    //   /v1/product/Products?$filter=Price gt 10
    //   /v1/order/Orders?$expand=OrderItems
    public static IEnumerable<object[]> AllCases => Suite.AsTheoryData(TestDensity.Minimal);
}
