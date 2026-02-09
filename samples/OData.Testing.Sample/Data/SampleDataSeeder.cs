using OData.Testing.Sample.Models;

namespace OData.Testing.Sample.Data;

/// <summary>
/// Seeds the database with comprehensive test data covering various data scenarios:
/// nulls, empty collections, multiple relationships, different enum values, etc.
/// </summary>
public static class SampleDataSeeder
{
    public static void Seed(SampleDbContext context)
    {
        // ── Categories (with self-referencing hierarchy) ──
        var rootCat = new Category { Id = 1, Name = "Electronics", Description = "Electronic devices", IsActive = true, CreatedDate = new DateTime(2023, 1, 1) };
        var subCat1 = new Category { Id = 2, Name = "Phones", Description = "Mobile phones", IsActive = true, ParentCategoryId = 1, CreatedDate = new DateTime(2023, 2, 1) };
        var subCat2 = new Category { Id = 3, Name = "Laptops", Description = "Portable computers", IsActive = true, ParentCategoryId = 1, CreatedDate = new DateTime(2023, 3, 1) };
        var catFood = new Category { Id = 4, Name = "Food", Description = "Food items", IsActive = true, CreatedDate = new DateTime(2023, 4, 1) };
        var catInactive = new Category { Id = 5, Name = "Deprecated", Description = null, IsActive = false, CreatedDate = new DateTime(2022, 1, 1) };

        context.Categories.AddRange(rootCat, subCat1, subCat2, catFood, catInactive);

        // ── Suppliers ──
        var supplier1 = new Supplier { Id = 1, CompanyName = "TechCorp", ContactName = "John Doe", Email = "john@techcorp.com", Phone = "555-0100", City = "Seattle", Country = "USA", IsActive = true };
        var supplier2 = new Supplier { Id = 2, CompanyName = "GadgetWorld", ContactName = "Jane Smith", Email = "jane@gadgetworld.com", Phone = "555-0200", City = "London", Country = "UK", IsActive = true };
        var supplier3 = new Supplier { Id = 3, CompanyName = "FreshFoods", ContactName = null, Email = null, Phone = null, City = "Paris", Country = "France", IsActive = false };

        context.Suppliers.AddRange(supplier1, supplier2, supplier3);

        // ── Tags ──
        var tag1 = new Tag { Id = 1, Name = "Sale", Description = "On sale items" };
        var tag2 = new Tag { Id = 2, Name = "Featured", Description = "Featured products" };
        var tag3 = new Tag { Id = 3, Name = "New", Description = null };
        var tag4 = new Tag { Id = 4, Name = "BestSeller", Description = "Top selling items" };

        context.Tags.AddRange(tag1, tag2, tag3, tag4);

        // ── Products (varied data for thorough testing) ──
        var products = new List<Product>
        {
            new() { Id = 1, Name = "iPhone 15", Description = "Latest Apple phone", Price = 999.99m, Weight = 0.17, StockQuantity = 50, IsActive = true, IsDiscontinued = false, CreatedDate = new DateTime(2024, 1, 15), ModifiedDate = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero), Status = ProductStatus.Active, Sku = "IPHONE-15", Rating = 5, CategoryId = 2, SupplierId = 1 },
            new() { Id = 2, Name = "Galaxy S24", Description = "Samsung flagship", Price = 899.50m, Weight = 0.19, StockQuantity = 30, IsActive = true, IsDiscontinued = false, CreatedDate = new DateTime(2024, 2, 10), ModifiedDate = new DateTimeOffset(2024, 5, 15, 0, 0, 0, TimeSpan.Zero), Status = ProductStatus.Active, Sku = "GAL-S24", Rating = 4, CategoryId = 2, SupplierId = 2 },
            new() { Id = 3, Name = "MacBook Pro", Description = "Professional laptop", Price = 2499.00m, Weight = 1.83, StockQuantity = 15, IsActive = true, IsDiscontinued = false, CreatedDate = new DateTime(2024, 3, 1), ModifiedDate = null, Status = ProductStatus.Active, Sku = "MBP-M3", Rating = 5, CategoryId = 3, SupplierId = 1 },
            new() { Id = 4, Name = "ThinkPad X1", Description = "Business laptop", Price = 1799.00m, Weight = 1.36, StockQuantity = 20, IsActive = true, IsDiscontinued = false, CreatedDate = new DateTime(2024, 3, 15), ModifiedDate = DateTimeOffset.UtcNow, Status = ProductStatus.Active, Sku = "TP-X1", Rating = 4, CategoryId = 3, SupplierId = 2 },
            new() { Id = 5, Name = "Organic Apples", Description = "Fresh organic apples", Price = 4.99m, Weight = 1.0, StockQuantity = 200, IsActive = true, IsDiscontinued = false, CreatedDate = new DateTime(2024, 4, 1), ModifiedDate = null, Status = ProductStatus.Active, Sku = "ORG-APL", Rating = null, CategoryId = 4, SupplierId = 3 },
            new() { Id = 6, Name = "Artisan Bread", Description = null, Price = 6.50m, Weight = 0.5, StockQuantity = 100, IsActive = true, IsDiscontinued = null, CreatedDate = new DateTime(2024, 4, 15), ModifiedDate = null, Status = ProductStatus.Active, Sku = null, Rating = 3, CategoryId = 4, SupplierId = 3 },
            new() { Id = 7, Name = "Old Phone", Description = "Discontinued phone model", Price = 299.00m, Weight = 0.2, StockQuantity = 0, IsActive = false, IsDiscontinued = true, CreatedDate = new DateTime(2022, 6, 1), ModifiedDate = new DateTimeOffset(2023, 12, 31, 23, 59, 59, TimeSpan.Zero), Status = ProductStatus.Discontinued, Sku = "OLD-PH", Rating = 2, CategoryId = 2, SupplierId = null },
            new() { Id = 8, Name = "Draft Gadget", Description = "Not yet released", Price = 0m, Weight = 0, StockQuantity = 0, IsActive = false, IsDiscontinued = null, CreatedDate = new DateTime(2024, 6, 1), ModifiedDate = null, Status = ProductStatus.Draft, Sku = null, Rating = null, CategoryId = 1, SupplierId = null },
            new() { Id = 9, Name = "Budget Earbuds", Description = "Affordable wireless earbuds", Price = 29.99m, Weight = 0.05, StockQuantity = 500, IsActive = true, IsDiscontinued = false, CreatedDate = new DateTime(2024, 5, 1), ModifiedDate = null, Status = ProductStatus.Active, Sku = "BUD-EB", Rating = 3, CategoryId = 1, SupplierId = 1 },
            new() { Id = 10, Name = "Premium Headphones", Description = "Noise-cancelling headphones", Price = 349.00m, Weight = 0.25, StockQuantity = 40, IsActive = true, IsDiscontinued = false, CreatedDate = new DateTime(2024, 5, 15), ModifiedDate = new DateTimeOffset(2024, 7, 1, 12, 0, 0, TimeSpan.Zero), Status = ProductStatus.Active, Sku = "PREM-HP", Rating = 5, CategoryId = 1, SupplierId = 2 },
        };

