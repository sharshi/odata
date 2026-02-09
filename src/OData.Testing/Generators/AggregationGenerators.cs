namespace OData.Testing.Generators;

/// <summary>
/// Generates $apply test cases: aggregate (sum/min/max/avg/countdistinct),
/// groupby, groupby with aggregate, filter transformation, multiple aggregates.
/// Also generates $compute test cases.
/// </summary>
public static class AggregationGenerators
{
    public static IEnumerable<QueryTestCase> GenerateAllApply(EntityMetadata meta)
    {
        return GenerateAggregateSum(meta)
            .Concat(GenerateAggregateMin(meta))
            .Concat(GenerateAggregateMax(meta))
            .Concat(GenerateAggregateAvg(meta))
            .Concat(GenerateAggregateCount(meta))
            .Concat(GenerateAggregateCountDistinct(meta))
            .Concat(GenerateGroupBy(meta))
            .Concat(GenerateGroupByWithAggregate(meta))
            .Concat(GenerateFilterTransformation(meta))
            .Concat(GenerateMultipleAggregates(meta));
    }

    public static IEnumerable<QueryTestCase> GenerateAllCompute(EntityMetadata meta)
    {
        return GenerateComputeBasic(meta)
            .Concat(GenerateComputeInFilter(meta))
            .Concat(GenerateComputeInOrderBy(meta))
            .Concat(GenerateComputeInSelect(meta));
    }

    // ── $apply=aggregate ──

    public static IEnumerable<QueryTestCase> GenerateAggregateSum(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Apply.AggregateSum;

        foreach (var prop in meta.NumericProperties.Where(p => !p.IsKey).Take(3))
        {
            yield return new(es, cat, $"sum_{prop.Name}",
                $"$apply=aggregate({prop.Name} with sum as Total{prop.Name})",
                $"$apply=aggregate({prop.Name} with sum as Total{prop.Name})");
        }
    }

    public static IEnumerable<QueryTestCase> GenerateAggregateMin(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Apply.AggregateMin;

        foreach (var prop in meta.NumericProperties.Where(p => !p.IsKey).Take(3))
        {
            yield return new(es, cat, $"min_{prop.Name}",
                $"$apply=aggregate({prop.Name} with min as Min{prop.Name})",
                $"$apply=aggregate({prop.Name} with min as Min{prop.Name})");
        }
    }

    public static IEnumerable<QueryTestCase> GenerateAggregateMax(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Apply.AggregateMax;

        foreach (var prop in meta.NumericProperties.Where(p => !p.IsKey).Take(3))
        {
            yield return new(es, cat, $"max_{prop.Name}",
                $"$apply=aggregate({prop.Name} with max as Max{prop.Name})",
                $"$apply=aggregate({prop.Name} with max as Max{prop.Name})");
        }
    }

    public static IEnumerable<QueryTestCase> GenerateAggregateAvg(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Apply.AggregateAvg;

        foreach (var prop in meta.NumericProperties.Where(p => !p.IsKey).Take(3))
        {
            yield return new(es, cat, $"avg_{prop.Name}",
                $"$apply=aggregate({prop.Name} with average as Avg{prop.Name})",
                $"$apply=aggregate({prop.Name} with average as Avg{prop.Name})");
        }
    }

    public static IEnumerable<QueryTestCase> GenerateAggregateCount(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Apply.AggregateCount;

        yield return new(es, cat, "count_all",
            "$apply=aggregate($count as TotalCount)",
            "$apply=aggregate($count as TotalCount)");
    }

    public static IEnumerable<QueryTestCase> GenerateAggregateCountDistinct(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Apply.AggregateCountDistinct;

        foreach (var prop in meta.StringProperties.Take(2))
        {
            yield return new(es, cat, $"countdistinct_{prop.Name}",
                $"$apply=aggregate({prop.Name} with countdistinct as Distinct{prop.Name})",
                $"$apply=aggregate({prop.Name} with countdistinct as Distinct{prop.Name})");
        }

        foreach (var prop in meta.NumericProperties.Where(p => !p.IsKey).Take(2))
        {
            yield return new(es, cat, $"countdistinct_{prop.Name}",
                $"$apply=aggregate({prop.Name} with countdistinct as Distinct{prop.Name})",
                $"$apply=aggregate({prop.Name} with countdistinct as Distinct{prop.Name})");
        }
    }

    // ── $apply=groupby ──

