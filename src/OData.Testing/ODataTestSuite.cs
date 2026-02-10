using System.Reflection;
using Microsoft.OData.Edm;

namespace OData.Testing;

/// <summary>
/// Auto-discovers entity sets from OData EDM models and generates test cases
/// across all route components. Accepts your real OData setup and produces
/// full URLs covering $filter, $select, $expand, $orderby, $top, $skip,
/// $count, $apply, and $compute — at configurable density levels.
///
/// <para>Usage:</para>
/// <code>
/// var suite = ODataTestSuite.FromRouteComponents(rc =>
/// {
///     rc.Add("v1/client/payer/benefits", benefitsModel);
///     rc.Add("v1/task", taskModel);
///     rc.Add("v1", mainModel);
/// });
///
/// // Get full URLs at balanced density
/// foreach (var url in suite.GenerateUrls())
///     Console.WriteLine(url);
///
/// // Or get xUnit theory data for one [Theory] to exercise everything
/// public static IEnumerable&lt;object[]&gt; AllCases => suite.AsTheoryData();
/// </code>
/// </summary>
public class ODataTestSuite
{
    private readonly List<RouteComponent> _components = new();

    private ODataTestSuite() { }

    internal void AddComponent(string routePrefix, IEdmModel model, Assembly[]? assemblies)
    {
        _components.Add(new RouteComponent(routePrefix, model, assemblies));
    }

    /// <summary>
    /// Create a test suite from OData route components.
    /// Each component is a route prefix + EDM model pair (matching your AddRouteComponents calls).
    /// </summary>
    public static ODataTestSuite FromRouteComponents(Action<RouteComponentBuilder> configure)
    {
        var suite = new ODataTestSuite();
        var builder = new RouteComponentBuilder(suite);
        configure(builder);
        return suite;
    }

    /// <summary>
    /// Create a test suite from a single EDM model with one route prefix.
    /// </summary>
    public static ODataTestSuite FromModel(string routePrefix, IEdmModel model)
    {
        var suite = new ODataTestSuite();
        suite._components.Add(new RouteComponent(routePrefix, model, null));
        return suite;
    }

    // ── Generation ──

    /// <summary>
    /// Generate test cases across all route components and entity sets.
    /// Each test case includes the RoutePrefix, EntitySet, and QueryString.
    /// </summary>
    /// <param name="density">Controls tests per subcategory per entity. Defaults to Balanced (~3).</param>
    public IEnumerable<QueryTestCase> GenerateTestCases(TestDensity density = TestDensity.Balanced)
    {
        var generators = DiscoverGenerators();
        var allCases = generators.SelectMany(g => g.GenerateAll());
        return ApplyDensity(allCases, density);
    }

    /// <summary>
    /// Generate full request URLs across all route components and entity sets.
    /// Format: /{routePrefix}/{entitySet}?{queryString}
    /// </summary>
    /// <param name="density">Controls tests per subcategory per entity. Defaults to Balanced (~3).</param>
    public IEnumerable<string> GenerateUrls(TestDensity density = TestDensity.Balanced)
    {
        return GenerateTestCases(density).Select(BuildUrl);
    }

    /// <summary>
    /// Generate test cases as xUnit [MemberData] compatible format.
    /// </summary>
    /// <param name="density">Controls tests per subcategory per entity. Defaults to Balanced (~3).</param>
    public IEnumerable<object[]> AsTheoryData(TestDensity density = TestDensity.Balanced)
    {
        return GenerateTestCases(density).Select(tc => new object[] { tc });
    }

