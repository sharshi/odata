namespace OData.Testing;

/// <summary>
/// Describes an entity type's structure for test case generation.
/// Extracted from EF Core model metadata or built manually.
/// </summary>
public class EntityMetadata
{
    /// <summary>
    /// The CLR type of the entity.
    /// </summary>
    public Type EntityType { get; set; } = null!;

    /// <summary>
    /// The OData entity set name (e.g., "Products").
    /// </summary>
    public string EntitySetName { get; set; } = string.Empty;

    /// <summary>
    /// The primary key property name(s).
    /// </summary>
    public List<PropertyMetadata> KeyProperties { get; set; } = new();

    /// <summary>
    /// Scalar (non-navigation) properties.
    /// </summary>
    public List<PropertyMetadata> ScalarProperties { get; set; } = new();

    /// <summary>
    /// Navigation properties pointing to single related entities.
    /// </summary>
    public List<NavigationMetadata> SingleNavigations { get; set; } = new();

    /// <summary>
    /// Navigation properties pointing to collections of related entities.
    /// </summary>
    public List<NavigationMetadata> CollectionNavigations { get; set; } = new();

    // Convenience accessors

    public IEnumerable<PropertyMetadata> StringProperties =>
        ScalarProperties.Where(p => p.ClrType == typeof(string));

    public IEnumerable<PropertyMetadata> NumericProperties =>
        ScalarProperties.Where(p => IsNumericType(p.ClrType));

    public IEnumerable<PropertyMetadata> DateTimeProperties =>
        ScalarProperties.Where(p =>
            p.ClrType == typeof(DateTime) ||
            p.ClrType == typeof(DateTimeOffset) ||
            p.ClrType == typeof(DateTime?) ||
            p.ClrType == typeof(DateTimeOffset?));

    public IEnumerable<PropertyMetadata> BoolProperties =>
        ScalarProperties.Where(p => p.ClrType == typeof(bool) || p.ClrType == typeof(bool?));

    public IEnumerable<PropertyMetadata> NullableProperties =>
        ScalarProperties.Where(p => p.IsNullable);

    public IEnumerable<PropertyMetadata> EnumProperties =>
        ScalarProperties.Where(p =>
            p.ClrType.IsEnum ||
            (Nullable.GetUnderlyingType(p.ClrType)?.IsEnum ?? false));

    public IEnumerable<PropertyMetadata> GuidProperties =>
        ScalarProperties.Where(p => p.ClrType == typeof(Guid) || p.ClrType == typeof(Guid?));

    public IEnumerable<PropertyMetadata> DecimalProperties =>
        ScalarProperties.Where(p =>
            p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?) ||
            p.ClrType == typeof(double) || p.ClrType == typeof(double?) ||
            p.ClrType == typeof(float) || p.ClrType == typeof(float?));

    private static bool IsNumericType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t == typeof(int) || t == typeof(long) || t == typeof(short) ||
               t == typeof(decimal) || t == typeof(double) || t == typeof(float) ||
               t == typeof(byte);
    }
}

/// <summary>
/// Metadata for a scalar property.
/// </summary>
public class PropertyMetadata
{
    public string Name { get; set; } = string.Empty;
    public Type ClrType { get; set; } = null!;
    public bool IsNullable { get; set; }
    public bool IsKey { get; set; }

    /// <summary>
    /// A sample value appropriate for this property type, formatted as an OData literal.
    /// </summary>
    public string SampleODataLiteral { get; set; } = string.Empty;

    /// <summary>
    /// A second sample value for range/inequality queries.
    /// </summary>
    public string SampleODataLiteral2 { get; set; } = string.Empty;
}

/// <summary>
/// Metadata for a navigation property.
/// </summary>
public class NavigationMetadata
{
    public string Name { get; set; } = string.Empty;
    public Type TargetType { get; set; } = null!;
    public bool IsCollection { get; set; }

    /// <summary>
    /// Properties on the target entity that can be used in nested $filter/$select/$orderby.
    /// </summary>
    public List<PropertyMetadata> TargetProperties { get; set; } = new();

    /// <summary>
    /// Sub-navigations on the target entity for multi-level expand.
    /// </summary>
    public List<NavigationMetadata> TargetNavigations { get; set; } = new();
}