    public static IEnumerable<QueryTestCase> GenerateGroupBy(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Apply.GroupBy;

        foreach (var prop in meta.StringProperties.Take(2))
        {
            yield return new(es, cat, $"groupby_{prop.Name}",
                $"$apply=groupby(({prop.Name}))",
                $"$apply=groupby(({prop.Name}))");
        }

        foreach (var prop in meta.BoolProperties.Take(1))
        {
            yield return new(es, cat, $"groupby_{prop.Name}",
                $"$apply=groupby(({prop.Name}))",
                $"$apply=groupby(({prop.Name}))");
        }

        // Multi-field groupby
        var groupProps = meta.StringProperties.Take(2).ToList();
        if (groupProps.Count == 2)
        {
            yield return new(es, cat, "groupby_two_fields",
                $"$apply=groupby(({groupProps[0].Name},{groupProps[1].Name}))",
                $"$apply=groupby(({groupProps[0].Name},{groupProps[1].Name}))");
        }
    }

    public static IEnumerable<QueryTestCase> GenerateGroupByWithAggregate(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Apply.GroupByWithAggregate;

        var groupProp = meta.StringProperties.FirstOrDefault() ?? meta.BoolProperties.FirstOrDefault();
        var aggProp = meta.NumericProperties.FirstOrDefault(p => !p.IsKey);

        if (groupProp != null && aggProp != null)
        {
            yield return new(es, cat, "groupby_sum",
                $"$apply=groupby(({groupProp.Name}),aggregate({aggProp.Name} with sum as Total))",
                $"$apply=groupby(({groupProp.Name}),aggregate({aggProp.Name} with sum as Total))");

            yield return new(es, cat, "groupby_avg",
                $"$apply=groupby(({groupProp.Name}),aggregate({aggProp.Name} with average as Avg))",
                $"$apply=groupby(({groupProp.Name}),aggregate({aggProp.Name} with average as Avg))");

            yield return new(es, cat, "groupby_count",
                $"$apply=groupby(({groupProp.Name}),aggregate($count as Count))",
                $"$apply=groupby(({groupProp.Name}),aggregate($count as Count))");

            yield return new(es, cat, "groupby_min_max",
                $"$apply=groupby(({groupProp.Name}),aggregate({aggProp.Name} with min as Min,{aggProp.Name} with max as Max))",
                $"$apply=groupby(({groupProp.Name}),aggregate({aggProp.Name} with min as Min,{aggProp.Name} with max as Max))");
        }

        // GroupBy on navigation property
        foreach (var nav in meta.SingleNavigations.Take(1))
        {
            var navStrProp = nav.TargetProperties.FirstOrDefault(p => p.ClrType == typeof(string));
            if (navStrProp != null)
            {
                yield return new(es, cat, $"groupby_nav_{nav.Name}_{navStrProp.Name}",
                    $"$apply=groupby(({nav.Name}/{navStrProp.Name}),aggregate($count as Count))",
                    $"$apply=groupby(({nav.Name}/{navStrProp.Name}),aggregate($count as Count))");
            }
        }
    }

    public static IEnumerable<QueryTestCase> GenerateFilterTransformation(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Apply.FilterTransformation;

        var numProp = meta.NumericProperties.FirstOrDefault(p => !p.IsKey);
        var groupProp = meta.StringProperties.FirstOrDefault();

        if (numProp != null)
        {
            // filter then aggregate
            yield return new(es, cat, "filter_then_aggregate",
                $"$apply=filter({numProp.Name} gt 0)/aggregate($count as Count)",
                $"$apply=filter({numProp.Name} gt 0)/aggregate($count as Count)");
        }

        if (numProp != null && groupProp != null)
        {
            // filter then groupby with aggregate
            yield return new(es, cat, "filter_then_groupby",
                $"$apply=filter({numProp.Name} gt 0)/groupby(({groupProp.Name}),aggregate($count as Count))",
                $"$apply=filter({numProp.Name} gt 0)/groupby(({groupProp.Name}),aggregate($count as Count))");
        }

        // String filter then aggregate
        var strProp = meta.StringProperties.FirstOrDefault();
        if (strProp != null)
        {
            yield return new(es, cat, "filter_contains_then_count",
                $"$apply=filter(contains({strProp.Name},'a'))/aggregate($count as Count)",
                $"$apply=filter(contains({strProp.Name},'a'))/aggregate($count as Count)");
        }
    }