    /// <summary>
    /// Returns a summary of what this suite covers: route components, entity sets, and estimated test counts.
    /// </summary>
    public SuiteSummary Summarize(TestDensity density = TestDensity.Balanced)
    {
        var generators = DiscoverGenerators();
        var entitySets = new List<EntitySetInfo>();

        foreach (var gen in generators)
        {
            var meta = gen.Metadata;
            var cases = ApplyDensity(gen.GenerateAll(), density).ToList();
            var byCat = cases.GroupBy(tc => tc.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            entitySets.Add(new EntitySetInfo
            {
                RoutePrefix = meta.RoutePrefix,
                EntitySetName = meta.EntitySetName,
                PropertyCount = meta.ScalarProperties.Count,
                NavigationCount = meta.SingleNavigations.Count + meta.CollectionNavigations.Count,
                TestCaseCount = cases.Count,
                ByCategory = byCat
            });
        }

        return new SuiteSummary
        {
            RouteComponentCount = _components.Count,
            EntitySets = entitySets,
            TotalTestCases = entitySets.Sum(e => e.TestCaseCount)
        };
    }

    // ── Discovery ──

    private List<ODataQueryGenerator> DiscoverGenerators()
    {
        var generators = new List<ODataQueryGenerator>();

        foreach (var component in _components)
        {
            var container = component.Model.EntityContainer;
            if (container == null) continue;

            foreach (var entitySet in container.EntitySets())
            {
                var metadata = ExtractMetadata(entitySet, component);
                if (metadata != null)
                    generators.Add(ODataQueryGenerator.FromMetadata(metadata));
            }
        }

        return generators;
    }

    private static EntityMetadata? ExtractMetadata(IEdmEntitySet entitySet, RouteComponent component)
    {
        var edmEntityType = entitySet.EntityType();

        // Try to resolve CLR type (preferred — uses the full reflection-based extractor)
        var clrType = ResolveClrType(edmEntityType, component.SearchAssemblies);
        if (clrType != null)
        {
            var metadata = EntityMetadataExtractor.Extract(clrType, entitySet.Name);
            metadata.RoutePrefix = component.RoutePrefix;
            return metadata;
        }

        // Fallback: extract metadata directly from EDM model
        return ExtractFromEdm(entitySet, edmEntityType, component);
    }

    private static Type? ResolveClrType(IEdmEntityType edmType, Assembly[]? searchAssemblies)
    {
        var assemblies = searchAssemblies ?? AppDomain.CurrentDomain.GetAssemblies();
        var typeName = edmType.Name;
        var fullName = edmType.FullTypeName();

        return assemblies
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .FirstOrDefault(t => t.FullName == fullName || t.Name == typeName);
    }

    // ── EDM Fallback Extraction ──

    private static EntityMetadata ExtractFromEdm(IEdmEntitySet entitySet, IEdmEntityType edmType, RouteComponent component)
    {
        var metadata = new EntityMetadata
        {
            EntityType = typeof(object), // placeholder — no CLR type available
            EntitySetName = entitySet.Name,
            RoutePrefix = component.RoutePrefix
        };

        // Key properties
        foreach (var keyProp in edmType.Key())
        {
            var pm = EdmPropertyToMetadata(keyProp, isKey: true);
            metadata.KeyProperties.Add(pm);
            metadata.ScalarProperties.Add(pm);
        }

        // Structural (scalar) properties
        foreach (var prop in edmType.StructuralProperties())
        {
            if (metadata.KeyProperties.Any(k => k.Name == prop.Name)) continue;

            var pm = EdmPropertyToMetadata(prop, isKey: false);
            metadata.ScalarProperties.Add(pm);
        }

        // Navigation properties
        foreach (var nav in edmType.NavigationProperties())
        {
            var targetEdmType = nav.ToEntityType();
            var navMeta = new NavigationMetadata
            {
                Name = nav.Name,
                TargetType = typeof(object),
                IsCollection = nav.Type.IsCollection()
            };

            // Extract target properties (1 level deep)
            foreach (var targetProp in targetEdmType.StructuralProperties())
            {
                navMeta.TargetProperties.Add(EdmPropertyToMetadata(targetProp, isKey: false));
            }

            if (nav.Type.IsCollection())
                metadata.CollectionNavigations.Add(navMeta);
            else
                metadata.SingleNavigations.Add(navMeta);
        }

        return metadata;
    }

    private static PropertyMetadata EdmPropertyToMetadata(IEdmStructuralProperty prop, bool isKey)
    {
        var clrType = EdmTypeToClr(prop.Type);
        var isNullable = prop.Type.IsNullable;
        if (isNullable && clrType.IsValueType)
            clrType = typeof(Nullable<>).MakeGenericType(clrType);

        return new PropertyMetadata
        {
            Name = prop.Name,
            ClrType = clrType,
            IsNullable = isNullable,
            IsKey = isKey,
            SampleODataLiteral = GetEdmSampleLiteral(prop.Type, 1),
            SampleODataLiteral2 = GetEdmSampleLiteral(prop.Type, 2)
        };
    }

    private static Type EdmTypeToClr(IEdmTypeReference typeRef)
    {
        if (typeRef.IsEnum()) return typeof(string);
        if (!typeRef.IsPrimitive()) return typeof(string);

        var kind = typeRef.AsPrimitive().PrimitiveKind();
        return kind switch
        {
            EdmPrimitiveTypeKind.String => typeof(string),
            EdmPrimitiveTypeKind.Int32 => typeof(int),
            EdmPrimitiveTypeKind.Int64 => typeof(long),
            EdmPrimitiveTypeKind.Int16 => typeof(short),
            EdmPrimitiveTypeKind.Byte => typeof(byte),
            EdmPrimitiveTypeKind.Decimal => typeof(decimal),
            EdmPrimitiveTypeKind.Double => typeof(double),
            EdmPrimitiveTypeKind.Single => typeof(float),
            EdmPrimitiveTypeKind.Boolean => typeof(bool),
            EdmPrimitiveTypeKind.DateTimeOffset => typeof(DateTimeOffset),
            EdmPrimitiveTypeKind.Date => typeof(DateTime),
            EdmPrimitiveTypeKind.Guid => typeof(Guid),
            EdmPrimitiveTypeKind.Duration => typeof(TimeSpan),
            _ => typeof(string)
        };
    }

    private static string GetEdmSampleLiteral(IEdmTypeReference typeRef, int variant)
    {
        if (typeRef.IsEnum())
        {
            var enumType = typeRef.AsEnum().EnumDefinition();
            var members = enumType.Members.ToList();
            if (members.Count > 0)
            {
                var idx = Math.Min(variant - 1, members.Count - 1);
                return $"'{members[idx].Name}'";
            }
            return variant == 1 ? "'Value1'" : "'Value2'";
        }

        if (!typeRef.IsPrimitive())
            return variant == 1 ? "'test'" : "'other'";

        var kind = typeRef.AsPrimitive().PrimitiveKind();
        return kind switch
        {
            EdmPrimitiveTypeKind.String => variant == 1 ? "'test'" : "'other'",
            EdmPrimitiveTypeKind.Int32 or EdmPrimitiveTypeKind.Int64 or EdmPrimitiveTypeKind.Int16 or EdmPrimitiveTypeKind.Byte
                => variant == 1 ? "1" : "100",
            EdmPrimitiveTypeKind.Decimal or EdmPrimitiveTypeKind.Double or EdmPrimitiveTypeKind.Single
                => variant == 1 ? "10.5" : "99.99",
            EdmPrimitiveTypeKind.Boolean => variant == 1 ? "true" : "false",
            EdmPrimitiveTypeKind.DateTimeOffset => variant == 1 ? "2024-01-01T00:00:00Z" : "2024-12-31T23:59:59Z",
            EdmPrimitiveTypeKind.Date => variant == 1 ? "2024-01-01" : "2024-12-31",
            EdmPrimitiveTypeKind.Guid => variant == 1
                ? "00000000-0000-0000-0000-000000000001"
                : "00000000-0000-0000-0000-000000000002",
            _ => variant == 1 ? "1" : "2"
        };
    }

    // ── Density Control ──

    private static IEnumerable<QueryTestCase> ApplyDensity(IEnumerable<QueryTestCase> cases, TestDensity density)
    {
        if (density == TestDensity.Comprehensive)
            return cases;

        var maxPerSubCategory = (int)density;

        // Group by entity set + category + subcategory, take N from each group.
        // Generators produce cases from simple → complex, so taking first N gives good coverage.
        return cases
            .GroupBy(tc => $"{tc.EntitySet}|{tc.Category}|{tc.SubCategory}")
            .SelectMany(g => g.Take(maxPerSubCategory));
    }

    // ── URL Building ──

    private static string BuildUrl(QueryTestCase tc)
    {
        var prefix = tc.RoutePrefix?.TrimStart('/').TrimEnd('/');
        return string.IsNullOrEmpty(prefix)
            ? $"/{tc.EntitySet}?{tc.QueryString}"
            : $"/{prefix}/{tc.EntitySet}?{tc.QueryString}";
    }

    // ── Internal Types ──

    private record RouteComponent(string RoutePrefix, IEdmModel Model, Assembly[]? SearchAssemblies);
}

/// <summary>
/// Builder for adding OData route components to an <see cref="ODataTestSuite"/>.
/// Each call to Add corresponds to one AddRouteComponents call in your OData setup.
/// </summary>
public class RouteComponentBuilder
{
    private readonly ODataTestSuite _suite;
    internal RouteComponentBuilder(ODataTestSuite suite) => _suite = suite;

