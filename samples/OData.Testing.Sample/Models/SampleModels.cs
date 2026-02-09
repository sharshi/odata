namespace OData.Testing.Sample.Models;

/// <summary>
/// Sample entity models demonstrating various relationship types and property types
/// for comprehensive OData query testing.
/// </summary>

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public double Weight { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public bool? IsDiscontinued { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTimeOffset? ModifiedDate { get; set; }
    public ProductStatus Status { get; set; }
    public string? Sku { get; set; }
    public int? Rating { get; set; }

    // Many-to-one: Product -> Category
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // Many-to-one: Product -> Supplier
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    // One-to-many: Product -> OrderItems
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Many-to-many: Product -> Tags (via ProductTag join)
    public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }

    // Self-referencing: Category -> ParentCategory
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }

    // One-to-many: Category -> Products
    public ICollection<Product> Products { get; set; } = new List<Product>();

    // One-to-many: Category -> ChildCategories
    public ICollection<Category> ChildCategories { get; set; } = new List<Category>();
}

public class Supplier
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; }

    // One-to-many: Supplier -> Products
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public bool IsPriority { get; set; }

    // One-to-many: Order -> OrderItems
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? Discount { get; set; }
    public string? Notes { get; set; }

    // Many-to-one: OrderItem -> Order
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    // Many-to-one: OrderItem -> Product
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Many-to-many: Tag -> Products (via ProductTag join)
    public ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
}

/// <summary>
/// Explicit join table for Product-Tag many-to-many relationship.
/// </summary>
public class ProductTag
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}

// ── Enums ──

public enum ProductStatus
{
    Draft,
    Active,
    Discontinued,
    OutOfStock
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
