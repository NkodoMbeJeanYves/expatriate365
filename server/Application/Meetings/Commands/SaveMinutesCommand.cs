using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Meetings.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Meetings.Commands;

public record SaveMinutesCommand(Guid TenantId, Guid MeetingId, SaveMinutesRequest Dto)
    : IRequest<ServiceResult<MeetingMinuteDto>>;

public record ApproveMinutesCommand(Guid TenantId, Guid MeetingId)
    : IRequest<ServiceResult<MeetingMinuteDto>>;

public class SaveMinutesCommandHandler(AppDbContext db, ILogger<SaveMinutesCommandHandler> log)
    : IRequestHandler<SaveMinutesCommand, ServiceResult<MeetingMinuteDto>>
{
    public async Task<ServiceResult<MeetingMinuteDto>> Handle(SaveMinutesCommand request, CancellationToken ct)
    {
        var meeting = await db.Meetings.Include(m => m.Minute)
            .FirstOrDefaultAsync(m => m.Id == request.MeetingId && m.TenantId == request.TenantId, ct);
        if (meeting is null) return ServiceResult<MeetingMinuteDto>.Failure("Réunion introuvable.");

        if (meeting.Minute is not null)
        {
            meeting.Minute.Content = request.Dto.Content;
            meeting.Minute.Decisions = request.Dto.Decisions;
            if (request.Dto.AttachmentUrl is not null) meeting.Minute.AttachmentUrl = request.Dto.AttachmentUrl;
            meeting.Minute.IsApproved = false;
            meeting.Minute.ApprovedAt = null;
        }
        else
        {
            var minute = new MeetingMinute
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                MeetingId = request.MeetingId,
                Content = request.Dto.Content,
                Decisions = request.Dto.Decisions,
                AttachmentUrl = request.Dto.AttachmentUrl,
            };
            db.MeetingMinutes.Add(minute);
            meeting.Minute = minute;
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Minutes saved for meeting {Id}", request.MeetingId);

        var min = meeting.Minute!;
        return ServiceResult<MeetingMinuteDto>.Success(new MeetingMinuteDto(
            min.Id.ToString(), min.MeetingId.ToString(), min.Content, min.Decisions,
            min.AttachmentUrl,
            min.IsApproved, min.ApprovedAt?.ToString("O"), min.CreatedAt.ToString("O"), min.UpdatedAt?.ToString("O")));
    }
}

public class ApproveMinutesCommandHandler(AppDbContext db, ILogger<ApproveMinutesCommandHandler> log)
    : IRequestHandler<ApproveMinutesCommand, ServiceResult<MeetingMinuteDto>>
{
    public async Task<ServiceResult<MeetingMinuteDto>> Handle(ApproveMinutesCommand request, CancellationToken ct)
    {
        var minute = await db.MeetingMinutes
            .FirstOrDefaultAsync(m => m.MeetingId == request.MeetingId && m.TenantId == request.TenantId, ct);
        if (minute is null) return ServiceResult<MeetingMinuteDto>.Failure("Compte-rendu introuvable.");
        if (minute.IsApproved) return ServiceResult<MeetingMinuteDto>.Failure("Compte-rendu déjà approuvé.");

        minute.IsApproved = true;
        minute.ApprovedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Minutes approved for meeting {Id}", request.MeetingId);

        return ServiceResult<MeetingMinuteDto>.Success(new MeetingMinuteDto(
            minute.Id.ToString(), minute.MeetingId.ToString(), minute.Content, minute.Decisions,
            minute.AttachmentUrl,
            minute.IsApproved, minute.ApprovedAt?.ToString("O"), minute.CreatedAt.ToString("O"), minute.UpdatedAt?.ToString("O")));
    }
}
