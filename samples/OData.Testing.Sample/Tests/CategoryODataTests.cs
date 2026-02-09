using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using OData.Testing.Sample.Data;
using OData.Testing.Sample.Models;
using Xunit;

namespace OData.Testing.Sample.Tests;

/// <summary>
/// OData query tests for the Categories endpoint.
/// Demonstrates testing self-referencing entities (ParentCategory/ChildCategories).
/// </summary>
public class CategoryODataTests : ODataEndpointTests<SampleDbContext>
{
    private static readonly ODataQueryGenerator Generator =
        ODataQueryGenerator.ForEntity<Category>("Categories");

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

    // ── All filter tests for Categories ──

    [Theory]
    [MemberData(nameof(FilterCases))]
    public Task Filters(QueryTestCase tc) => RunTestAsync(tc);

    // ── Expand tests (including self-referencing) ──

    [Theory]
    [MemberData(nameof(ExpandCases))]
    public Task Expands(QueryTestCase tc) => RunTestAsync(tc);

    // ── Select tests ──

    [Theory]
    [MemberData(nameof(SelectCases))]
    public Task Selects(QueryTestCase tc) => RunTestAsync(tc);

    // ── OrderBy tests ──

    [Theory]
    [MemberData(nameof(OrderByCases))]
    public Task OrderBys(QueryTestCase tc) => RunTestAsync(tc);

    // ── Combination tests ──

    [Theory]
    [MemberData(nameof(CombinationCases))]
    public Task Combinations(QueryTestCase tc) => RunTestAsync(tc);

    // ── MemberData Sources ──

    public static IEnumerable<object[]> FilterCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAllFilters());

    public static IEnumerable<object[]> ExpandCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAllExpands());

    public static IEnumerable<object[]> SelectCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAllSelects());

    public static IEnumerable<object[]> OrderByCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAllOrderBys());

    public static IEnumerable<object[]> CombinationCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAllCombinations());
}
