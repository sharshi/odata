### DeleteInterceptor

#### Usage

The `DeleteInterceptor` class is designed to intercept the `SaveChanges` and `SaveChangesAsync` methods of a `DbContext` to perform additional logic before the changes are saved to the database. Specifically, it sets the `ModifiedBy` property of entities that are being added, modified, or deleted. For entities that are being deleted, it also loads related entities and sets their `ModifiedBy` property.

#### Example Scenario

Imagine you have an application where you need to track who modified or deleted records. You have an entity called `Order` with a `ModifiedBy` property. When an `Order` is deleted, you also want to update the `ModifiedBy` property of related `OrderItem` entities.

```csharp
public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public int ModifiedBy { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public string ProductName { get; set; }
    public int ModifiedBy { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
}

public class ApplicationDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("YourConnectionString");
        optionsBuilder.AddInterceptors(new DeleteInterceptor());
    }
}
```

In this example, when an `Order` is deleted, the `DeleteInterceptor` will ensure that the `ModifiedBy` property of the `Order` and its related `OrderItem` entities are set to a specific value (e.g., `1`).

#### Explanation

1. **SaveChanges and SaveChangesAsync Overrides**:
   - These methods intercept the `SaveChanges` and `SaveChangesAsync` calls to perform additional logic before the changes are saved to the database.
   - They retrieve the relevant entries (added, modified, or deleted) and set the `ModifiedBy` property for each entity.
   - For deleted entities, they load related entities and set their `ModifiedBy` property.

2. **GetRelevantEntries Method**:
   - This method retrieves entries from the `ChangeTracker` that are either added, modified, or deleted.

3. **SetModifiedBy Method**:
   - This method sets the `ModifiedBy` property of an entity to a specific value (e.g., `1`).

4. **LoadAndSetModifiedByForCascadeDeleteableEntities and Async Version**:
   - These methods load related entities for cascade delete and set their `ModifiedBy` property.
   - They iterate through the navigation properties of the entity and load them if they are not already loaded.

5. **SetModifiedByForRelatedEntities Method**:
   - This method sets the `ModifiedBy` property for related entities.
   - It handles both collection and single navigation properties.
