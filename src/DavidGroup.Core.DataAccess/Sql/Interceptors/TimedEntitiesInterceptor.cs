using DavidGroup.Core.DataAccess.Sql.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DavidGroup.Core.DataAccess.Sql.Interceptors;

/// <summary>
/// Intercepts <see cref="DbContext.SaveChangesAsync(System.Threading.CancellationToken)"/> calls
/// to automatically manage timestamp fields for entities implementing the <see cref="ITimedEntity"/> interface.
/// </summary>
/// <remarks>
/// This interceptor ensures that entities are automatically assigned and updated with
/// <see cref="ITimedEntity.CreatedAtUtc"/> and <see cref="ITimedEntity.ModifiedAtUtc"/> values
/// whenever they are added or modified in the <see cref="DbContext"/>.
/// <para>
/// By using UTC timestamps, this approach maintains consistent time tracking across distributed systems
/// and different time zones.
/// </para>
/// </remarks>
public class TimedEntitiesInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Handles the logic.
    /// </summary>
    /// <param name="eventData"></param>
    /// <param name="result"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new())
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        IEnumerable<EntityEntry<ITimedEntity>> entries = eventData.Context.ChangeTracker.Entries<ITimedEntity>();

        foreach (EntityEntry<ITimedEntity> entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                    entry.Entity.ModifiedAtUtc = entry.Entity.CreatedAtUtc;
                    break;
                case EntityState.Modified:
                case EntityState.Deleted:
                    entry.Entity.ModifiedAtUtc = DateTime.UtcNow;
                    break;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