    /// <summary>
    /// Add a route component. Entity CLR types are resolved from all loaded assemblies.
    /// </summary>
    /// <param name="routePrefix">The route prefix (e.g., "v1/task", "odata").</param>
    /// <param name="model">The EDM model for this route component.</param>
    public RouteComponentBuilder Add(string routePrefix, IEdmModel model)
    {
        _suite.AddComponent(routePrefix, model, null);
        return this;
    }

    /// <summary>
    /// Add a route component with specific assemblies to search for CLR entity types.
    /// Use this when entity types are in a specific assembly that might not be loaded yet.
    /// </summary>
    /// <param name="routePrefix">The route prefix (e.g., "v1/task", "odata").</param>
    /// <param name="model">The EDM model for this route component.</param>
    /// <param name="assemblies">Assemblies to search for CLR entity types.</param>
    public RouteComponentBuilder Add(string routePrefix, IEdmModel model, params Assembly[] assemblies)
    {
        _suite.AddComponent(routePrefix, model, assemblies);
        return this;
    }
}

/// <summary>
/// Summary of test suite coverage.
/// </summary>
public class SuiteSummary
{
    public int RouteComponentCount { get; set; }
    public List<EntitySetInfo> EntitySets { get; set; } = new();
    public int TotalTestCases { get; set; }

    public override string ToString()
    {
        var lines = new List<string>
        {
            $"OData Test Suite: {RouteComponentCount} route component(s), {EntitySets.Count} entity set(s), {TotalTestCases} total test cases",
            ""
        };

        foreach (var es in EntitySets)
        {
            lines.Add($"  [{es.RoutePrefix}] {es.EntitySetName}: {es.TestCaseCount} tests ({es.PropertyCount} props, {es.NavigationCount} navs)");
            foreach (var cat in es.ByCategory.OrderBy(kvp => kvp.Key))
                lines.Add($"    {cat.Key}: {cat.Value}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
/// Info about one entity set in the test suite.
/// </summary>
public class EntitySetInfo
{
    public string RoutePrefix { get; set; } = string.Empty;
    public string EntitySetName { get; set; } = string.Empty;
    public int PropertyCount { get; set; }
    public int NavigationCount { get; set; }
    public int TestCaseCount { get; set; }
    public Dictionary<string, int> ByCategory { get; set; } = new();
}
