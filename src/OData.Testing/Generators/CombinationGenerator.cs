namespace OData.Testing.Generators;

/// <summary>
/// Generates test cases that combine multiple OData query options together.
/// These test the evaluation order and interaction between query options:
/// $apply → $compute → $filter → $count → $orderby → $skip → $top → $select/$expand
/// </summary>
public static class CombinationGenerator
{
    public static IEnumerable<QueryTestCase> GenerateAll(EntityMetadata meta)
    {
        return GenerateFilterAndSelect(meta)
            .Concat(GenerateFilterAndOrderBy(meta))
            .Concat(GenerateFilterAndPaging(meta))
            .Concat(GenerateSelectAndExpand(meta))
            .Concat(GenerateExpandAndFilter(meta))
            .Concat(GenerateFilterAndExpand(meta))
            .Concat(GenerateOrderByAndPaging(meta))
            .Concat(GenerateCountAndPaging(meta))
            .Concat(GenerateApplyAndFilter(meta))
            .Concat(GenerateApplyAndOrderBy(meta))
            .Concat(GenerateFullPipeline(meta))
            .Concat(GenerateKitchenSink(meta));
    }

    /// <summary>
    /// $filter + $select: Filter rows then project columns.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateFilterAndSelect(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Combination.FilterAndSelect;

        foreach (var filterProp in meta.NumericProperties.Where(p => !p.IsKey).Take(2))
        {
            foreach (var selectProp in meta.ScalarProperties.Take(2))
            {
                yield return new(es, cat, $"filter_{filterProp.Name}_select_{selectProp.Name}",
                    $"$filter={filterProp.Name} gt {filterProp.SampleODataLiteral}&$select={selectProp.Name}",
                    $"$filter={filterProp.Name} gt {filterProp.SampleODataLiteral}&$select={selectProp.Name}");
            }
        }