        context.Products.AddRange(products);

        // ── ProductTags ──
        context.ProductTags.AddRange(
            new ProductTag { Id = 1, ProductId = 1, TagId = 2 },  // iPhone -> Featured
            new ProductTag { Id = 2, ProductId = 1, TagId = 3 },  // iPhone -> New
            new ProductTag { Id = 3, ProductId = 2, TagId = 1 },  // Galaxy -> Sale
            new ProductTag { Id = 4, ProductId = 2, TagId = 2 },  // Galaxy -> Featured
            new ProductTag { Id = 5, ProductId = 3, TagId = 4 },  // MacBook -> BestSeller
            new ProductTag { Id = 6, ProductId = 5, TagId = 3 },  // Apples -> New
            new ProductTag { Id = 7, ProductId = 9, TagId = 1 },  // Earbuds -> Sale
            new ProductTag { Id = 8, ProductId = 10, TagId = 2 }, // Headphones -> Featured
            new ProductTag { Id = 9, ProductId = 10, TagId = 4 }  // Headphones -> BestSeller
        );

        // ── Orders ──
        var orders = new List<Order>
        {
            new() { Id = 1, OrderNumber = "ORD-001", CustomerName = "Alice Johnson", CustomerEmail = "alice@example.com", OrderDate = new DateTime(2024, 6, 1), ShippedDate = new DateTime(2024, 6, 3), TotalAmount = 1999.49m, Status = OrderStatus.Delivered, Notes = "Express shipping", IsPriority = true },
            new() { Id = 2, OrderNumber = "ORD-002", CustomerName = "Bob Wilson", CustomerEmail = "bob@example.com", OrderDate = new DateTime(2024, 6, 5), ShippedDate = new DateTime(2024, 6, 8), TotalAmount = 899.50m, Status = OrderStatus.Delivered, Notes = null, IsPriority = false },
            new() { Id = 3, OrderNumber = "ORD-003", CustomerName = "Charlie Brown", CustomerEmail = null, OrderDate = new DateTime(2024, 6, 10), ShippedDate = null, TotalAmount = 2528.99m, Status = OrderStatus.Processing, Notes = "Gift wrap requested", IsPriority = true },
            new() { Id = 4, OrderNumber = "ORD-004", CustomerName = "Diana Prince", CustomerEmail = "diana@example.com", OrderDate = new DateTime(2024, 6, 15), ShippedDate = null, TotalAmount = 34.98m, Status = OrderStatus.Pending, Notes = null, IsPriority = false },
            new() { Id = 5, OrderNumber = "ORD-005", CustomerName = "Eve Adams", CustomerEmail = "eve@example.com", OrderDate = new DateTime(2024, 6, 20), ShippedDate = null, TotalAmount = 0m, Status = OrderStatus.Cancelled, Notes = "Customer requested cancellation", IsPriority = false },
        };

