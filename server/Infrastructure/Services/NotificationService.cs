using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Infrastructure.Services;

public class NotificationService(AppDbContext db, IEmailService email, ILogger<NotificationService> log) : INotificationService
{
    public async Task NotifyAsync(Guid tenantId, Guid userId, string type, string title, string body, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(tenantId, userId, type, ct)) return;

        db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
        });
        await db.SaveChangesAsync(ct);
        log.LogInformation("In-app notification [{Type}] → user {UserId}", type, userId);
    }

    public async Task NotifyWithEmailAsync(Guid tenantId, Guid userId, string type, string title, string body,
        string emailSubject, string emailHtml, CancellationToken ct = default)
    {
        if (!await IsEnabledAsync(tenantId, userId, type, ct)) return;

        db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
        });
        await db.SaveChangesAsync(ct);
        log.LogInformation("In-app notification [{Type}] → user {UserId}", type, userId);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is not null)
            await email.SendAsync(user.Email, $"{user.FirstName} {user.LastName}", emailSubject, emailHtml, ct);
    }

    private async Task<bool> IsEnabledAsync(Guid tenantId, Guid userId, string type, CancellationToken ct)
    {
        var pref = await db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.UserId == userId && p.NotificationType == type, ct);
        // Default: enabled unless explicitly disabled
        return pref is not { Enabled: false };
    }
}
