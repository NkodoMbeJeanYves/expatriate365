namespace server.Infrastructure.Services;

public interface INotificationService
{
    Task NotifyAsync(Guid tenantId, Guid userId, string type, string title, string body, CancellationToken ct = default, string? entityType = null, string? entityId = null);
    Task NotifyWithEmailAsync(Guid tenantId, Guid userId, string type, string title, string body, string emailSubject, string emailHtml, CancellationToken ct = default, string? entityType = null, string? entityId = null);
}
