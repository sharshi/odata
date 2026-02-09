using System.Net;
using System.Text.Json;
using OData.Testing.Infrastructure;
using Xunit;

namespace OData.Testing.Assertions;

/// <summary>
/// Assertion helpers for validating OData HTTP responses.
/// Verifies status codes, JSON structure, OData conventions, and data constraints.
/// </summary>
public static class ODataAssert
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Fully validates an OData response against the test case expectations.
    /// </summary>
    public static async Task ValidateResponseAsync(
        HttpResponseMessage response,
        QueryTestCase testCase,
        ClientEvaluationInterceptor? clientEvalInterceptor = null)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (testCase.ExpectSuccess)
        {
            AssertSuccessStatusCode(response, testCase, content);
        }
        else if (testCase.ExpectedStatusCode.HasValue)
        {
            Assert.Equal(testCase.ExpectedStatusCode.Value, (int)response.StatusCode);
            return; // No further validation for expected error responses
        }

        // Parse JSON
        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(content);
        }
        catch (JsonException ex)
        {
            Assert.Fail($"Response is not valid JSON for query [{testCase}]: {ex.Message}\nResponse: {Truncate(content, 500)}");
        }

        using (doc)
        {
            var root = doc!.RootElement;

            // Validate OData structure
            ValidateODataStructure(root, testCase, content);

            // Validate $count
            if (testCase.ExpectCount)
            {
                ValidateInlineCount(root, testCase);
            }

            // Validate $top constraint
            if (testCase.ExpectedMaxItems.HasValue)
            {
                ValidateMaxItems(root, testCase);
            }

            // Validate $select
            if (testCase.ExpectedProperties != null && testCase.ExpectedProperties.Length > 0)
            {
                ValidateSelectedProperties(root, testCase);
            }
        }

        // Validate no client-side evaluation
        if (clientEvalInterceptor != null)
        {
            ValidateNoClientEvaluation(clientEvalInterceptor, testCase);
        }
    }

    /// <summary>
    /// Assert that the response has a success (2xx) status code.
    /// Provides detailed error information on failure.
    /// </summary>
    public static void AssertSuccessStatusCode(
        HttpResponseMessage response, QueryTestCase testCase, string? content = null)
    {
        if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
            return;

        var body = content ?? response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var errorDetail = ExtractODataError(body);

        Assert.Fail(
            $"OData query failed with HTTP {(int)response.StatusCode} {response.StatusCode}.\n" +
            $"Test: {testCase}\n" +
            $"Query: {testCase.QueryString}\n" +
            $"Error: {errorDetail}\n" +
            $"Full response: {Truncate(body, 1000)}");
    }

    /// <summary>
    /// Validates the response has proper OData collection structure (value array).
    /// </summary>
    public static void ValidateODataStructure(JsonElement root, QueryTestCase testCase, string content)
    {
        // Collection responses should have a "value" array
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("value", out var valueElement))
        {
            Assert.True(valueElement.ValueKind == JsonValueKind.Array,
                $"'value' property should be an array for query [{testCase}].\nResponse: {Truncate(content, 500)}");
        }
        // Single entity responses or aggregation results might not have "value"
        // That's OK for $apply queries
    }

    /// <summary>
    /// Validates @odata.count is present when $count=true is used.
    /// </summary>
    public static void ValidateInlineCount(JsonElement root, QueryTestCase testCase)
    {
        // OData uses @odata.count or @count depending on format version
        var hasCount = root.TryGetProperty("@odata.count", out var countElement) ||
                       root.TryGetProperty("@count", out countElement);

        Assert.True(hasCount,
            $"Expected @odata.count in response for query [{testCase}] with $count=true.");

        if (hasCount)
        {
            Assert.True(countElement.ValueKind == JsonValueKind.Number,
                $"@odata.count should be a number for query [{testCase}].");

            var count = countElement.GetInt64();
            Assert.True(count >= 0,
                $"@odata.count should be >= 0 for query [{testCase}], got {count}.");
        }
    }

    /// <summary>
    /// Validates the result count does not exceed ExpectedMaxItems.
    /// </summary>
    public static void ValidateMaxItems(JsonElement root, QueryTestCase testCase)
    {
        if (!root.TryGetProperty("value", out var valueElement)) return;
        if (valueElement.ValueKind != JsonValueKind.Array) return;

        var actualCount = valueElement.GetArrayLength();
        Assert.True(actualCount <= testCase.ExpectedMaxItems!.Value,
            $"Expected at most {testCase.ExpectedMaxItems} items but got {actualCount} for query [{testCase}].");
    }

    /// <summary>
    /// Validates that $select properly restricts the returned properties.
    /// </summary>
    public static void ValidateSelectedProperties(JsonElement root, QueryTestCase testCase)
    {
        if (!root.TryGetProperty("value", out var valueElement)) return;
        if (valueElement.ValueKind != JsonValueKind.Array) return;
        if (valueElement.GetArrayLength() == 0) return;

        var firstItem = valueElement[0];
        if (firstItem.ValueKind != JsonValueKind.Object) return;

        var actualProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in firstItem.EnumerateObject())
        {
            // Skip OData metadata properties
            if (prop.Name.StartsWith("@") || prop.Name.StartsWith("odata."))
                continue;
            actualProperties.Add(prop.Name);
        }

        foreach (var expectedProp in testCase.ExpectedProperties!)
        {
            Assert.True(actualProperties.Contains(expectedProp),
                $"Expected property '{expectedProp}' not found in response for query [{testCase}]. " +
                $"Actual properties: [{string.Join(", ", actualProperties)}]");
        }
    }

    /// <summary>
    /// Validates that no client-side evaluation occurred during the query.
    /// </summary>
    public static void ValidateNoClientEvaluation(
        ClientEvaluationInterceptor interceptor, QueryTestCase testCase)
    {
        Assert.False(interceptor.HasClientEvaluation,
            $"Client-side evaluation detected for query [{testCase}].\n" +
            interceptor.GetSummary());
    }

    /// <summary>
    /// Extracts error message from OData error response body.
    /// </summary>
    private static string ExtractODataError(string body)
    {
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var errorObj))
            {
                if (errorObj.TryGetProperty("message", out var message))
                    return message.GetString() ?? "Unknown error";
                return errorObj.ToString();
            }

            if (doc.RootElement.TryGetProperty("title", out var title))
                return title.GetString() ?? "Unknown error";
        }
        catch { }

        return Truncate(body, 300);
    }

    private static string Truncate(string s, int maxLength)
    {
        return s.Length <= maxLength ? s : s[..maxLength] + "... (truncated)";
    }
}
