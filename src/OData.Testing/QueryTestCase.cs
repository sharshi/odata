using Xunit.Abstractions;

namespace OData.Testing;

/// <summary>
/// Represents a single OData query test case to be executed against an endpoint.
/// </summary>
public class QueryTestCase : IXunitSerializable
{
    /// <summary>
    /// Human-readable description of what this test case verifies.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The OData entity set name (e.g., "Products").
    /// </summary>
    public string EntitySet { get; set; } = string.Empty;

    /// <summary>
    /// The raw OData query string (e.g., "$filter=Price gt 10&amp;$orderby=Name").
    /// </summary>
    public string QueryString { get; set; } = string.Empty;

    /// <summary>
    /// The test category (e.g., "Filter.Comparison", "Expand.Nested").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The specific subcategory (e.g., "eq", "StringContains", "MultiLevel").
    /// </summary>
    public string SubCategory { get; set; } = string.Empty;

    /// <summary>
    /// The OData route prefix for this entity set (e.g., "odata", "v1/task").
    /// Used to build the request URL: /{RoutePrefix}/{EntitySet}?{QueryString}
    /// </summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>
    /// Whether this test expects a successful (2xx) response. Defaults to true.
    /// </summary>
    public bool ExpectSuccess { get; set; } = true;

    /// <summary>
    /// If set, the expected HTTP status code.
    /// </summary>
    public int? ExpectedStatusCode { get; set; }

    /// <summary>
    /// If true, the response must contain @odata.count.
    /// </summary>
    public bool ExpectCount { get; set; }

    /// <summary>
    /// If set, the maximum number of items expected in the response.
    /// </summary>
    public int? ExpectedMaxItems { get; set; }

    /// <summary>
    /// If set, the $select properties that should be the only ones in the response.
    /// </summary>
    public string[]? ExpectedProperties { get; set; }

    public QueryTestCase() { }

    public QueryTestCase(string entitySet, string category, string subCategory, string description, string queryString)
    {
        EntitySet = entitySet;
        Category = category;
        SubCategory = subCategory;
        Description = description;
        QueryString = queryString;
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        Description = info.GetValue<string>(nameof(Description));
        EntitySet = info.GetValue<string>(nameof(EntitySet));
        QueryString = info.GetValue<string>(nameof(QueryString));
        Category = info.GetValue<string>(nameof(Category));
        SubCategory = info.GetValue<string>(nameof(SubCategory));
        RoutePrefix = info.GetValue<string>(nameof(RoutePrefix));
        ExpectSuccess = info.GetValue<bool>(nameof(ExpectSuccess));
        ExpectCount = info.GetValue<bool>(nameof(ExpectCount));
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue(nameof(Description), Description);
        info.AddValue(nameof(EntitySet), EntitySet);
        info.AddValue(nameof(QueryString), QueryString);
        info.AddValue(nameof(Category), Category);
        info.AddValue(nameof(SubCategory), SubCategory);
        info.AddValue(nameof(RoutePrefix), RoutePrefix);
        info.AddValue(nameof(ExpectSuccess), ExpectSuccess);
        info.AddValue(nameof(ExpectCount), ExpectCount);
    }

    public override string ToString() => $"[{Category}.{SubCategory}] {Description}";
}
