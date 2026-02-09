namespace OData.Testing.Generators;

/// <summary>
/// Generates $top, $skip, and combined paging test cases.
/// </summary>
public static class PagingGenerators
{
    public static IEnumerable<QueryTestCase> GenerateAll(EntityMetadata meta)
    {
        return GenerateTop(meta)
            .Concat(GenerateSkip(meta))
            .Concat(GenerateTopAndSkip(meta))
            .Concat(GenerateEdgeCases(meta));
    }

    /// <summary>
    /// $top with various values.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateTop(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Paging.Top;

        yield return new(es, cat, "top_1",
            "$top=1", "$top=1") { ExpectedMaxItems = 1 };

        yield return new(es, cat, "top_5",
            "$top=5", "$top=5") { ExpectedMaxItems = 5 };

        yield return new(es, cat, "top_10",
            "$top=10", "$top=10") { ExpectedMaxItems = 10 };

        yield return new(es, cat, "top_50",
            "$top=50", "$top=50") { ExpectedMaxItems = 50 };

        yield return new(es, cat, "top_100",
            "$top=100", "$top=100") { ExpectedMaxItems = 100 };
    }

    /// <summary>
    /// $skip with various values.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateSkip(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Paging.Skip;

        yield return new(es, cat, "skip_0",
            "$skip=0", "$skip=0");

        yield return new(es, cat, "skip_1",
            "$skip=1", "$skip=1");

        yield return new(es, cat, "skip_5",
            "$skip=5", "$skip=5");

        yield return new(es, cat, "skip_10",
            "$skip=10", "$skip=10");
    }

    /// <summary>
    /// Combined $top and $skip (paging).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateTopAndSkip(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Paging.TopAndSkip;

        // Page 1 (size 5)
        yield return new(es, cat, "page1_size5",
            "$top=5&$skip=0", "$top=5&$skip=0") { ExpectedMaxItems = 5 };

        // Page 2 (size 5)
        yield return new(es, cat, "page2_size5",
            "$top=5&$skip=5", "$top=5&$skip=5") { ExpectedMaxItems = 5 };

        // Page 3 (size 5)
        yield return new(es, cat, "page3_size5",
            "$top=5&$skip=10", "$top=5&$skip=10") { ExpectedMaxItems = 5 };

        // Page 1 (size 10)
        yield return new(es, cat, "page1_size10",
            "$top=10&$skip=0", "$top=10&$skip=0") { ExpectedMaxItems = 10 };

        // Page 2 (size 10)
        yield return new(es, cat, "page2_size10",
            "$top=10&$skip=10", "$top=10&$skip=10") { ExpectedMaxItems = 10 };

        // Page 1 (size 1) - single item paging
        yield return new(es, cat, "page1_size1",
            "$top=1&$skip=0", "$top=1&$skip=0") { ExpectedMaxItems = 1 };

        // Page 2 (size 1)
        yield return new(es, cat, "page2_size1",
            "$top=1&$skip=1", "$top=1&$skip=1") { ExpectedMaxItems = 1 };

        // Large page size
        yield return new(es, cat, "large_page",
            "$top=100&$skip=0", "$top=100&$skip=0") { ExpectedMaxItems = 100 };
    }

    /// <summary>
    /// Edge cases: $top=0, $skip beyond count.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateEdgeCases(EntityMetadata meta)
    {
        var es = meta.EntitySetName;

        // $top=0 should return empty result
        yield return new(es, ODataTestCategory.Paging.TopZero, "top_zero",
            "$top=0", "$top=0") { ExpectedMaxItems = 0 };

        // $skip beyond expected data count
        yield return new(es, ODataTestCategory.Paging.SkipBeyondCount, "skip_1000",
            "$skip=1000", "$skip=1000") { ExpectedMaxItems = 0 };

        yield return new(es, ODataTestCategory.Paging.SkipBeyondCount, "skip_99999",
            "$skip=99999", "$skip=99999") { ExpectedMaxItems = 0 };
    }
}