    public static IEnumerable<QueryTestCase> GenerateMultipleAggregates(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Apply.MultipleAggregates;

        var numProps = meta.NumericProperties.Where(p => !p.IsKey).Take(2).ToList();

        if (numProps.Count >= 1)
        {
            var p = numProps[0];
            yield return new(es, cat, "sum_avg_count",
                $"$apply=aggregate({p.Name} with sum as Total,{p.Name} with average as Avg,$count as Count)",
                $"$apply=aggregate({p.Name} with sum as Total,{p.Name} with average as Avg,$count as Count)");

            yield return new(es, cat, "min_max",
                $"$apply=aggregate({p.Name} with min as Min,{p.Name} with max as Max)",
                $"$apply=aggregate({p.Name} with min as Min,{p.Name} with max as Max)");
        }

        if (numProps.Count >= 2)
        {
            yield return new(es, cat, "cross_property_agg",
                $"$apply=aggregate({numProps[0].Name} with sum as Total1,{numProps[1].Name} with sum as Total2,$count as Count)",
                $"$apply=aggregate({numProps[0].Name} with sum as Total1,{numProps[1].Name} with sum as Total2,$count as Count)");
        }
    }

    // ── $compute ──

    public static IEnumerable<QueryTestCase> GenerateComputeBasic(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Compute.Basic;

        var numProps = meta.NumericProperties.Where(p => !p.IsKey).Take(2).ToList();

        if (numProps.Count >= 1)
        {
            yield return new(es, cat, "mul_constant",
                $"$compute={numProps[0].Name} mul 2 as Doubled",
                $"$compute={numProps[0].Name} mul 2 as Doubled");
        }

        if (numProps.Count >= 2)
        {
            yield return new(es, cat, "add_properties",
                $"$compute={numProps[0].Name} add {numProps[1].Name} as Combined",
                $"$compute={numProps[0].Name} add {numProps[1].Name} as Combined");

            yield return new(es, cat, "mul_properties",
                $"$compute={numProps[0].Name} mul {numProps[1].Name} as Product",
                $"$compute={numProps[0].Name} mul {numProps[1].Name} as Product");
        }

        // String computation
        foreach (var prop in meta.StringProperties.Take(1))
        {
            yield return new(es, cat, $"length_{prop.Name}",
                $"$compute=length({prop.Name}) as {prop.Name}Length",
                $"$compute=length({prop.Name}) as {prop.Name}Length");
        }
    }

    public static IEnumerable<QueryTestCase> GenerateComputeInFilter(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Compute.InFilter;

        var numProps = meta.NumericProperties.Where(p => !p.IsKey).Take(2).ToList();

        if (numProps.Count >= 1)
        {
            yield return new(es, cat, "filter_computed",
                $"$compute={numProps[0].Name} mul 2 as Doubled&$filter=Doubled gt 10",
                $"$compute={numProps[0].Name} mul 2 as Doubled&$filter=Doubled gt 10");
        }

        if (numProps.Count >= 2)
        {
            yield return new(es, cat, "filter_computed_sum",
                $"$compute={numProps[0].Name} add {numProps[1].Name} as Total&$filter=Total gt 0",
                $"$compute={numProps[0].Name} add {numProps[1].Name} as Total&$filter=Total gt 0");
        }
    }

    public static IEnumerable<QueryTestCase> GenerateComputeInOrderBy(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Compute.InOrderBy;

        var numProps = meta.NumericProperties.Where(p => !p.IsKey).Take(2).ToList();

        if (numProps.Count >= 1)
        {
            yield return new(es, cat, "orderby_computed",
                $"$compute={numProps[0].Name} mul 2 as Doubled&$orderby=Doubled desc",
                $"$compute={numProps[0].Name} mul 2 as Doubled&$orderby=Doubled desc");
        }

        if (numProps.Count >= 2)
        {
            yield return new(es, cat, "orderby_computed_sum",
                $"$compute={numProps[0].Name} add {numProps[1].Name} as Total&$orderby=Total asc",
                $"$compute={numProps[0].Name} add {numProps[1].Name} as Total&$orderby=Total asc");
        }
    }

    public static IEnumerable<QueryTestCase> GenerateComputeInSelect(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Compute.InSelect;

        var numProps = meta.NumericProperties.Where(p => !p.IsKey).Take(2).ToList();
        var firstScalar = meta.ScalarProperties.FirstOrDefault();

        if (numProps.Count >= 1 && firstScalar != null)
        {
            yield return new(es, cat, "select_computed",
                $"$compute={numProps[0].Name} mul 2 as Doubled&$select={firstScalar.Name},Doubled",
                $"$compute={numProps[0].Name} mul 2 as Doubled&$select={firstScalar.Name},Doubled");
        }
    }
}