        foreach (var filterProp in meta.StringProperties.Take(1))
        {
            var selectNames = string.Join(",", meta.ScalarProperties.Take(3).Select(p => p.Name));
            yield return new(es, cat, $"filter_contains_select_multi",
                $"$filter=contains({filterProp.Name},'test')&$select={selectNames}",
                $"$filter=contains({filterProp.Name},'test')&$select={selectNames}");
        }
    }

    /// <summary>
    /// $filter + $orderby: Filter then sort results.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateFilterAndOrderBy(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Combination.FilterAndOrderBy;

        foreach (var filterProp in meta.NumericProperties.Where(p => !p.IsKey).Take(2))
        {
            foreach (var orderProp in meta.ScalarProperties.Where(p => p.Name != filterProp.Name).Take(2))
            {
                yield return new(es, cat, $"filter_{filterProp.Name}_orderby_{orderProp.Name}",
                    $"$filter={filterProp.Name} gt 0&$orderby={orderProp.Name} asc",
                    $"$filter={filterProp.Name} gt 0&$orderby={orderProp.Name} asc");

                yield return new(es, cat, $"filter_{filterProp.Name}_orderby_{orderProp.Name}_desc",
                    $"$filter={filterProp.Name} gt 0&$orderby={orderProp.Name} desc",
                    $"$filter={filterProp.Name} gt 0&$orderby={orderProp.Name} desc");
            }
        }
    }

    /// <summary>
    /// $filter + $top + $skip: Filter then page.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateFilterAndPaging(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Combination.FilterAndPaging;

        foreach (var prop in meta.NumericProperties.Where(p => !p.IsKey).Take(2))
        {
            yield return new(es, cat, $"filter_{prop.Name}_top5",
                $"$filter={prop.Name} gt 0&$top=5",
                $"$filter={prop.Name} gt 0&$top=5") { ExpectedMaxItems = 5 };

            yield return new(es, cat, $"filter_{prop.Name}_top5_skip2",
                $"$filter={prop.Name} gt 0&$top=5&$skip=2",
                $"$filter={prop.Name} gt 0&$top=5&$skip=2") { ExpectedMaxItems = 5 };
        }

        foreach (var prop in meta.StringProperties.Take(1))
        {
            yield return new(es, cat, $"filter_contains_{prop.Name}_paging",
                $"$filter=contains({prop.Name},'a')&$top=10&$skip=0",
                $"$filter=contains({prop.Name},'a')&$top=10&$skip=0") { ExpectedMaxItems = 10 };
        }
    }

    /// <summary>
    /// $select + $expand: Project with eager loading.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateSelectAndExpand(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Combination.SelectAndExpand;

        foreach (var nav in meta.SingleNavigations)
        {
            var scalarProp = meta.ScalarProperties.FirstOrDefault(p => !p.IsKey);
            if (scalarProp != null)
            {
                yield return new(es, cat, $"select_{scalarProp.Name}_expand_{nav.Name}",
                    $"$select={scalarProp.Name},{nav.Name}&$expand={nav.Name}",
                    $"$select={scalarProp.Name},{nav.Name}&$expand={nav.Name}");

                // Expand with nested select
                var navProp = nav.TargetProperties.FirstOrDefault(p => !p.IsKey);
                if (navProp != null)
                {
                    yield return new(es, cat, $"select_expand_nested_select",
                        $"$select={scalarProp.Name},{nav.Name}&$expand={nav.Name}($select={navProp.Name})",
                        $"$select={scalarProp.Name},{nav.Name}&$expand={nav.Name}($select={navProp.Name})");
                }
            }
        }

        foreach (var nav in meta.CollectionNavigations)
        {
            var scalarProp = meta.ScalarProperties.FirstOrDefault(p => !p.IsKey);
            if (scalarProp != null)
            {
                yield return new(es, cat, $"select_{scalarProp.Name}_expand_collection_{nav.Name}",
                    $"$select={scalarProp.Name},{nav.Name}&$expand={nav.Name}",
                    $"$select={scalarProp.Name},{nav.Name}&$expand={nav.Name}");
            }
        }
    }

    /// <summary>
    /// $expand with nested $filter on the same nav + outer $filter: Both must work together.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateExpandAndFilter(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Combination.ExpandAndFilter;

        foreach (var nav in meta.CollectionNavigations)
        {
            var navProp = nav.TargetProperties.FirstOrDefault(p => !p.IsKey);
            var outerProp = meta.NumericProperties.FirstOrDefault(p => !p.IsKey);

            if (navProp != null && outerProp != null)
            {
                yield return new(es, cat, $"outer_filter_nested_expand_filter",
                    $"$filter={outerProp.Name} gt 0&$expand={nav.Name}($filter={navProp.Name} ne null)",
                    $"$filter={outerProp.Name} gt 0&$expand={nav.Name}($filter={navProp.Name} ne null)");
            }
        }
    }

    /// <summary>
    /// $filter using lambda + $expand on same navigation property.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateFilterAndExpand(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Combination.FilterAndExpand;

        foreach (var nav in meta.CollectionNavigations)
        {
            var navProp = nav.TargetProperties.FirstOrDefault(p => !p.IsKey);
            if (navProp != null)
            {
                yield return new(es, cat, $"lambda_filter_and_expand_{nav.Name}",
                    $"$filter={nav.Name}/any(x:x/{navProp.Name} ne null)&$expand={nav.Name}",
                    $"$filter={nav.Name}/any(x:x/{navProp.Name} ne null)&$expand={nav.Name}");

                yield return new(es, cat, $"lambda_filter_and_expand_nested_{nav.Name}",
                    $"$filter={nav.Name}/any()&$expand={nav.Name}($top=5)",
                    $"$filter={nav.Name}/any()&$expand={nav.Name}($top=5)");
            }
        }
    }

    /// <summary>
    /// $orderby + $top + $skip: Sort then page.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateOrderByAndPaging(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Combination.OrderByAndPaging;

        foreach (var prop in meta.ScalarProperties.Take(3))
        {
            yield return new(es, cat, $"orderby_{prop.Name}_top5",
                $"$orderby={prop.Name} asc&$top=5",
                $"$orderby={prop.Name} asc&$top=5") { ExpectedMaxItems = 5 };

            yield return new(es, cat, $"orderby_{prop.Name}_desc_top5_skip2",
                $"$orderby={prop.Name} desc&$top=5&$skip=2",
                $"$orderby={prop.Name} desc&$top=5&$skip=2") { ExpectedMaxItems = 5 };
        }
    }

    /// <summary>
    /// $count=true + $top + $skip: Verify count reflects total, not page.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateCountAndPaging(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Combination.CountAndPaging;

        yield return new(es, cat, "count_top5",
            "$count=true&$top=5", "$count=true&$top=5")
        { ExpectCount = true, ExpectedMaxItems = 5 };

        yield return new(es, cat, "count_top5_skip2",
            "$count=true&$top=5&$skip=2", "$count=true&$top=5&$skip=2")
        { ExpectCount = true, ExpectedMaxItems = 5 };

        // Count with orderby and paging
        var prop = meta.ScalarProperties.FirstOrDefault();
        if (prop != null)
        {
            yield return new(es, cat, "count_orderby_paging",
                $"$count=true&$orderby={prop.Name} asc&$top=5&$skip=0",
                $"$count=true&$orderby={prop.Name} asc&$top=5&$skip=0")
            { ExpectCount = true, ExpectedMaxItems = 5 };
        }
    }

    /// <summary>
    /// $apply + $filter (post-aggregation filter on aggregated results).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateApplyAndFilter(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Combination.ApplyAndFilter;

        var groupProp = meta.StringProperties.FirstOrDefault();
        var aggProp = meta.NumericProperties.FirstOrDefault(p => !p.IsKey);

        if (groupProp != null && aggProp != null)
        {
            yield return new(es, cat, "groupby_then_filter",
                $"$apply=groupby(({groupProp.Name}),aggregate({aggProp.Name} with sum as Total))&$filter=Total gt 0",
                $"$apply=groupby(({groupProp.Name}),aggregate({aggProp.Name} with sum as Total))&$filter=Total gt 0");

            yield return new(es, cat, "groupby_count_then_filter",
                $"$apply=groupby(({groupProp.Name}),aggregate($count as Count))&$filter=Count gt 1",
                $"$apply=groupby(({groupProp.Name}),aggregate($count as Count))&$filter=Count gt 1");
        }
    }

    /// <summary>
    /// $apply + $orderby: Sort aggregation results.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateApplyAndOrderBy(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Combination.ApplyAndOrderBy;

        var groupProp = meta.StringProperties.FirstOrDefault();
        var aggProp = meta.NumericProperties.FirstOrDefault(p => !p.IsKey);

        if (groupProp != null && aggProp != null)
        {
            yield return new(es, cat, "groupby_orderby_total",
                $"$apply=groupby(({groupProp.Name}),aggregate({aggProp.Name} with sum as Total))&$orderby=Total desc",
                $"$apply=groupby(({groupProp.Name}),aggregate({aggProp.Name} with sum as Total))&$orderby=Total desc");

            yield return new(es, cat, "groupby_orderby_count",
                $"$apply=groupby(({groupProp.Name}),aggregate($count as Count))&$orderby=Count desc",
                $"$apply=groupby(({groupProp.Name}),aggregate($count as Count))&$orderby=Count desc");

            yield return new(es, cat, "groupby_orderby_paging",
                $"$apply=groupby(({groupProp.Name}),aggregate($count as Count))&$orderby=Count desc&$top=5",
                $"$apply=groupby(({groupProp.Name}),aggregate($count as Count))&$orderby=Count desc&$top=5");
        }
    }

    /// <summary>
    /// Full pipeline: $filter + $select + $expand + $orderby + $top + $skip + $count=true.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateFullPipeline(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Combination.FullPipeline;

        var filterProp = meta.NumericProperties.FirstOrDefault(p => !p.IsKey);
        var selectProps = meta.ScalarProperties.Take(3).ToList();
        var nav = meta.SingleNavigations.FirstOrDefault() ??
                  meta.CollectionNavigations.FirstOrDefault();
        var orderProp = meta.ScalarProperties.FirstOrDefault(p => !p.IsKey);

        if (filterProp != null && selectProps.Count >= 2 && orderProp != null)
        {
            var selectNames = string.Join(",", selectProps.Select(p => p.Name));

            // Without expand
            yield return new(es, cat, "filter_select_orderby_paging_count",
                $"$filter={filterProp.Name} gt 0&$select={selectNames}&$orderby={orderProp.Name} asc&$top=10&$skip=0&$count=true",
                $"$filter={filterProp.Name} gt 0&$select={selectNames}&$orderby={orderProp.Name} asc&$top=10&$skip=0&$count=true")
            { ExpectCount = true, ExpectedMaxItems = 10 };

            // With expand
            if (nav != null)
            {
                yield return new(es, cat, "full_pipeline_with_expand",
                    $"$filter={filterProp.Name} gt 0&$select={selectNames},{nav.Name}&$expand={nav.Name}&$orderby={orderProp.Name} desc&$top=5&$skip=0&$count=true",
                    $"$filter={filterProp.Name} gt 0&$select={selectNames},{nav.Name}&$expand={nav.Name}&$orderby={orderProp.Name} desc&$top=5&$skip=0&$count=true")
                { ExpectCount = true, ExpectedMaxItems = 5 };
            }
        }
    }

    /// <summary>
    /// Kitchen sink: maximally complex queries combining many features.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateKitchenSink(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Combination.KitchenSink;

        var numProp = meta.NumericProperties.FirstOrDefault(p => !p.IsKey);
        var strProp = meta.StringProperties.FirstOrDefault();
        var collNav = meta.CollectionNavigations.FirstOrDefault();
        var singleNav = meta.SingleNavigations.FirstOrDefault();

        // Complex filter + expand with nested options + orderby + paging + count
        if (numProp != null && strProp != null && collNav != null)
        {
            var navProp = collNav.TargetProperties.FirstOrDefault(p => !p.IsKey);
            if (navProp != null)
            {
                yield return new(es, cat, "kitchen_sink_1",
                    $"$filter={numProp.Name} gt 0 and contains({strProp.Name},'a')" +
                    $"&$expand={collNav.Name}($filter={navProp.Name} ne null;$orderby={navProp.Name} asc;$top=5)" +
                    $"&$orderby={numProp.Name} desc" +
                    $"&$top=10&$skip=0&$count=true",
                    $"$filter={numProp.Name} gt 0 and contains({strProp.Name},'a')" +
                    $"&$expand={collNav.Name}($filter={navProp.Name} ne null;$orderby={navProp.Name} asc;$top=5)" +
                    $"&$orderby={numProp.Name} desc" +
                    $"&$top=10&$skip=0&$count=true")
                { ExpectCount = true, ExpectedMaxItems = 10 };
            }
        }

        // Filter with lambda + expand + select + orderby + paging
        if (collNav != null && singleNav != null && numProp != null)
        {
            var navProp = collNav.TargetProperties.FirstOrDefault(p => !p.IsKey);
            if (navProp != null)
            {
                yield return new(es, cat, "kitchen_sink_2",
                    $"$filter={collNav.Name}/any(x:x/{navProp.Name} ne null)" +
                    $"&$select={numProp.Name},{collNav.Name},{singleNav.Name}" +
                    $"&$expand={collNav.Name}($top=3),{singleNav.Name}" +
                    $"&$orderby={numProp.Name} asc" +
                    $"&$top=20&$count=true",
                    $"$filter={collNav.Name}/any(x:x/{navProp.Name} ne null)" +
                    $"&$select={numProp.Name},{collNav.Name},{singleNav.Name}" +
                    $"&$expand={collNav.Name}($top=3),{singleNav.Name}" +
                    $"&$orderby={numProp.Name} asc" +
                    $"&$top=20&$count=true")
                { ExpectCount = true, ExpectedMaxItems = 20 };
            }
        }

        // Arithmetic filter + string filter combined + orderby expression + paging
        if (numProp != null && strProp != null)
        {
            yield return new(es, cat, "kitchen_sink_3",
                $"$filter=({numProp.Name} mul 2 gt 10) and (length({strProp.Name}) gt 1)" +
                $"&$orderby=length({strProp.Name}) desc,{numProp.Name} asc" +
                $"&$top=15&$skip=0&$count=true",
                $"$filter=({numProp.Name} mul 2 gt 10) and (length({strProp.Name}) gt 1)" +
                $"&$orderby=length({strProp.Name}) desc,{numProp.Name} asc" +
                $"&$top=15&$skip=0&$count=true")
            { ExpectCount = true, ExpectedMaxItems = 15 };
        }
    }
}