        context.Orders.AddRange(orders);

        // ── OrderItems ──
        context.OrderItems.AddRange(
            // Order 1: iPhone + Galaxy
            new OrderItem { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 999.99m, Discount = null, Notes = null },
            new OrderItem { Id = 2, OrderId = 1, ProductId = 2, Quantity = 1, UnitPrice = 899.50m, Discount = 0.10m, Notes = "Bundle discount" },

            // Order 2: Galaxy
            new OrderItem { Id = 3, OrderId = 2, ProductId = 2, Quantity = 1, UnitPrice = 899.50m, Discount = null, Notes = null },

            // Order 3: MacBook + Earbuds
            new OrderItem { Id = 4, OrderId = 3, ProductId = 3, Quantity = 1, UnitPrice = 2499.00m, Discount = null, Notes = "For work" },
            new OrderItem { Id = 5, OrderId = 3, ProductId = 9, Quantity = 1, UnitPrice = 29.99m, Discount = null, Notes = null },

            // Order 4: Apples + Bread
            new OrderItem { Id = 6, OrderId = 4, ProductId = 5, Quantity = 3, UnitPrice = 4.99m, Discount = null, Notes = "Organic preferred" },
            new OrderItem { Id = 7, OrderId = 4, ProductId = 6, Quantity = 2, UnitPrice = 6.50m, Discount = 0.50m, Notes = null },

            // Order 5 (cancelled): no items delivered, but items were on the order
            new OrderItem { Id = 8, OrderId = 5, ProductId = 10, Quantity = 1, UnitPrice = 349.00m, Discount = null, Notes = "Cancelled before shipping" }
        );

        context.SaveChanges();
    }
}
