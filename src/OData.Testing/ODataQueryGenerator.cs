using OData.Testing.Generators;

namespace OData.Testing;

/// <summary>
/// Orchestrates test case generation across all query categories.
/// Entry point for consumers to generate test permutations for their entities.
/// </summary>
public class ODataQueryGenerator
{
    private readonly EntityMetadata _metadata;

    public ODataQueryGenerator(EntityMetadata metadata)
    {
        _metadata = metadata;
    }

    /// <summary>
    /// Creates a generator for the given entity type and entity set name.
    /// Extracts metadata via reflection.
    /// </summary>
    /// <param name="entitySetName">The OData entity set name (e.g., "Products").</param>
    /// <param name="routePrefix">The OData route prefix (e.g., "odata", "v1/task"). Defaults to "odata".</param>
    public static ODataQueryGenerator ForEntity<TEntity>(string entitySetName, string routePrefix = "odata") where TEntity : class
    {
        var metadata = EntityMetadataExtractor.Extract(typeof(TEntity), entitySetName);
        metadata.RoutePrefix = routePrefix;
        return new ODataQueryGenerator(metadata);
    }

    /// <summary>
    /// Creates a generator for the given entity type and entity set name.
    /// Extracts metadata via reflection.
    /// </summary>
    /// <param name="entityType">The CLR entity type.</param>
    /// <param name="entitySetName">The OData entity set name (e.g., "Products").</param>
    /// <param name="routePrefix">The OData route prefix (e.g., "odata", "v1/task"). Defaults to "odata".</param>
    public static ODataQueryGenerator ForEntity(Type entityType, string entitySetName, string routePrefix = "odata")
    {
        var metadata = EntityMetadataExtractor.Extract(entityType, entitySetName);
        metadata.RoutePrefix = routePrefix;
        return new ODataQueryGenerator(metadata);
    }

    /// <summary>
    /// Creates a generator from pre-built metadata.
    /// </summary>
    public static ODataQueryGenerator FromMetadata(EntityMetadata metadata)
    {
        return new ODataQueryGenerator(metadata);
    }

    // ── Generate All ──

    /// <summary>
    /// Generates ALL test cases across every category. Can produce hundreds of test cases.
    /// </summary>
    public IEnumerable<QueryTestCase> GenerateAll()
    {
        return GenerateAllFilters()
            .Concat(GenerateAllSelects())
            .Concat(GenerateAllExpands())
            .Concat(GenerateAllOrderBys())
            .Concat(GenerateAllPaging())
            .Concat(GenerateAllCounts())
            .Concat(GenerateAllApply())
            .Concat(GenerateAllCompute())
            .Concat(GenerateAllCombinations());
    }

    // ── Per-Category Generation ──

    public IEnumerable<QueryTestCase> GenerateAllFilters() =>
        WithRoutePrefix(FilterGenerators.GenerateAll(_metadata));

    public IEnumerable<QueryTestCase> GenerateFilterComparison() =>
        WithRoutePrefix(FilterGenerators.GenerateComparison(_metadata));

    public IEnumerable<QueryTestCase> GenerateFilterLogical() =>
        WithRoutePrefix(FilterGenerators.GenerateLogical(_metadata));

    public IEnumerable<QueryTestCase> GenerateFilterArithmetic() =>
        WithRoutePrefix(FilterGenerators.GenerateArithmetic(_metadata));

    public IEnumerable<QueryTestCase> GenerateFilterStringFunctions() =>
        WithRoutePrefix(FilterGenerators.GenerateStringFunctions(_metadata));

    public IEnumerable<QueryTestCase> GenerateFilterDateFunctions() =>
        WithRoutePrefix(FilterGenerators.GenerateDateFunctions(_metadata));

    public IEnumerable<QueryTestCase> GenerateFilterMathFunctions() =>
        WithRoutePrefix(FilterGenerators.GenerateMathFunctions(_metadata));

    public IEnumerable<QueryTestCase> GenerateFilterLambda() =>
        WithRoutePrefix(FilterGenerators.GenerateLambda(_metadata));

    public IEnumerable<QueryTestCase> GenerateFilterNull() =>
        WithRoutePrefix(FilterGenerators.GenerateNull(_metadata));

