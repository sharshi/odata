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
    public static ODataQueryGenerator ForEntity<TEntity>(string entitySetName) where TEntity : class
    {
        var metadata = EntityMetadataExtractor.Extract(typeof(TEntity), entitySetName);
        return new ODataQueryGenerator(metadata);
    }

    /// <summary>
    /// Creates a generator for the given entity type and entity set name.
    /// Extracts metadata via reflection.
    /// </summary>
    public static ODataQueryGenerator ForEntity(Type entityType, string entitySetName)
    {
        var metadata = EntityMetadataExtractor.Extract(entityType, entitySetName);
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
        FilterGenerators.GenerateAll(_metadata);

    public IEnumerable<QueryTestCase> GenerateFilterComparison() =>
        FilterGenerators.GenerateComparison(_metadata);

    public IEnumerable<QueryTestCase> GenerateFilterLogical() =>
        FilterGenerators.GenerateLogical(_metadata);

    public IEnumerable<QueryTestCase> GenerateFilterArithmetic() =>
        FilterGenerators.GenerateArithmetic(_metadata);

    public IEnumerable<QueryTestCase> GenerateFilterStringFunctions() =>
        FilterGenerators.GenerateStringFunctions(_metadata);

    public IEnumerable<QueryTestCase> GenerateFilterDateFunctions() =>
        FilterGenerators.GenerateDateFunctions(_metadata);

    public IEnumerable<QueryTestCase> GenerateFilterMathFunctions() =>
        FilterGenerators.GenerateMathFunctions(_metadata);

    public IEnumerable<QueryTestCase> GenerateFilterLambda() =>
        FilterGenerators.GenerateLambda(_metadata);

    public IEnumerable<QueryTestCase> GenerateFilterNull() =>
        FilterGenerators.GenerateNull(_metadata);

    public IEnumerable<QueryTestCase> GenerateFilterIn() =>
        FilterGenerators.GenerateIn(_metadata);

    public IEnumerable<QueryTestCase> GenerateFilterNavigationProperty() =>
        FilterGenerators.GenerateNavigationProperty(_metadata);

    public IEnumerable<QueryTestCase> GenerateFilterSpecialCharacters() =>
        FilterGenerators.GenerateSpecialCharacters(_metadata);

    public IEnumerable<QueryTestCase> GenerateAllSelects() =>
        SelectGenerator.GenerateAll(_metadata);

    public IEnumerable<QueryTestCase> GenerateAllExpands() =>
        ExpandGenerator.GenerateAll(_metadata);

    public IEnumerable<QueryTestCase> GenerateExpandSingle() =>
        ExpandGenerator.GenerateSingle(_metadata);

    public IEnumerable<QueryTestCase> GenerateExpandMultiple() =>
        ExpandGenerator.GenerateMultiple(_metadata);

    public IEnumerable<QueryTestCase> GenerateExpandNestedFilter() =>
        ExpandGenerator.GenerateNestedFilter(_metadata);

    public IEnumerable<QueryTestCase> GenerateExpandNestedSelect() =>
        ExpandGenerator.GenerateNestedSelect(_metadata);

    public IEnumerable<QueryTestCase> GenerateExpandNestedOrderBy() =>
        ExpandGenerator.GenerateNestedOrderBy(_metadata);

    public IEnumerable<QueryTestCase> GenerateExpandMultiLevel() =>
        ExpandGenerator.GenerateMultiLevel(_metadata);

    public IEnumerable<QueryTestCase> GenerateExpandCombinedNested() =>
        ExpandGenerator.GenerateCombinedNested(_metadata);

    public IEnumerable<QueryTestCase> GenerateAllOrderBys() =>
        OrderByGenerator.GenerateAll(_metadata);

    public IEnumerable<QueryTestCase> GenerateAllPaging() =>
        PagingGenerators.GenerateAll(_metadata);

    public IEnumerable<QueryTestCase> GenerateAllCounts() =>
        CountGenerator.GenerateAll(_metadata);

    public IEnumerable<QueryTestCase> GenerateAllApply() =>
        AggregationGenerators.GenerateAllApply(_metadata);

    public IEnumerable<QueryTestCase> GenerateAllCompute() =>
        AggregationGenerators.GenerateAllCompute(_metadata);

    public IEnumerable<QueryTestCase> GenerateAllCombinations() =>
        CombinationGenerator.GenerateAll(_metadata);

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
}
