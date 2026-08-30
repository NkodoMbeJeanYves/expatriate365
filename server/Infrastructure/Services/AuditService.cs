using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Infrastructure.Services;

public class AuditService(AppDbContext db)
{
    public void Log(string action, Guid userId, Guid? tenantId = null,
                    string? entityType = null, string? entityId = null, string? meta = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id         = Guid.NewGuid(),
            Action     = action,
            UserId     = userId,
            TenantId   = tenantId,
            EntityType = entityType,
            EntityId   = entityId,
            Meta       = meta,
            CreatedAt  = DateTime.UtcNow,
        });
    }
}
