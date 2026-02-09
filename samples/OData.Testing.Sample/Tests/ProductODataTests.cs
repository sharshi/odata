using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using OData.Testing.Sample.Data;
using OData.Testing.Sample.Models;
using Xunit;

namespace OData.Testing.Sample.Tests;

/// <summary>
/// Comprehensive OData query tests for the Products endpoint.
/// Demonstrates how to use OData.Testing to generate and run hundreds of
/// query permutations against a real OData pipeline.
/// </summary>
public class ProductODataTests : ODataEndpointTests<SampleDbContext>
{
    private static readonly ODataQueryGenerator Generator =
        ODataQueryGenerator.ForEntity<Product>("Products");

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

    // ── $filter Tests ──

    [Theory]
    [MemberData(nameof(FilterComparisonCases))]
    public Task Filter_Comparison(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(FilterLogicalCases))]
    public Task Filter_Logical(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(FilterArithmeticCases))]
    public Task Filter_Arithmetic(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(FilterStringFunctionCases))]
    public Task Filter_StringFunctions(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(FilterDateFunctionCases))]
    public Task Filter_DateFunctions(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(FilterMathFunctionCases))]
    public Task Filter_MathFunctions(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(FilterLambdaCases))]
    public Task Filter_Lambda(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(FilterNullCases))]
    public Task Filter_Null(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(FilterInCases))]
    public Task Filter_In(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(FilterNavPropertyCases))]
    public Task Filter_NavigationProperty(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(FilterSpecialCharsCases))]
    public Task Filter_SpecialCharacters(QueryTestCase tc) => RunTestAsync(tc);

    // ── $select Tests ──

    [Theory]
    [MemberData(nameof(SelectCases))]
    public Task Select(QueryTestCase tc) => RunTestAsync(tc);

    // ── $expand Tests ──

    [Theory]
    [MemberData(nameof(ExpandSingleCases))]
    public Task Expand_Single(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(ExpandMultipleCases))]
    public Task Expand_Multiple(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(ExpandNestedFilterCases))]
    public Task Expand_NestedFilter(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(ExpandNestedSelectCases))]
    public Task Expand_NestedSelect(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(ExpandNestedOrderByCases))]
    public Task Expand_NestedOrderBy(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(ExpandMultiLevelCases))]
    public Task Expand_MultiLevel(QueryTestCase tc) => RunTestAsync(tc);

    [Theory]
    [MemberData(nameof(ExpandCombinedNestedCases))]
    public Task Expand_CombinedNested(QueryTestCase tc) => RunTestAsync(tc);

    // ── $orderby Tests ──

    [Theory]
    [MemberData(nameof(OrderByCases))]
    public Task OrderBy(QueryTestCase tc) => RunTestAsync(tc);

    // ── $top / $skip Tests ──

    [Theory]
    [MemberData(nameof(PagingCases))]
    public Task Paging(QueryTestCase tc) => RunTestAsync(tc);

    // ── $count Tests ──

    [Theory]
    [MemberData(nameof(CountCases))]
    public Task Count(QueryTestCase tc) => RunTestAsync(tc);

    // ── $apply Tests ──

    [Theory]
    [MemberData(nameof(ApplyCases))]
    public Task Apply(QueryTestCase tc) => RunTestAsync(tc);

    // ── $compute Tests ──

    [Theory]
    [MemberData(nameof(ComputeCases))]
    public Task Compute(QueryTestCase tc) => RunTestAsync(tc);

    // ── Combination Tests ──

    [Theory]
    [MemberData(nameof(CombinationCases))]
    public Task Combinations(QueryTestCase tc) => RunTestAsync(tc);

    // ── MemberData Sources ──

    public static IEnumerable<object[]> FilterComparisonCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateFilterComparison());

    public static IEnumerable<object[]> FilterLogicalCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateFilterLogical());

    public static IEnumerable<object[]> FilterArithmeticCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateFilterArithmetic());

    public static IEnumerable<object[]> FilterStringFunctionCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateFilterStringFunctions());

    public static IEnumerable<object[]> FilterDateFunctionCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateFilterDateFunctions());

    public static IEnumerable<object[]> FilterMathFunctionCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateFilterMathFunctions());

    public static IEnumerable<object[]> FilterLambdaCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateFilterLambda());

    public static IEnumerable<object[]> FilterNullCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateFilterNull());

    public static IEnumerable<object[]> FilterInCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateFilterIn());

    public static IEnumerable<object[]> FilterNavPropertyCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateFilterNavigationProperty());

    public static IEnumerable<object[]> FilterSpecialCharsCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateFilterSpecialCharacters());

    public static IEnumerable<object[]> SelectCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAllSelects());

    public static IEnumerable<object[]> ExpandSingleCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateExpandSingle());

    public static IEnumerable<object[]> ExpandMultipleCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateExpandMultiple());

    public static IEnumerable<object[]> ExpandNestedFilterCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateExpandNestedFilter());

    public static IEnumerable<object[]> ExpandNestedSelectCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateExpandNestedSelect());

    public static IEnumerable<object[]> ExpandNestedOrderByCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateExpandNestedOrderBy());

    public static IEnumerable<object[]> ExpandMultiLevelCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateExpandMultiLevel());

    public static IEnumerable<object[]> ExpandCombinedNestedCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateExpandCombinedNested());

    public static IEnumerable<object[]> OrderByCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAllOrderBys());

    public static IEnumerable<object[]> PagingCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAllPaging());

    public static IEnumerable<object[]> CountCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAllCounts());

    public static IEnumerable<object[]> ApplyCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAllApply());

    public static IEnumerable<object[]> ComputeCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAllCompute());

    public static IEnumerable<object[]> CombinationCases =>
        ODataQueryGenerator.AsTheoryData(Generator.GenerateAllCombinations());
}
