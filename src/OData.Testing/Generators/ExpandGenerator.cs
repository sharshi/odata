namespace OData.Testing.Generators;

/// <summary>
/// Generates $expand test cases: single, multiple, nested query options,
/// multi-level, $levels, combined nested options, wildcard.
/// </summary>
public static class ExpandGenerator
{
    public static IEnumerable<QueryTestCase> GenerateAll(EntityMetadata meta)
    {
        return GenerateSingle(meta)
            .Concat(GenerateMultiple(meta))
            .Concat(GenerateNestedFilter(meta))
            .Concat(GenerateNestedSelect(meta))
            .Concat(GenerateNestedOrderBy(meta))
            .Concat(GenerateNestedTopSkip(meta))
            .Concat(GenerateNestedCount(meta))
            .Concat(GenerateMultiLevel(meta))
            .Concat(GenerateCombinedNested(meta))
            .Concat(GenerateWildcard(meta));
    }

    /// <summary>
    /// $expand with a single navigation property.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateSingle(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Expand.Single;

        foreach (var nav in meta.SingleNavigations)
        {
            yield return new(es, cat, $"single_{nav.Name}",
                $"$expand={nav.Name}",
                $"$expand={nav.Name}");
        }

        foreach (var nav in meta.CollectionNavigations)
        {
            yield return new(es, cat, $"collection_{nav.Name}",
                $"$expand={nav.Name}",
                $"$expand={nav.Name}");
        }
    }

    /// <summary>
    /// $expand with multiple navigation properties.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateMultiple(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Expand.Multiple;

        var allNavs = meta.SingleNavigations
            .Concat<NavigationMetadata>(meta.CollectionNavigations)
            .ToList();

        if (allNavs.Count >= 2)
        {
            // Expand two navs
            var names = string.Join(",", allNavs.Take(2).Select(n => n.Name));
            yield return new(es, cat, "two_navs",
                $"$expand={names}",
                $"$expand={names}");
        }

        if (allNavs.Count >= 3)
        {
            var names = string.Join(",", allNavs.Take(3).Select(n => n.Name));
            yield return new(es, cat, "three_navs",
                $"$expand={names}",
                $"$expand={names}");
        }

        // All navigations
        if (allNavs.Count > 3)
        {
            var names = string.Join(",", allNavs.Select(n => n.Name));
            yield return new(es, cat, "all_navs",
                $"$expand={names}",
                $"$expand={names}");
        }
    }

    /// <summary>
    /// $expand with nested $filter.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateNestedFilter(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Expand.NestedFilter;

        foreach (var nav in meta.CollectionNavigations)
        {
            foreach (var targetProp in nav.TargetProperties.Where(p => !p.IsKey).Take(3))
            {
                yield return new(es, cat, $"{nav.Name}_filter_{targetProp.Name}_eq",
                    $"$expand={nav.Name}($filter={targetProp.Name} eq {targetProp.SampleODataLiteral})",
                    $"$expand={nav.Name}($filter={targetProp.Name} eq {targetProp.SampleODataLiteral})");

                if (IsNumericType(targetProp.ClrType))
                {
                    yield return new(es, cat, $"{nav.Name}_filter_{targetProp.Name}_gt",
                        $"$expand={nav.Name}($filter={targetProp.Name} gt {targetProp.SampleODataLiteral})",
                        $"$expand={nav.Name}($filter={targetProp.Name} gt {targetProp.SampleODataLiteral})");
                }

                if (targetProp.ClrType == typeof(string))
                {
                    yield return new(es, cat, $"{nav.Name}_filter_contains_{targetProp.Name}",
                        $"$expand={nav.Name}($filter=contains({targetProp.Name},'test'))",
                        $"$expand={nav.Name}($filter=contains({targetProp.Name},'test'))");
                }
            }
        }

        // Nested filter on single navigation is less common but valid via nested expand
        foreach (var nav in meta.SingleNavigations)
        {
            foreach (var targetProp in nav.TargetProperties.Where(p => !p.IsKey && p.ClrType == typeof(string)).Take(1))
            {
                yield return new(es, cat, $"{nav.Name}_single_filter_{targetProp.Name}",
                    $"$expand={nav.Name}($filter={targetProp.Name} ne null)",
                    $"$expand={nav.Name}($filter={targetProp.Name} ne null)");
            }
        }
    }

    /// <summary>
    /// $expand with nested $select.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateNestedSelect(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Expand.NestedSelect;

        foreach (var nav in meta.SingleNavigations.Concat<NavigationMetadata>(meta.CollectionNavigations))
        {
            // Select single target property
            foreach (var targetProp in nav.TargetProperties.Take(2))
            {
                yield return new(es, cat, $"{nav.Name}_select_{targetProp.Name}",
                    $"$expand={nav.Name}($select={targetProp.Name})",
                    $"$expand={nav.Name}($select={targetProp.Name})");
            }

            // Select multiple target properties
            var twoProps = nav.TargetProperties.Take(2).ToList();
            if (twoProps.Count == 2)
            {
                var names = string.Join(",", twoProps.Select(p => p.Name));
                yield return new(es, cat, $"{nav.Name}_select_multi",
                    $"$expand={nav.Name}($select={names})",
                    $"$expand={nav.Name}($select={names})");
            }
        }
    }

    /// <summary>
    /// $expand with nested $orderby.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateNestedOrderBy(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Expand.NestedOrderBy;

        foreach (var nav in meta.CollectionNavigations)
        {
            foreach (var targetProp in nav.TargetProperties.Where(p => !p.IsKey).Take(2))
            {
                yield return new(es, cat, $"{nav.Name}_orderby_{targetProp.Name}_asc",
                    $"$expand={nav.Name}($orderby={targetProp.Name} asc)",
                    $"$expand={nav.Name}($orderby={targetProp.Name} asc)");

                yield return new(es, cat, $"{nav.Name}_orderby_{targetProp.Name}_desc",
                    $"$expand={nav.Name}($orderby={targetProp.Name} desc)",
                    $"$expand={nav.Name}($orderby={targetProp.Name} desc)");
            }
        }
    }

    /// <summary>
    /// $expand with nested $top and $skip.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateNestedTopSkip(EntityMetadata meta)
    {
        var es = meta.EntitySetName;

        foreach (var nav in meta.CollectionNavigations)
        {
            yield return new(es, ODataTestCategory.Expand.NestedTop, $"{nav.Name}_top",
                $"$expand={nav.Name}($top=5)",
                $"$expand={nav.Name}($top=5)");

            yield return new(es, ODataTestCategory.Expand.NestedSkip, $"{nav.Name}_skip",
                $"$expand={nav.Name}($skip=1)",
                $"$expand={nav.Name}($skip=1)");

            yield return new(es, ODataTestCategory.Expand.NestedTop, $"{nav.Name}_top_skip",
                $"$expand={nav.Name}($top=3;$skip=1)",
                $"$expand={nav.Name}($top=3;$skip=1)");
        }
    }

    /// <summary>
    /// $expand with nested $count=true.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateNestedCount(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Expand.NestedCount;

        foreach (var nav in meta.CollectionNavigations)
        {
            yield return new(es, cat, $"{nav.Name}_count",
                $"$expand={nav.Name}($count=true)",
                $"$expand={nav.Name}($count=true)");
        }
    }

    /// <summary>
    /// Multi-level $expand (expand within expand).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateMultiLevel(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Expand.MultiLevel;

        // Two-level expand: nav -> nav.subNav
        foreach (var nav in meta.SingleNavigations)
        {
            foreach (var subNav in nav.TargetNavigations.Take(2))
            {
                yield return new(es, cat, $"{nav.Name}_then_{subNav.Name}",
                    $"$expand={nav.Name}($expand={subNav.Name})",
                    $"$expand={nav.Name}($expand={subNav.Name})");
            }
        }

        foreach (var nav in meta.CollectionNavigations)
        {
            foreach (var subNav in nav.TargetNavigations.Where(n => !n.IsCollection).Take(2))
            {
                yield return new(es, cat, $"{nav.Name}_then_{subNav.Name}",
                    $"$expand={nav.Name}($expand={subNav.Name})",
                    $"$expand={nav.Name}($expand={subNav.Name})");
            }
        }
    }

    /// <summary>
    /// $expand with combined nested query options ($filter + $select + $orderby + $top).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateCombinedNested(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Expand.CombinedNested;

        foreach (var nav in meta.CollectionNavigations)
        {
            var props = nav.TargetProperties.Where(p => !p.IsKey).ToList();
            if (props.Count < 2) continue;

            var filterProp = props[0];
            var selectProp = props[1];

            // filter + select
            yield return new(es, cat, $"{nav.Name}_filter_select",
                $"$expand={nav.Name}($filter={filterProp.Name} ne null;$select={selectProp.Name})",
                $"$expand={nav.Name}($filter={filterProp.Name} ne null;$select={selectProp.Name})");

            // filter + orderby + top
            yield return new(es, cat, $"{nav.Name}_filter_orderby_top",
                $"$expand={nav.Name}($filter={filterProp.Name} ne null;$orderby={selectProp.Name} desc;$top=5)",
                $"$expand={nav.Name}($filter={filterProp.Name} ne null;$orderby={selectProp.Name} desc;$top=5)");

            // filter + select + orderby + top + count
            yield return new(es, cat, $"{nav.Name}_full_nested",
                $"$expand={nav.Name}($filter={filterProp.Name} ne null;$select={filterProp.Name},{selectProp.Name};$orderby={selectProp.Name} asc;$top=10;$count=true)",
                $"$expand={nav.Name}($filter={filterProp.Name} ne null;$select={filterProp.Name},{selectProp.Name};$orderby={selectProp.Name} asc;$top=10;$count=true)");
        }
    }

    /// <summary>
    /// $expand=* (wildcard expand all navigation properties).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateWildcard(EntityMetadata meta)
    {
        var allNavs = meta.SingleNavigations.Concat<NavigationMetadata>(meta.CollectionNavigations).ToList();
        if (allNavs.Count > 0)
        {
            yield return new(meta.EntitySetName, ODataTestCategory.Expand.Wildcard, "wildcard",
                "$expand=*",
                "$expand=*");
        }
    }

    private static bool IsNumericType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t == typeof(int) || t == typeof(long) || t == typeof(short) ||
               t == typeof(decimal) || t == typeof(double) || t == typeof(float) ||
               t == typeof(byte);
    }
}
