using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class DeleteInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Synchronous SaveChanges method override.
    /// Intercepts the SaveChanges call to set the ModifiedBy property for relevant entities.
    /// </summary>
    /// <param name="eventData">Event data containing the DbContext.</param>
    /// <param name="result">The result of the SaveChanges operation.</param>
    /// <returns>The result of the SaveChanges operation.</returns>
    public override int SaveChanges(DbContextEventData eventData, int result)
    {
        var context = eventData.Context;
        var entries = GetRelevantEntries(context);

        foreach (var entry in entries)
        {
            SetModifiedBy(entry.Entity);
            if (entry.State == EntityState.Deleted)
            {
                LoadAndSetModifiedByForCascadeDeleteableEntities(entry.Entity, context);
            }
        }

        return base.SaveChanges(eventData, result);
    }

    /// <summary>
    /// Asynchronous SaveChangesAsync method override.
    /// Intercepts the SaveChangesAsync call to set the ModifiedBy property for relevant entities.
    /// </summary>
    /// <param name="eventData">Event data containing the DbContext.</param>
    /// <param name="result">The result of the SaveChangesAsync operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the SaveChangesAsync operation.</returns>
    public override async Task<int> SaveChangesAsync(DbContextEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        var entries = GetRelevantEntries(context);

        foreach (var entry in entries)
        {
            SetModifiedBy(entry.Entity);
            if (entry.State == EntityState.Deleted)
            {
                await LoadAndSetModifiedByForCascadeDeleteableEntitiesAsync(entry.Entity, context, cancellationToken);
            }
        }

        return await base.SaveChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Retrieves entries that are either Added, Modified, or Deleted.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <returns>A collection of relevant EntityEntry objects.</returns>
    private IEnumerable<EntityEntry> GetRelevantEntries(DbContext context)
    {
        return context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Deleted || e.State == EntityState.Modified || e.State == EntityState.Added);
    }

    /// <summary>
    /// Sets the ModifiedBy property of an entity to a specific value.
    /// </summary>
    /// <param name="entity">The entity whose ModifiedBy property is to be set.</param>
    private void SetModifiedBy(object entity)
    {
        var modifiedByProperty = entity.GetType().GetProperty("ModifiedBy");
        if (modifiedByProperty != null)
        {
            modifiedByProperty.SetValue(entity, 1);
        }
    }

    /// <summary>
    /// Loads related entities for cascade delete and sets their ModifiedBy property.
    /// </summary>
    /// <param name="entity">The entity being deleted.</param>
    /// <param name="context">The DbContext instance.</param>
    private void LoadAndSetModifiedByForCascadeDeleteableEntities(object entity, DbContext context)
    {
        var navigationProperties = context.Entry(entity).Navigations;

        foreach (var navigation in navigationProperties)
        {
            if (!navigation.IsLoaded)
            {
                navigation.Load(); 
            }

            SetModifiedByForRelatedEntities(navigation);
        }
    }

    /// <summary>
    /// Asynchronously loads related entities for cascade delete and sets their ModifiedBy property.
    /// </summary>
    /// <param name="entity">The entity being deleted.</param>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task LoadAndSetModifiedByForCascadeDeleteableEntitiesAsync(object entity, DbContext context, CancellationToken cancellationToken)
    {
        var navigationProperties = context.Entry(entity).Navigations;

        foreach (var navigation in navigationProperties)
        {
            if (!navigation.IsLoaded)
            {
                await navigation.LoadAsync(cancellationToken);
            }

            SetModifiedByForRelatedEntities(navigation);
        }
    }

    /// <summary>
    /// Sets the ModifiedBy property for related entities.
    /// </summary>
    /// <param name="navigation">The navigation entry containing related entities.</param>
    private void SetModifiedByForRelatedEntities(NavigationEntry navigation)
    {
        if (navigation.Metadata.IsCollection())
        {
            var relatedEntities = navigation.CurrentValue as IEnumerable<object>;
            if (relatedEntities != null)
            {
                foreach (var relatedEntity in relatedEntities)
                {
                    SetModifiedBy(relatedEntity);
                }
            }
        }
        else
        {
            var relatedEntity = navigation.CurrentValue;
            if (relatedEntity != null)
            {
                SetModifiedBy(relatedEntity);
            }
        }
    }
}
