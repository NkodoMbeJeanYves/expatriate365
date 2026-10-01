using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Meetings.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Meetings.Commands;

// ── Create ────────────────────────────────────────────────────────────────────

public record CreateActionItemCommand(Guid TenantId, Guid MeetingId, CreateActionItemRequest Dto)
    : IRequest<ServiceResult<ActionItemDto>>;

public class CreateActionItemHandler(AppDbContext db, ILogger<CreateActionItemHandler> log)
    : IRequestHandler<CreateActionItemCommand, ServiceResult<ActionItemDto>>
{
    public async Task<ServiceResult<ActionItemDto>> Handle(CreateActionItemCommand request, CancellationToken ct)
    {
        var meeting = await db.Meetings.FirstOrDefaultAsync(m => m.Id == request.MeetingId && m.TenantId == request.TenantId && m.IsActive, ct);
        if (meeting is null) return ServiceResult<ActionItemDto>.Failure("Meeting not found.");

        var item = new MeetingActionItem
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            MeetingId = request.MeetingId,
            Title = request.Dto.Title.Trim(),
            Description = request.Dto.Description?.Trim(),
            AssignedToMemberId = string.IsNullOrEmpty(request.Dto.AssignedToMemberId) ? null : Guid.Parse(request.Dto.AssignedToMemberId),
            DueDate = string.IsNullOrEmpty(request.Dto.DueDate) ? null : DateOnly.Parse(request.Dto.DueDate),
        };

        db.MeetingActionItems.Add(item);
        await db.SaveChangesAsync(ct);
        log.LogInformation("ActionItem created {Id} for meeting {MeetingId}", item.Id, request.MeetingId);

        return ServiceResult<ActionItemDto>.Success(await ActionItemMapper.ToDto(db, item, ct));
    }
}

// ── Update ────────────────────────────────────────────────────────────────────

public record UpdateActionItemCommand(Guid TenantId, Guid ItemId, UpdateActionItemRequest Dto)
    : IRequest<ServiceResult<ActionItemDto>>;

public class UpdateActionItemHandler(AppDbContext db, ILogger<UpdateActionItemHandler> log)
    : IRequestHandler<UpdateActionItemCommand, ServiceResult<ActionItemDto>>
{
    public async Task<ServiceResult<ActionItemDto>> Handle(UpdateActionItemCommand request, CancellationToken ct)
    {
        var item = await db.MeetingActionItems.FirstOrDefaultAsync(i => i.Id == request.ItemId && i.TenantId == request.TenantId && i.IsActive, ct);
        if (item is null) return ServiceResult<ActionItemDto>.Failure("Action item not found.");

        item.Title = request.Dto.Title.Trim();
        item.Description = request.Dto.Description?.Trim();
        item.AssignedToMemberId = string.IsNullOrEmpty(request.Dto.AssignedToMemberId) ? null : Guid.Parse(request.Dto.AssignedToMemberId);
        item.DueDate = string.IsNullOrEmpty(request.Dto.DueDate) ? null : DateOnly.Parse(request.Dto.DueDate);
        item.Status = request.Dto.Status;
        item.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("ActionItem updated {Id}", item.Id);

        return ServiceResult<ActionItemDto>.Success(await ActionItemMapper.ToDto(db, item, ct));
    }
}

// ── Delete ────────────────────────────────────────────────────────────────────

public record DeleteActionItemCommand(Guid TenantId, Guid ItemId) : IRequest<ServiceResult<bool>>;

public class DeleteActionItemHandler(AppDbContext db, ILogger<DeleteActionItemHandler> log)
    : IRequestHandler<DeleteActionItemCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeleteActionItemCommand request, CancellationToken ct)
    {
        var item = await db.MeetingActionItems.FirstOrDefaultAsync(i => i.Id == request.ItemId && i.TenantId == request.TenantId && i.IsActive, ct);
        if (item is null) return ServiceResult<bool>.Failure("Action item not found.");

        item.IsActive = false;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        log.LogInformation("ActionItem deleted {Id}", item.Id);

        return ServiceResult<bool>.Success(true);
    }
}

// ── Helper ───────────────────────────────────────────────────────────────────

internal static class ActionItemMapper
{
    internal static async Task<ActionItemDto> ToDto(AppDbContext db, MeetingActionItem item, CancellationToken ct)
    {
        string? assignedName = null;
        if (item.AssignedToMemberId.HasValue)
        {
            var member = await db.Members.Include(m => m.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == item.AssignedToMemberId.Value, ct);
            if (member is not null)
                assignedName = member.User.FirstName + " " + member.User.LastName;
        }
        return new ActionItemDto(
            item.Id.ToString(), item.MeetingId.ToString(),
            item.Title, item.Description,
            item.AssignedToMemberId?.ToString(), assignedName,
            item.DueDate?.ToString("yyyy-MM-dd"), item.Status,
            item.CreatedAt.ToString("O"), item.UpdatedAt?.ToString("O"));
    }
}
