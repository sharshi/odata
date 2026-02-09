using Microsoft.Extensions.DependencyInjection;
using OData.Testing.Sample.Data;
using OData.Testing.Sample.Models;
using Xunit;

namespace OData.Testing.Sample.Tests;

/// <summary>
/// Demonstrates ODataAppTests — point it at your real Startup class and
/// provide one [Theory] to exercise the entire OData pipeline.
///
/// This approach uses your actual controllers, middleware, and OData configuration.
/// The package generates hundreds of query permutations across $filter, $select,
/// $expand, $orderby, $top, $skip, $count, $apply, and $compute.
/// </summary>
public class AppODataTests : ODataAppTests<SampleStartup>
{
    protected override void SeedData(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
        context.Database.EnsureCreated();
        SampleDataSeeder.Seed(context);
    }

    // ── One test to exercise everything ──

    [Theory]
    [MemberData(nameof(AllCases))]
    public Task AllQueries(QueryTestCase tc) => RunTestAsync(tc);

    // ── Generate test cases for all entity sets ──

    public static IEnumerable<object[]> AllCases =>
        ODataQueryGenerator.AsTheoryData(
            ODataQueryGenerator.ForEntity<Product>("Products", "odata").GenerateAll()
                .Concat(ODataQueryGenerator.ForEntity<Category>("Categories", "odata").GenerateAll())
                .Concat(ODataQueryGenerator.ForEntity<Order>("Orders", "odata").GenerateAll()));
}
