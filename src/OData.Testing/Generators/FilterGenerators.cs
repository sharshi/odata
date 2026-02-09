namespace OData.Testing.Generators;

/// <summary>
/// Generates $filter test cases across all filter subcategories:
/// comparison, logical, arithmetic, string functions, date functions,
/// math functions, lambda operators, null handling, enums, and special characters.
/// </summary>
public static class FilterGenerators
{
    /// <summary>
    /// Generates all filter test cases for the given entity metadata.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateAll(EntityMetadata meta)
    {
        return GenerateComparison(meta)
            .Concat(GenerateLogical(meta))
            .Concat(GenerateArithmetic(meta))
            .Concat(GenerateStringFunctions(meta))
            .Concat(GenerateDateFunctions(meta))
            .Concat(GenerateMathFunctions(meta))
            .Concat(GenerateLambda(meta))
            .Concat(GenerateNull(meta))
            .Concat(GenerateIn(meta))
            .Concat(GenerateNavigationProperty(meta))
            .Concat(GenerateSpecialCharacters(meta));
    }

    /// <summary>
    /// $filter comparison operators: eq, ne, gt, ge, lt, le.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateComparison(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Filter.Comparison;

        // eq/ne for all property types
        foreach (var prop in meta.ScalarProperties.Where(p => !p.IsKey))
        {
            yield return new(es, cat, "eq",
                $"$filter={prop.Name} eq {prop.SampleODataLiteral}",
                $"$filter={prop.Name} eq {prop.SampleODataLiteral}");

            yield return new(es, cat, "ne",
                $"$filter={prop.Name} ne {prop.SampleODataLiteral}",
                $"$filter={prop.Name} ne {prop.SampleODataLiteral}");
        }

        // gt, ge, lt, le for numeric properties
        foreach (var prop in meta.NumericProperties.Where(p => !p.IsKey))
        {
            yield return new(es, cat, "gt",
                $"$filter={prop.Name} gt {prop.SampleODataLiteral}",
                $"$filter={prop.Name} gt {prop.SampleODataLiteral}");

            yield return new(es, cat, "ge",
                $"$filter={prop.Name} ge {prop.SampleODataLiteral}",
                $"$filter={prop.Name} ge {prop.SampleODataLiteral}");

            yield return new(es, cat, "lt",
                $"$filter={prop.Name} lt {prop.SampleODataLiteral2}",
                $"$filter={prop.Name} lt {prop.SampleODataLiteral2}");

            yield return new(es, cat, "le",
                $"$filter={prop.Name} le {prop.SampleODataLiteral2}",
                $"$filter={prop.Name} le {prop.SampleODataLiteral2}");
        }

        // gt, ge, lt, le for datetime properties
        foreach (var prop in meta.DateTimeProperties)
        {
            yield return new(es, cat, "gt_date",
                $"$filter={prop.Name} gt {prop.SampleODataLiteral}",
                $"$filter={prop.Name} gt {prop.SampleODataLiteral}");

            yield return new(es, cat, "lt_date",
                $"$filter={prop.Name} lt {prop.SampleODataLiteral2}",
                $"$filter={prop.Name} lt {prop.SampleODataLiteral2}");
        }

        // eq for bool properties
        foreach (var prop in meta.BoolProperties)
        {
            yield return new(es, cat, "eq_bool_true",
                $"$filter={prop.Name} eq true",
                $"$filter={prop.Name} eq true");

            yield return new(es, cat, "eq_bool_false",
                $"$filter={prop.Name} eq false",
                $"$filter={prop.Name} eq false");
        }

