using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using OData.Testing.Sample.Data;
using OData.Testing.Sample.Models;
using Xunit;

namespace OData.Testing.Sample.Tests;

/// <summary>
/// OData query tests for the Orders endpoint.
/// Demonstrates testing entities with enum properties, date filtering,
/// and one-to-many relationships (Order -> OrderItems).
/// </summary>
public class OrderODataTests : ODataEndpointTests<SampleDbContext>
{
    private static readonly ODataQueryGenerator Generator =
        ODataQueryGenerator.ForEntity<Order>("Orders");

    protected override IEdmModel BuildEdmModel()
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

    protected override Dictionary<Type, string>? GetEntitySetMappings() => new()
    {
        [typeof(Product)] = "Products",
        [typeof(Category)] = "Categories",
        [typeof(Supplier)] = "Suppliers",
        [typeof(Order)] = "Orders",
        [typeof(OrderItem)] = "OrderItems",
        [typeof(Tag)] = "Tags",
        [typeof(ProductTag)] = "ProductTags"
    };

    protected override void SeedData(SampleDbContext context)
    {
        SampleDataSeeder.Seed(context);
    }

    // ── All test categories ──

    [Theory]
    [MemberData(nameof(AllCases))]
    public Task AllQueries(QueryTestCase tc) => RunTestAsync(tc);

    // ── MemberData Source ──

    public static IEnumerable<object[]> AllCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAll());
}
