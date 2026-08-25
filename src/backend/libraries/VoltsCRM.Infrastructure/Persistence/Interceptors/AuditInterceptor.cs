using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using VoltsCRM.Domain.Common;
using VoltsCRM.Infrastructure.Auditing;

namespace VoltsCRM.Infrastructure.Persistence.Interceptors;

public class AuditInterceptor(TimeProvider timeProvider, IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Enforce append-only auditing at the persistence layer: audit rows may only ever be inserted.
        foreach (var audit in eventData.Context.ChangeTracker.Entries<AuditEvent>())
        {
            if (audit.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("Audit events are append-only and cannot be modified or deleted.");
        }

        foreach (var entry in eventData.Context.ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(nameof(Entity.CreatedAt)).CurrentValue = now;
                entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = now;
                entry.Property(nameof(Entity.CreatedById)).CurrentValue = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(Entity.UpdatedAt)).CurrentValue = now;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