        // eq for guid properties
        foreach (var prop in meta.GuidProperties)
        {
            yield return new(es, cat, "eq_guid",
                $"$filter={prop.Name} eq {prop.SampleODataLiteral}",
                $"$filter={prop.Name} eq {prop.SampleODataLiteral}");
        }
    }

    /// <summary>
    /// $filter logical operators: and, or, not, grouping.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateLogical(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Filter.Logical;

        var numProps = meta.NumericProperties.Where(p => !p.IsKey).ToList();
        var strProps = meta.StringProperties.ToList();

        // and: combine two numeric comparisons
        if (numProps.Count >= 1)
        {
            var p = numProps[0];
            yield return new(es, cat, "and",
                $"$filter={p.Name} gt {p.SampleODataLiteral} and {p.Name} lt {p.SampleODataLiteral2}",
                $"$filter={p.Name} gt {p.SampleODataLiteral} and {p.Name} lt {p.SampleODataLiteral2}");
        }

        // or: either of two conditions
        if (numProps.Count >= 1)
        {
            var p = numProps[0];
            yield return new(es, cat, "or",
                $"$filter={p.Name} lt {p.SampleODataLiteral} or {p.Name} gt {p.SampleODataLiteral2}",
                $"$filter={p.Name} lt {p.SampleODataLiteral} or {p.Name} gt {p.SampleODataLiteral2}");
        }

        // not: negate a string function
        if (strProps.Count >= 1)
        {
            var p = strProps[0];
            yield return new(es, cat, "not",
                $"$filter=not endswith({p.Name},'test')",
                $"$filter=not endswith({p.Name},'test')");
        }

        // Grouping with parentheses
        if (numProps.Count >= 1 && strProps.Count >= 1)
        {
            var np = numProps[0];
            var sp = strProps[0];
            yield return new(es, cat, "grouping",
                $"$filter=({np.Name} gt {np.SampleODataLiteral}) and (contains({sp.Name},'test'))",
                $"$filter=({np.Name} gt {np.SampleODataLiteral}) and (contains({sp.Name},'test'))");
        }

        // Multiple and/or combined
        if (numProps.Count >= 1 && strProps.Count >= 1)
        {
            var np = numProps[0];
            var sp = strProps[0];
            yield return new(es, cat, "complex_logical",
                $"$filter=({np.Name} gt {np.SampleODataLiteral} or {np.Name} lt 0) and contains({sp.Name},'a')",
                $"$filter=({np.Name} gt {np.SampleODataLiteral} or {np.Name} lt 0) and contains({sp.Name},'a')");
        }

        // Cross-property comparison (if we have 2 numeric props)
        if (numProps.Count >= 2)
        {
            yield return new(es, cat, "cross_property",
                $"$filter={numProps[0].Name} gt {numProps[1].Name}",
                $"$filter={numProps[0].Name} gt {numProps[1].Name}");
        }
    }

    /// <summary>
    /// $filter arithmetic operators: add, sub, mul, div, mod.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateArithmetic(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Filter.Arithmetic;

        foreach (var prop in meta.NumericProperties.Where(p => !p.IsKey).Take(2))
        {
            yield return new(es, cat, "add",
                $"$filter={prop.Name} add 5 gt 10",
                $"$filter={prop.Name} add 5 gt 10");

            yield return new(es, cat, "sub",
                $"$filter={prop.Name} sub 5 gt 0",
                $"$filter={prop.Name} sub 5 gt 0");

            yield return new(es, cat, "mul",
                $"$filter={prop.Name} mul 2 gt 10",
                $"$filter={prop.Name} mul 2 gt 10");

            yield return new(es, cat, "div",
                $"$filter={prop.Name} div 2 gt 0",
                $"$filter={prop.Name} div 2 gt 0");

            yield return new(es, cat, "mod",
                $"$filter={prop.Name} mod 2 eq 0",
                $"$filter={prop.Name} mod 2 eq 0");
        }

        // Combined arithmetic
        var numProps = meta.NumericProperties.Where(p => !p.IsKey).ToList();
        if (numProps.Count >= 2)
        {
            yield return new(es, cat, "combined",
                $"$filter={numProps[0].Name} mul {numProps[1].Name} gt 0",
                $"$filter={numProps[0].Name} mul {numProps[1].Name} gt 0");
        }
    }

    /// <summary>
    /// $filter string functions: contains, startswith, endswith, tolower, toupper,
    /// trim, concat, indexof, substring, length.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateStringFunctions(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Filter.StringFunctions;

        foreach (var prop in meta.StringProperties.Take(3))
        {
            yield return new(es, cat, "contains",
                $"$filter=contains({prop.Name},'test')",
                $"$filter=contains({prop.Name},'test')");

            yield return new(es, cat, "startswith",
                $"$filter=startswith({prop.Name},'T')",
                $"$filter=startswith({prop.Name},'T')");

            yield return new(es, cat, "endswith",
                $"$filter=endswith({prop.Name},'t')",
                $"$filter=endswith({prop.Name},'t')");

            yield return new(es, cat, "tolower",
                $"$filter=tolower({prop.Name}) eq 'test'",
                $"$filter=tolower({prop.Name}) eq 'test'");

            yield return new(es, cat, "toupper",
                $"$filter=toupper({prop.Name}) eq 'TEST'",
                $"$filter=toupper({prop.Name}) eq 'TEST'");

            yield return new(es, cat, "trim",
                $"$filter=trim({prop.Name}) eq 'test'",
                $"$filter=trim({prop.Name}) eq 'test'");

            yield return new(es, cat, "length",
                $"$filter=length({prop.Name}) gt 0",
                $"$filter=length({prop.Name}) gt 0");

            yield return new(es, cat, "indexof",
                $"$filter=indexof({prop.Name},'e') gt -1",
                $"$filter=indexof({prop.Name},'e') gt -1");

            yield return new(es, cat, "substring",
                $"$filter=substring({prop.Name},0,1) eq 'T'",
                $"$filter=substring({prop.Name},0,1) eq 'T'");

            yield return new(es, cat, "concat",
                $"$filter=concat({prop.Name},'_suffix') ne ''",
                $"$filter=concat({prop.Name},'_suffix') ne ''");

            // Nested function calls
            yield return new(es, cat, "nested_contains_tolower",
                $"$filter=contains(tolower({prop.Name}),'test')",
                $"$filter=contains(tolower({prop.Name}),'test')");

            yield return new(es, cat, "nested_startswith_trim",
                $"$filter=startswith(trim({prop.Name}),'T')",
                $"$filter=startswith(trim({prop.Name}),'T')");

            yield return new(es, cat, "length_gt",
                $"$filter=length({prop.Name}) ge 1",
                $"$filter=length({prop.Name}) ge 1");
        }
    }

    /// <summary>
    /// $filter date/time functions: year, month, day, hour, minute, second, date, time, now.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateDateFunctions(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Filter.DateFunctions;

        foreach (var prop in meta.DateTimeProperties.Take(2))
        {
            yield return new(es, cat, "year",
                $"$filter=year({prop.Name}) eq 2024",
                $"$filter=year({prop.Name}) eq 2024");

            yield return new(es, cat, "month",
                $"$filter=month({prop.Name}) eq 1",
                $"$filter=month({prop.Name}) eq 1");

            yield return new(es, cat, "day",
                $"$filter=day({prop.Name}) eq 15",
                $"$filter=day({prop.Name}) eq 15");

            yield return new(es, cat, "hour",
                $"$filter=hour({prop.Name}) ge 0",
                $"$filter=hour({prop.Name}) ge 0");

            yield return new(es, cat, "minute",
                $"$filter=minute({prop.Name}) ge 0",
                $"$filter=minute({prop.Name}) ge 0");

            yield return new(es, cat, "second",
                $"$filter=second({prop.Name}) ge 0",
                $"$filter=second({prop.Name}) ge 0");

            yield return new(es, cat, "date",
                $"$filter=date({prop.Name}) gt 2020-01-01",
                $"$filter=date({prop.Name}) gt 2020-01-01");

            yield return new(es, cat, "before_now",
                $"$filter={prop.Name} lt now()",
                $"$filter={prop.Name} lt now()");

            yield return new(es, cat, "year_and_month",
                $"$filter=year({prop.Name}) eq 2024 and month({prop.Name}) eq 6",
                $"$filter=year({prop.Name}) eq 2024 and month({prop.Name}) eq 6");
        }
    }

    /// <summary>
    /// $filter math functions: round, floor, ceiling.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateMathFunctions(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Filter.MathFunctions;

        foreach (var prop in meta.DecimalProperties.Take(2))
        {
            yield return new(es, cat, "round",
                $"$filter=round({prop.Name}) gt 0",
                $"$filter=round({prop.Name}) gt 0");

            yield return new(es, cat, "floor",
                $"$filter=floor({prop.Name}) ge 0",
                $"$filter=floor({prop.Name}) ge 0");

            yield return new(es, cat, "ceiling",
                $"$filter=ceiling({prop.Name}) ge 1",
                $"$filter=ceiling({prop.Name}) ge 1");
        }
    }

    /// <summary>
    /// $filter lambda operators: any, all on collection navigation properties.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateLambda(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Filter.Lambda;

        foreach (var nav in meta.CollectionNavigations)
        {
            // any() - non-empty check
            yield return new(es, cat, "any_nonempty",
                $"$filter={nav.Name}/any()",
                $"$filter={nav.Name}/any()");

            // any with predicate on each target property
            foreach (var targetProp in nav.TargetProperties.Where(p => !p.IsKey).Take(3))
            {
                yield return new(es, cat, "any_predicate",
                    $"$filter={nav.Name}/any(x:x/{targetProp.Name} eq {targetProp.SampleODataLiteral})",
                    $"$filter={nav.Name}/any(x:x/{targetProp.Name} eq {targetProp.SampleODataLiteral})");
            }

            // all with predicate
            foreach (var targetProp in nav.TargetProperties.Where(p => !p.IsKey).Take(2))
            {
                yield return new(es, cat, "all_predicate",
                    $"$filter={nav.Name}/all(x:x/{targetProp.Name} ne null)",
                    $"$filter={nav.Name}/all(x:x/{targetProp.Name} ne null)");
            }

            // any with multiple conditions
            var twoProps = nav.TargetProperties.Where(p => !p.IsKey).Take(2).ToList();
            if (twoProps.Count == 2)
            {
                yield return new(es, cat, "any_multi_condition",
                    $"$filter={nav.Name}/any(x:x/{twoProps[0].Name} eq {twoProps[0].SampleODataLiteral} and x/{twoProps[1].Name} ne null)",
                    $"$filter={nav.Name}/any(x:x/{twoProps[0].Name} eq {twoProps[0].SampleODataLiteral} and x/{twoProps[1].Name} ne null)");
            }

            // Nested lambda (any inside any) - if target has collection navs
            foreach (var nestedNav in nav.TargetNavigations.Where(n => n.IsCollection).Take(1))
            {
                var nestedProp = nestedNav.TargetProperties.FirstOrDefault(p => !p.IsKey);
                if (nestedProp != null)
                {
                    yield return new(es, cat, "nested_any",
                        $"$filter={nav.Name}/any(x:x/{nestedNav.Name}/any(y:y/{nestedProp.Name} ne null))",
                        $"$filter={nav.Name}/any(x:x/{nestedNav.Name}/any(y:y/{nestedProp.Name} ne null))");
                }
            }
        }
    }

    /// <summary>
    /// $filter null handling: eq null, ne null, null propagation.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateNull(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Filter.Null;

        foreach (var prop in meta.NullableProperties.Take(4))
        {
            yield return new(es, cat, "eq_null",
                $"$filter={prop.Name} eq null",
                $"$filter={prop.Name} eq null");

            yield return new(es, cat, "ne_null",
                $"$filter={prop.Name} ne null",
                $"$filter={prop.Name} ne null");
        }

        // Null propagation in string functions
        foreach (var prop in meta.StringProperties.Where(p => p.IsNullable).Take(2))
        {
            yield return new(es, cat, "null_in_length",
                $"$filter=length({prop.Name}) gt 0",
                $"$filter=length({prop.Name}) gt 0");

            yield return new(es, cat, "null_in_contains",
                $"$filter=contains({prop.Name},'test')",
                $"$filter=contains({prop.Name},'test')");
        }

        // Nullable navigation property
        foreach (var nav in meta.SingleNavigations.Take(2))
        {
            yield return new(es, cat, "null_navigation",
                $"$filter={nav.Name} ne null",
                $"$filter={nav.Name} ne null");
        }
    }

    /// <summary>
    /// $filter 'in' operator for set membership.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateIn(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Filter.In;

        foreach (var prop in meta.StringProperties.Take(2))
        {
            yield return new(es, cat, "in_strings",
                $"$filter={prop.Name} in ('Alpha','Beta','Gamma')",
                $"$filter={prop.Name} in ('Alpha','Beta','Gamma')");
        }

        foreach (var prop in meta.NumericProperties.Where(p => !p.IsKey).Take(2))
        {
            yield return new(es, cat, "in_numbers",
                $"$filter={prop.Name} in (1,2,3,4,5)",
                $"$filter={prop.Name} in (1,2,3,4,5)");
        }

        foreach (var prop in meta.EnumProperties.Take(2))
        {
            yield return new(es, cat, "in_enums",
                $"$filter={prop.Name} in ({prop.SampleODataLiteral},{prop.SampleODataLiteral2})",
                $"$filter={prop.Name} in ({prop.SampleODataLiteral},{prop.SampleODataLiteral2})");
        }
    }

    /// <summary>
    /// $filter on navigation property scalar values.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateNavigationProperty(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Filter.NavigationProperty;

        foreach (var nav in meta.SingleNavigations)
        {
            foreach (var targetProp in nav.TargetProperties.Where(p => !p.IsKey).Take(3))
            {
                yield return new(es, cat, "nav_eq",
                    $"$filter={nav.Name}/{targetProp.Name} eq {targetProp.SampleODataLiteral}",
                    $"$filter={nav.Name}/{targetProp.Name} eq {targetProp.SampleODataLiteral}");
            }

            // String function on nav property
            foreach (var targetProp in nav.TargetProperties.Where(p => p.ClrType == typeof(string)).Take(2))
            {
                yield return new(es, cat, "nav_contains",
                    $"$filter=contains({nav.Name}/{targetProp.Name},'test')",
                    $"$filter=contains({nav.Name}/{targetProp.Name},'test')");
            }
        }

        // $count on collection navigation in filter
        foreach (var nav in meta.CollectionNavigations)
        {
            yield return new(es, cat, "nav_count_gt",
                $"$filter={nav.Name}/$count gt 0",
                $"$filter={nav.Name}/$count gt 0");

            yield return new(es, cat, "nav_count_eq",
                $"$filter={nav.Name}/$count eq 0",
                $"$filter={nav.Name}/$count eq 0");
        }
    }

    /// <summary>
    /// $filter with special characters and edge cases.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateSpecialCharacters(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Filter.SpecialCharacters;

        foreach (var prop in meta.StringProperties.Take(1))
        {
            // Single quote escaping
            yield return new(es, cat, "escaped_quote",
                $"$filter={prop.Name} eq 'O''Brien'",
                $"$filter={prop.Name} eq 'O''Brien'");

            // Empty string
            yield return new(es, cat, "empty_string",
                $"$filter={prop.Name} eq ''",
                $"$filter={prop.Name} eq ''");

            // String with spaces
            yield return new(es, cat, "string_with_spaces",
                $"$filter={prop.Name} eq 'hello world'",
                $"$filter={prop.Name} eq 'hello world'");

            // Unicode characters
            yield return new(es, cat, "unicode",
                $"$filter=contains({prop.Name},'caf%C3%A9')",
                $"$filter=contains({prop.Name},'caf%C3%A9')");
        }
    }
}
