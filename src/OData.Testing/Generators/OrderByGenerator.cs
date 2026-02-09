namespace OData.Testing.Generators;

/// <summary>
/// Generates $orderby test cases: single ascending/descending, multiple columns,
/// navigation property ordering, ordering by count, ordering by expression.
/// </summary>
public static class OrderByGenerator
{
    public static IEnumerable<QueryTestCase> GenerateAll(EntityMetadata meta)
    {
        return GenerateSingleAsc(meta)
            .Concat(GenerateSingleDesc(meta))
            .Concat(GenerateMultiple(meta))
            .Concat(GenerateNavigationProperty(meta))
            .Concat(GenerateByCount(meta))
            .Concat(GenerateByExpression(meta));
    }

    /// <summary>
    /// $orderby=[Property] (ascending, default).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateSingleAsc(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.OrderBy.SingleAsc;

        foreach (var prop in meta.ScalarProperties)
        {
            yield return new(es, cat, $"asc_{prop.Name}",
                $"$orderby={prop.Name}",
                $"$orderby={prop.Name}");

            yield return new(es, cat, $"asc_explicit_{prop.Name}",
                $"$orderby={prop.Name} asc",
                $"$orderby={prop.Name} asc");
        }
    }

    /// <summary>
    /// $orderby=[Property] desc.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateSingleDesc(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.OrderBy.SingleDesc;

        foreach (var prop in meta.ScalarProperties)
        {
            yield return new(es, cat, $"desc_{prop.Name}",
                $"$orderby={prop.Name} desc",
                $"$orderby={prop.Name} desc");
        }
    }

    /// <summary>
    /// $orderby with multiple columns.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateMultiple(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.OrderBy.Multiple;

        var props = meta.ScalarProperties.ToList();

        if (props.Count >= 2)
        {
            // Two columns, same direction
            yield return new(es, cat, "two_asc",
                $"$orderby={props[0].Name} asc,{props[1].Name} asc",
                $"$orderby={props[0].Name} asc,{props[1].Name} asc");

            // Two columns, mixed directions
            yield return new(es, cat, "mixed_asc_desc",
                $"$orderby={props[0].Name} asc,{props[1].Name} desc",
                $"$orderby={props[0].Name} asc,{props[1].Name} desc");

            yield return new(es, cat, "mixed_desc_asc",
                $"$orderby={props[0].Name} desc,{props[1].Name} asc",
                $"$orderby={props[0].Name} desc,{props[1].Name} asc");
        }

        if (props.Count >= 3)
        {
            yield return new(es, cat, "three_columns",
                $"$orderby={props[0].Name} asc,{props[1].Name} desc,{props[2].Name} asc",
                $"$orderby={props[0].Name} asc,{props[1].Name} desc,{props[2].Name} asc");
        }
    }

    /// <summary>
    /// $orderby on navigation property fields.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateNavigationProperty(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.OrderBy.NavigationProperty;

        foreach (var nav in meta.SingleNavigations)
        {
            foreach (var targetProp in nav.TargetProperties.Where(p => !p.IsKey).Take(2))
            {
                yield return new(es, cat, $"{nav.Name}_{targetProp.Name}_asc",
                    $"$orderby={nav.Name}/{targetProp.Name} asc",
                    $"$orderby={nav.Name}/{targetProp.Name} asc");

                yield return new(es, cat, $"{nav.Name}_{targetProp.Name}_desc",
                    $"$orderby={nav.Name}/{targetProp.Name} desc",
                    $"$orderby={nav.Name}/{targetProp.Name} desc");
            }
        }
    }

    /// <summary>
    /// $orderby=[CollectionNav]/$count (order by count of related entities).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateByCount(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.OrderBy.ByCount;

        foreach (var nav in meta.CollectionNavigations)
        {
            yield return new(es, cat, $"{nav.Name}_count_asc",
                $"$orderby={nav.Name}/$count asc",
                $"$orderby={nav.Name}/$count asc");

            yield return new(es, cat, $"{nav.Name}_count_desc",
                $"$orderby={nav.Name}/$count desc",
                $"$orderby={nav.Name}/$count desc");
        }
    }

    /// <summary>
    /// $orderby with expressions (functions, arithmetic).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateByExpression(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.OrderBy.ByExpression;

        // Order by string length
        foreach (var prop in meta.StringProperties.Take(2))
        {
            yield return new(es, cat, $"length_{prop.Name}",
                $"$orderby=length({prop.Name}) asc",
                $"$orderby=length({prop.Name}) asc");

            yield return new(es, cat, $"length_{prop.Name}_desc",
                $"$orderby=length({prop.Name}) desc",
                $"$orderby=length({prop.Name}) desc");
        }

        // Order by year of date
        foreach (var prop in meta.DateTimeProperties.Take(1))
        {
            yield return new(es, cat, $"year_{prop.Name}",
                $"$orderby=year({prop.Name}) desc",
                $"$orderby=year({prop.Name}) desc");
        }

        // Order by arithmetic expression
        var numProps = meta.NumericProperties.Where(p => !p.IsKey).Take(2).ToList();
        if (numProps.Count >= 1)
        {
            yield return new(es, cat, "arithmetic",
                $"$orderby={numProps[0].Name} mul 2 desc",
                $"$orderby={numProps[0].Name} mul 2 desc");
        }

        if (numProps.Count >= 2)
        {
            yield return new(es, cat, "cross_property_arithmetic",
                $"$orderby={numProps[0].Name} add {numProps[1].Name} desc",
                $"$orderby={numProps[0].Name} add {numProps[1].Name} desc");
        }
    }
}
