namespace OData.Testing.Generators;

/// <summary>
/// Generates $select test cases: single property, multiple properties,
/// wildcard, navigation property, nested complex type selection.
/// </summary>
public static class SelectGenerator
{
    public static IEnumerable<QueryTestCase> GenerateAll(EntityMetadata meta)
    {
        return GenerateSingle(meta)
            .Concat(GenerateMultiple(meta))
            .Concat(GenerateWildcard(meta))
            .Concat(GenerateNavigationProperty(meta))
            .Concat(GenerateNestedComplex(meta));
    }

    /// <summary>
    /// $select with a single property.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateSingle(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Select.Single;

        foreach (var prop in meta.ScalarProperties)
        {
            yield return new(es, cat, prop.Name,
                $"$select={prop.Name}",
                $"$select={prop.Name}")
            {
                ExpectedProperties = new[] { prop.Name }
            };
        }
    }

    /// <summary>
    /// $select with multiple properties.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateMultiple(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Select.Multiple;

        var props = meta.ScalarProperties.ToList();

        // Select first two
        if (props.Count >= 2)
        {
            var names = string.Join(",", props.Take(2).Select(p => p.Name));
            yield return new(es, cat, "two_properties",
                $"$select={names}",
                $"$select={names}")
            {
                ExpectedProperties = props.Take(2).Select(p => p.Name).ToArray()
            };
        }

        // Select first three
        if (props.Count >= 3)
        {
            var names = string.Join(",", props.Take(3).Select(p => p.Name));
            yield return new(es, cat, "three_properties",
                $"$select={names}",
                $"$select={names}")
            {
                ExpectedProperties = props.Take(3).Select(p => p.Name).ToArray()
            };
        }

        // Select all scalar properties (explicit, not wildcard)
        if (props.Count > 3)
        {
            var names = string.Join(",", props.Select(p => p.Name));
            yield return new(es, cat, "all_scalar_explicit",
                $"$select={names}",
                $"$select={names}");
        }

        // Select mix of key + non-key
        var key = meta.KeyProperties.FirstOrDefault();
        var nonKey = meta.ScalarProperties.FirstOrDefault(p => !p.IsKey);
        if (key != null && nonKey != null)
        {
            yield return new(es, cat, "key_and_nonkey",
                $"$select={key.Name},{nonKey.Name}",
                $"$select={key.Name},{nonKey.Name}")
            {
                ExpectedProperties = new[] { key.Name, nonKey.Name }
            };
        }
    }

    /// <summary>
    /// $select=* (wildcard).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateWildcard(EntityMetadata meta)
    {
        yield return new(meta.EntitySetName, ODataTestCategory.Select.Wildcard, "wildcard",
            "$select=*",
            "$select=*");
    }

    /// <summary>
    /// $select including navigation property names.
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateNavigationProperty(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Select.NavigationProperty;

        foreach (var nav in meta.SingleNavigations)
        {
            // Select a navigation property (requires $expand to actually load it)
            yield return new(es, cat, $"nav_{nav.Name}",
                $"$select={nav.Name}&$expand={nav.Name}",
                $"$select={nav.Name}&$expand={nav.Name}");

            // Select scalar + navigation
            var firstProp = meta.ScalarProperties.FirstOrDefault();
            if (firstProp != null)
            {
                yield return new(es, cat, $"scalar_and_nav_{nav.Name}",
                    $"$select={firstProp.Name},{nav.Name}&$expand={nav.Name}",
                    $"$select={firstProp.Name},{nav.Name}&$expand={nav.Name}");
            }
        }
    }

    /// <summary>
    /// $select on nested/complex type properties (if nav target has sub-properties).
    /// </summary>
    public static IEnumerable<QueryTestCase> GenerateNestedComplex(EntityMetadata meta)
    {
        var es = meta.EntitySetName;
        var cat = ODataTestCategory.Select.NestedComplex;

        // $expand with nested $select
        foreach (var nav in meta.SingleNavigations)
        {
            foreach (var targetProp in nav.TargetProperties.Where(p => !p.IsKey).Take(3))
            {
                yield return new(es, cat, $"expand_select_{nav.Name}_{targetProp.Name}",
                    $"$expand={nav.Name}($select={targetProp.Name})",
                    $"$expand={nav.Name}($select={targetProp.Name})");
            }

            // Multiple nested select
            var twoProps = nav.TargetProperties.Where(p => !p.IsKey).Take(2).ToList();
            if (twoProps.Count == 2)
            {
                var names = string.Join(",", twoProps.Select(p => p.Name));
                yield return new(es, cat, $"expand_multiselect_{nav.Name}",
                    $"$expand={nav.Name}($select={names})",
                    $"$expand={nav.Name}($select={names})");
            }
        }

        foreach (var nav in meta.CollectionNavigations)
        {
            foreach (var targetProp in nav.TargetProperties.Where(p => !p.IsKey).Take(2))
            {
                yield return new(es, cat, $"expand_collection_select_{nav.Name}_{targetProp.Name}",
                    $"$expand={nav.Name}($select={targetProp.Name})",
                    $"$expand={nav.Name}($select={targetProp.Name})");
            }
        }
    }
}
