namespace OData.Testing.Generators;

/// <summary>
/// Generates $count test cases: inline count, count with filter, count with paging.
/// </summary>
public static class CountGenerator
{
    public static IEnumerable<QueryTestCase> GenerateAll(EntityMetadata meta)
    {
        return GenerateInlineCount(meta)
            .Concat(GenerateCountWithFilter(meta))
            .Concat(GenerateCountWithPaging(meta))
            .Concat(GenerateCountInFilter(meta));
    }

    /// <summary>
    /// $count=true (inline count in response).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateInlineCount(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Count.InlineCount;

        yield return new(es, cat, "inline_count",
            "$count=true", "$count=true") { ExpectCount = true };

        yield return new(es, cat, "inline_count_false",
            "$count=false", "$count=false") { ExpectCount = false };
    }

    /// <summary>
    /// $count=true combined with $filter.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateCountWithFilter(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Count.CountWithFilter;

        foreach (var prop in meta.NumericProperties.Where(p => !p.IsKey).Take(2))
        {
            yield return new(es, cat, $"count_filter_{prop.Name}_gt",
                $"$count=true&$filter={prop.Name} gt {prop.SampleODataLiteral}",
                $"$count=true&$filter={prop.Name} gt {prop.SampleODataLiteral}") { ExpectCount = true };
        }

        foreach (var prop in meta.StringProperties.Take(2))
        {
            yield return new(es, cat, $"count_filter_contains_{prop.Name}",
                $"$count=true&$filter=contains({prop.Name},'test')",
                $"$count=true&$filter=contains({prop.Name},'test')") { ExpectCount = true };
        }

        foreach (var prop in meta.BoolProperties.Take(1))
        {
            yield return new(es, cat, $"count_filter_{prop.Name}_true",
                $"$count=true&$filter={prop.Name} eq true",
                $"$count=true&$filter={prop.Name} eq true") { ExpectCount = true };
        }
    }

    /// <summary>
    /// $count=true combined with $top/$skip (count should reflect total, not page).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateCountWithPaging(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Count.CountWithPaging;

        yield return new(es, cat, "count_with_top",
            "$count=true&$top=5", "$count=true&$top=5")
        { ExpectCount = true, ExpectedMaxItems = 5 };

        yield return new(es, cat, "count_with_skip",
            "$count=true&$skip=2", "$count=true&$skip=2") { ExpectCount = true };

        yield return new(es, cat, "count_with_top_and_skip",
            "$count=true&$top=5&$skip=2", "$count=true&$top=5&$skip=2")
        { ExpectCount = true, ExpectedMaxItems = 5 };

        // Count with filter and paging
        var numProp = meta.NumericProperties.FirstOrDefault(p => !p.IsKey);
        if (numProp != null)
        {
            yield return new(es, cat, "count_filter_paging",
                $"$count=true&$filter={numProp.Name} gt 0&$top=3&$skip=1",
                $"$count=true&$filter={numProp.Name} gt 0&$top=3&$skip=1")
            { ExpectCount = true, ExpectedMaxItems = 3 };
        }
    }

    /// <summary>
    /// Using /$count in $filter (filter by collection count).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateCountInFilter(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Count.CountInFilter;

        foreach (var nav in meta.CollectionNavigations)
        {
            yield return new(es, cat, $"{nav.Name}_count_gt_0",
                $"$filter={nav.Name}/$count gt 0",
                $"$filter={nav.Name}/$count gt 0");

            yield return new(es, cat, $"{nav.Name}_count_ge_1",
                $"$filter={nav.Name}/$count ge 1",
                $"$filter={nav.Name}/$count ge 1");

            yield return new(es, cat, $"{nav.Name}_count_eq_0",
                $"$filter={nav.Name}/$count eq 0",
                $"$filter={nav.Name}/$count eq 0");

            yield return new(es, cat, $"{nav.Name}_count_lt_10",
                $"$filter={nav.Name}/$count lt 10",
                $"$filter={nav.Name}/$count lt 10");
        }
    }
}