/// <summary>
/// Extracts <see cref="EntityMetadata"/> from CLR types using reflection.
/// </summary>
public static class EntityMetadataExtractor
{
    /// <summary>
    /// Extract metadata from a CLR entity type using reflection.
    /// Looks for common conventions (Id property, navigation properties, etc.).
    /// </summary>
    public static EntityMetadata Extract(Type entityType, string entitySetName, int depth = 0)
    {
        var metadata = new EntityMetadata
        {
            EntityType = entityType,
            EntitySetName = entitySetName
        };

        var properties = entityType.GetProperties(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var propType = prop.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(propType);
            var isNullable = underlyingType != null ||
                             !propType.IsValueType ||
                             (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>));

            // Check if it's a navigation property (collection or entity reference)
            if (IsCollectionNavigation(propType))
            {
                var targetType = GetCollectionElementType(propType);
                if (targetType != null && IsEntityType(targetType))
                {
                    var nav = new NavigationMetadata
                    {
                        Name = prop.Name,
                        TargetType = targetType,
                        IsCollection = true
                    };

                    if (depth < 2)
                    {
                        var targetMeta = Extract(targetType, string.Empty, depth + 1);
                        nav.TargetProperties = targetMeta.ScalarProperties;
                        nav.TargetNavigations = targetMeta.SingleNavigations
                            .Concat<NavigationMetadata>(targetMeta.CollectionNavigations)
                            .ToList();
                    }

                    metadata.CollectionNavigations.Add(nav);
                    continue;
                }
            }

            if (IsSingleNavigation(propType))
            {
                var nav = new NavigationMetadata
                {
                    Name = prop.Name,
                    TargetType = propType,
                    IsCollection = false
                };

                if (depth < 2)
                {
                    var targetMeta = Extract(propType, string.Empty, depth + 1);
                    nav.TargetProperties = targetMeta.ScalarProperties;
                    nav.TargetNavigations = targetMeta.SingleNavigations
                        .Concat<NavigationMetadata>(targetMeta.CollectionNavigations)
                        .ToList();
                }

                metadata.SingleNavigations.Add(nav);
                continue;
            }

            // Scalar property
            var effectiveType = underlyingType ?? propType;
            var isKey = IsKeyProperty(prop.Name, entityType.Name);

            var pm = new PropertyMetadata
            {
                Name = prop.Name,
                ClrType = propType,
                IsNullable = isNullable && !propType.IsValueType || underlyingType != null,
                IsKey = isKey,
                SampleODataLiteral = GetSampleLiteral(effectiveType, 1),
                SampleODataLiteral2 = GetSampleLiteral(effectiveType, 2)
            };

            metadata.ScalarProperties.Add(pm);

            if (isKey)
                metadata.KeyProperties.Add(pm);
        }

        return metadata;
    }

    private static bool IsKeyProperty(string propertyName, string entityName)
    {
        return propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals($"{entityName}Id", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCollectionNavigation(Type type)
    {
        if (type == typeof(string)) return false;
        return type.IsGenericType &&
               (type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                type.GetGenericTypeDefinition() == typeof(IList<>) ||
                type.GetGenericTypeDefinition() == typeof(List<>) ||
                type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                type.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>)));
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type.IsGenericType)
        {
            var args = type.GetGenericArguments();
            if (args.Length == 1) return args[0];
        }

        var collInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>));
        return collInterface?.GetGenericArguments()[0];
    }

    private static bool IsSingleNavigation(Type type)
    {
        return IsEntityType(type);
    }

    private static bool IsEntityType(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
            type == typeof(Guid) || type == typeof(TimeSpan) ||
            type.IsEnum || type == typeof(byte[]))
            return false;

        if (Nullable.GetUnderlyingType(type) != null)
            return false;

        // Heuristic: entity types are classes with an Id-like property
        return type.IsClass &&
               type.GetProperties().Any(p =>
                   p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                   p.Name.Equals($"{type.Name}Id", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetSampleLiteral(Type type, int variant)
    {
        if (type == typeof(string)) return variant == 1 ? "'test'" : "'other'";
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            return variant == 1 ? "1" : "100";
        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            return variant == 1 ? "10.5" : "99.99";
        if (type == typeof(bool)) return variant == 1 ? "true" : "false";
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return variant == 1 ? "2024-01-01T00:00:00Z" : "2024-12-31T23:59:59Z";
        if (type == typeof(Guid))
            return variant == 1
                ? "00000000-0000-0000-0000-000000000001"
                : "00000000-0000-0000-0000-000000000002";
        if (type.IsEnum)
        {
            var values = Enum.GetNames(type);
            var idx = Math.Min(variant - 1, values.Length - 1);
            return $"'{values[idx]}'";
        }

        return variant == 1 ? "1" : "2";
    }
}
