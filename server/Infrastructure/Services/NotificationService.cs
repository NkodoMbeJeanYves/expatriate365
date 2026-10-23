using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Infrastructure.Services;

public class NotificationService(AppDbContext db, ILogger<NotificationService> log) : INotificationService
{
    public async Task NotifyAsync(Guid tenantId, Guid userId, string type, string title, string body, CancellationToken ct = default, string? entityType = null, string? entityId = null)
    {
        if (!await IsEnabledAsync(tenantId, userId, type, ct)) return;

        db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            EntityType = entityType,
            EntityId = entityId,
        });
        await db.SaveChangesAsync(ct);
        log.LogInformation("In-app notification [{Type}] → user {UserId}", type, userId);
    }

    public async Task NotifyWithEmailAsync(Guid tenantId, Guid userId, string type, string title, string body,
        string emailSubject, string emailHtml, CancellationToken ct = default, string? entityType = null, string? entityId = null)
    {
        if (!await IsEnabledAsync(tenantId, userId, type, ct)) return;

        db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            EntityType = entityType,
            EntityId = entityId,
        });

        // The login email (users.email) is a generated association address; only the
        // personal notification address (users.contact_email) may receive emails.
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.ContactEmail, u.FirstName, u.LastName, u.PreferredLanguage })
            .FirstOrDefaultAsync(ct);
        var queued = false;
        if (!string.IsNullOrWhiteSpace(user?.ContactEmail))
            queued = await EmailOutbox.EnqueueAsync(db, tenantId, userId, user.ContactEmail,
                $"{user.FirstName} {user.LastName}".Trim(), user.PreferredLanguage,
                EmailOutboxTemplates.PreRenderedHtml, new { subject = emailSubject, html = emailHtml },
                $"notify:{userId:N}:{type}:{entityId}:{Guid.NewGuid():N}", ct);

        await db.SaveChangesAsync(ct);
        log.LogInformation("In-app notification [{Type}] → user {UserId}; email {EmailState}", type, userId,
            queued ? $"queued for {EmailAddressMasking.Mask(user!.ContactEmail)}" : "not sent (no contact email)");
    }

    private async Task<bool> IsEnabledAsync(Guid tenantId, Guid userId, string type, CancellationToken ct)
    {
        var pref = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId && p.NotificationType == type, ct);
        // Default: enabled unless explicitly disabled
        return pref is not { Enabled: false };
    }
}
