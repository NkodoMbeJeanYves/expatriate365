using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Notifications;

// --- DTOs ---

public record NotificationDto(
    string Id,
    string Type,
    string Title,
    string Body,
    bool IsRead,
    string? ReadAt,
    string CreatedAt
);

public record NotificationListResult(
    IEnumerable<NotificationDto> Data,
    int UnreadCount,
    PaginationMeta Pagination
);

// --- Queries ---

public record ListNotificationsQuery(Guid TenantId, Guid UserId, int Page = 1, int Limit = 20, bool? Unread = null)
    : IRequest<NotificationListResult>;

public class ListNotificationsQueryHandler(AppDbContext db)
    : IRequestHandler<ListNotificationsQuery, NotificationListResult>
{
    public async Task<NotificationListResult> Handle(ListNotificationsQuery request, CancellationToken ct)
    {
        var q = db.Notifications
            .Where(n => n.TenantId == request.TenantId && n.UserId == request.UserId && n.IsActive);

        if (request.Unread == true)
            q = q.Where(n => !n.IsRead);

        var total    = await q.CountAsync(ct);
        var unread   = await db.Notifications.CountAsync(
            n => n.TenantId == request.TenantId && n.UserId == request.UserId && n.IsActive && !n.IsRead, ct);

        var items = await q.OrderByDescending(n => n.CreatedAt)
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .Select(n => new NotificationDto(
                n.Id.ToString(), n.Type, n.Title, n.Body, n.IsRead,
                n.ReadAt.HasValue ? n.ReadAt.Value.ToString("O") : null,
                n.CreatedAt.ToString("O")))
            .ToListAsync(ct);

        return new NotificationListResult(items, unread,
            new PaginationMeta { Page = request.Page, Limit = request.Limit, Total = total });
    }
}

// --- Commands ---

public record MarkNotificationReadCommand(Guid TenantId, Guid UserId, Guid NotificationId)
    : IRequest<ServiceResult<bool>>;

public class MarkNotificationReadCommandHandler(AppDbContext db)
    : IRequestHandler<MarkNotificationReadCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var n = await db.Notifications.FirstOrDefaultAsync(
            x => x.Id == request.NotificationId && x.UserId == request.UserId && x.TenantId == request.TenantId, ct);
        if (n is null) return ServiceResult<bool>.Failure("Notification introuvable.");

        n.IsRead = true;
        n.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }
}

public record MarkAllNotificationsReadCommand(Guid TenantId, Guid UserId)
    : IRequest<ServiceResult<int>>;

public class MarkAllNotificationsReadCommandHandler(AppDbContext db)
    : IRequestHandler<MarkAllNotificationsReadCommand, ServiceResult<int>>
{
    public async Task<ServiceResult<int>> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        var unread = await db.Notifications
            .Where(n => n.TenantId == request.TenantId && n.UserId == request.UserId && n.IsActive && !n.IsRead)
            .ToListAsync(ct);

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        return ServiceResult<int>.Success(unread.Count);
    }
}

// --- Notification Preferences ---

public record NotificationPreferenceDto(string Type, bool Enabled);

public record UpdatePreferencesRequest(List<NotificationPreferenceDto> Preferences);

public static class NotificationTypes
{
    public static readonly string[] All =
    [
        "charge_generated", "payment_confirmed", "payment_recorded",
        "event_invite", "welfare_update", "meeting_invite", "election_open"
    ];
}

public record GetNotificationPreferencesQuery(Guid TenantId, Guid UserId)
    : IRequest<List<NotificationPreferenceDto>>;

public class GetNotificationPreferencesQueryHandler(AppDbContext db)
    : IRequestHandler<GetNotificationPreferencesQuery, List<NotificationPreferenceDto>>
{
    public async Task<List<NotificationPreferenceDto>> Handle(GetNotificationPreferencesQuery request, CancellationToken ct)
    {
        var saved = await db.NotificationPreferences
            .Where(p => p.TenantId == request.TenantId && p.UserId == request.UserId)
            .ToListAsync(ct);

        // Return all known types; default to enabled if not explicitly saved
        return NotificationTypes.All
            .Select(type =>
            {
                var pref = saved.FirstOrDefault(p => p.NotificationType == type);
                return new NotificationPreferenceDto(type, pref?.Enabled ?? true);
            })
            .ToList();
    }
}

public record UpdateNotificationPreferencesCommand(Guid TenantId, Guid UserId, UpdatePreferencesRequest Dto)
    : IRequest<ServiceResult<List<NotificationPreferenceDto>>>;

public class UpdateNotificationPreferencesCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateNotificationPreferencesCommand, ServiceResult<List<NotificationPreferenceDto>>>
{
    public async Task<ServiceResult<List<NotificationPreferenceDto>>> Handle(UpdateNotificationPreferencesCommand request, CancellationToken ct)
    {
        var existing = await db.NotificationPreferences
            .Where(p => p.TenantId == request.TenantId && p.UserId == request.UserId)
            .ToListAsync(ct);

        foreach (var dto in request.Dto.Preferences)
        {
            if (!NotificationTypes.All.Contains(dto.Type)) continue;

            var pref = existing.FirstOrDefault(p => p.NotificationType == dto.Type);
            if (pref is null)
            {
                db.NotificationPreferences.Add(new NotificationPreference
                {
                    TenantId = request.TenantId,
                    UserId = request.UserId,
                    NotificationType = dto.Type,
                    Enabled = dto.Enabled,
                });
            }
            else
            {
                pref.Enabled = dto.Enabled;
                pref.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);

        var result = await new GetNotificationPreferencesQueryHandler(db)
            .Handle(new GetNotificationPreferencesQuery(request.TenantId, request.UserId), ct);
        return ServiceResult<List<NotificationPreferenceDto>>.Success(result);
    }
}