    public IEnumerable<QueryTestCase> GenerateFilterIn() =>
        WithRoutePrefix(FilterGenerators.GenerateIn(_metadata));

    public IEnumerable<QueryTestCase> GenerateFilterNavigationProperty() =>
        WithRoutePrefix(FilterGenerators.GenerateNavigationProperty(_metadata));

    public IEnumerable<QueryTestCase> GenerateFilterSpecialCharacters() =>
        WithRoutePrefix(FilterGenerators.GenerateSpecialCharacters(_metadata));

    public IEnumerable<QueryTestCase> GenerateAllSelects() =>
        WithRoutePrefix(SelectGenerator.GenerateAll(_metadata));

    public IEnumerable<QueryTestCase> GenerateAllExpands() =>
        WithRoutePrefix(ExpandGenerator.GenerateAll(_metadata));

    public IEnumerable<QueryTestCase> GenerateExpandSingle() =>
        WithRoutePrefix(ExpandGenerator.GenerateSingle(_metadata));

    public IEnumerable<QueryTestCase> GenerateExpandMultiple() =>
        WithRoutePrefix(ExpandGenerator.GenerateMultiple(_metadata));

    public IEnumerable<QueryTestCase> GenerateExpandNestedFilter() =>
        WithRoutePrefix(ExpandGenerator.GenerateNestedFilter(_metadata));

    public IEnumerable<QueryTestCase> GenerateExpandNestedSelect() =>
        WithRoutePrefix(ExpandGenerator.GenerateNestedSelect(_metadata));

    public IEnumerable<QueryTestCase> GenerateExpandNestedOrderBy() =>
        WithRoutePrefix(ExpandGenerator.GenerateNestedOrderBy(_metadata));

    public IEnumerable<QueryTestCase> GenerateExpandMultiLevel() =>
        WithRoutePrefix(ExpandGenerator.GenerateMultiLevel(_metadata));

    public IEnumerable<QueryTestCase> GenerateExpandCombinedNested() =>
        WithRoutePrefix(ExpandGenerator.GenerateCombinedNested(_metadata));

    public IEnumerable<QueryTestCase> GenerateAllOrderBys() =>
        WithRoutePrefix(OrderByGenerator.GenerateAll(_metadata));

    public IEnumerable<QueryTestCase> GenerateAllPaging() =>
        WithRoutePrefix(PagingGenerators.GenerateAll(_metadata));

    public IEnumerable<QueryTestCase> GenerateAllCounts() =>
        WithRoutePrefix(CountGenerator.GenerateAll(_metadata));

    public IEnumerable<QueryTestCase> GenerateAllApply() =>
        WithRoutePrefix(AggregationGenerators.GenerateAllApply(_metadata));

    public IEnumerable<QueryTestCase> GenerateAllCompute() =>
        WithRoutePrefix(AggregationGenerators.GenerateAllCompute(_metadata));

    public IEnumerable<QueryTestCase> GenerateAllCombinations() =>
        WithRoutePrefix(CombinationGenerator.GenerateAll(_metadata));

    // ── Utility ──

    /// <summary>
    /// Generates test cases for a specific category string.
    /// </summary>
    public IEnumerable<QueryTestCase> GenerateByCategory(string category)
    {
        return GenerateAll().Where(tc => tc.Category == category);
    }

    /// <summary>
    /// Gets a count of all test cases that would be generated.
    /// </summary>
    public int CountAll() => GenerateAll().Count();

    /// <summary>
    /// Gets counts broken down by category.
    /// </summary>
    public Dictionary<string, int> CountByCategory()
    {
        return GenerateAll()
            .GroupBy(tc => tc.Category)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Converts test cases to xUnit TheoryData format (IEnumerable&lt;object[]&gt;).
    /// </summary>
    public static IEnumerable<object[]> AsTheoryData(IEnumerable<QueryTestCase> testCases)
    {
        return testCases.Select(tc => new object[] { tc });
    }

    /// <summary>
    /// Sets the RoutePrefix on each generated test case from the entity metadata.
    /// </summary>
    private IEnumerable<QueryTestCase> WithRoutePrefix(IEnumerable<QueryTestCase> cases)
    {
        foreach (var tc in cases)
        {
            tc.RoutePrefix = _metadata.RoutePrefix;
            yield return tc;
        }
    }
}
