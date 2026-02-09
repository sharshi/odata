using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.ModelBuilder;
using OData.Testing.Sample.Data;
using OData.Testing.Sample.Models;

namespace OData.Testing.Sample;

/// <summary>
/// Real Startup configuration that sets up OData controllers and middleware.
/// Used by ODataAppTests to test against the real application pipeline.
/// </summary>
public class SampleStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // In-memory database for testing
        services.AddDbContext<SampleDbContext>(opt =>
            opt.UseInMemoryDatabase($"SampleDb_{Guid.NewGuid():N}"));

        // MVC + OData with real controllers
        services.AddControllers()
            .AddApplicationPart(typeof(SampleStartup).Assembly)
            .AddOData(opt =>
            {
                var builder = new ODataConventionModelBuilder();
                builder.EntitySet<Product>("Products");
                builder.EntitySet<Category>("Categories");
                builder.EntitySet<Supplier>("Suppliers");
                builder.EntitySet<Order>("Orders");
                builder.EntitySet<OrderItem>("OrderItems");
                builder.EntitySet<Tag>("Tags");
                builder.EntitySet<ProductTag>("ProductTags");
                var model = builder.GetEdmModel();

                opt.AddRouteComponents("odata", model);
                opt.Select().Filter().Expand().OrderBy().Count().SetMaxTop(1000);
            });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
