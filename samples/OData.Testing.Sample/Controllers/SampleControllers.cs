using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using OData.Testing.Sample.Data;
using OData.Testing.Sample.Models;

namespace OData.Testing.Sample.Controllers;

/// <summary>
/// Real OData controllers used with ODataAppTests.
/// These are the actual controllers that would exist in your application.
/// </summary>

public class ProductsController : ODataController
{
    private readonly SampleDbContext _db;
    public ProductsController(SampleDbContext db) => _db = db;

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000, MaxAnyAllExpressionDepth = 5, MaxNodeCount = 200)]
    public IActionResult Get() => Ok(_db.Products);

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000)]
    public IActionResult Get(int key) => _db.Products.Find(key) is { } p ? Ok(p) : NotFound();
}

public class CategoriesController : ODataController
{
    private readonly SampleDbContext _db;
    public CategoriesController(SampleDbContext db) => _db = db;

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000, MaxAnyAllExpressionDepth = 5, MaxNodeCount = 200)]
    public IActionResult Get() => Ok(_db.Categories);

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000)]
    public IActionResult Get(int key) => _db.Categories.Find(key) is { } c ? Ok(c) : NotFound();
}

public class SuppliersController : ODataController
{
    private readonly SampleDbContext _db;
    public SuppliersController(SampleDbContext db) => _db = db;

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000, MaxAnyAllExpressionDepth = 5, MaxNodeCount = 200)]
    public IActionResult Get() => Ok(_db.Suppliers);

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000)]
    public IActionResult Get(int key) => _db.Suppliers.Find(key) is { } s ? Ok(s) : NotFound();
}

public class OrdersController : ODataController
{
    private readonly SampleDbContext _db;
    public OrdersController(SampleDbContext db) => _db = db;

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000, MaxAnyAllExpressionDepth = 5, MaxNodeCount = 200)]
    public IActionResult Get() => Ok(_db.Orders);

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000)]
    public IActionResult Get(int key) => _db.Orders.Find(key) is { } o ? Ok(o) : NotFound();
}

public class OrderItemsController : ODataController
{
    private readonly SampleDbContext _db;
    public OrderItemsController(SampleDbContext db) => _db = db;

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000, MaxAnyAllExpressionDepth = 5, MaxNodeCount = 200)]
    public IActionResult Get() => Ok(_db.OrderItems);

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000)]
    public IActionResult Get(int key) => _db.OrderItems.Find(key) is { } oi ? Ok(oi) : NotFound();
}

public class TagsController : ODataController
{
    private readonly SampleDbContext _db;
    public TagsController(SampleDbContext db) => _db = db;

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000, MaxAnyAllExpressionDepth = 5, MaxNodeCount = 200)]
    public IActionResult Get() => Ok(_db.Tags);

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000)]
    public IActionResult Get(int key) => _db.Tags.Find(key) is { } t ? Ok(t) : NotFound();
}

public class ProductTagsController : ODataController
{
    private readonly SampleDbContext _db;
    public ProductTagsController(SampleDbContext db) => _db = db;

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000, MaxAnyAllExpressionDepth = 5, MaxNodeCount = 200)]
    public IActionResult Get() => Ok(_db.ProductTags);

    [EnableQuery(MaxExpansionDepth = 10, MaxTop = 1000)]
    public IActionResult Get(int key) => _db.ProductTags.Find(key) is { } pt ? Ok(pt) : NotFound();
}
