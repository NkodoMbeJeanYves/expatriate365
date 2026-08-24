using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Meetings.DTOs;
using server.Application.Meetings.Queries;
using server.Infrastructure.Persistence;

namespace server.Application.Meetings.Commands;

public record StartMeetingCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<MeetingDto>>;
public record CloseMeetingCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<MeetingDto>>;
public record CancelMeetingCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<MeetingDto>>;

public class StartMeetingCommandHandler(AppDbContext db, ILogger<StartMeetingCommandHandler> log)
    : IRequestHandler<StartMeetingCommand, ServiceResult<MeetingDto>>
{
    public async Task<ServiceResult<MeetingDto>> Handle(StartMeetingCommand request, CancellationToken ct)
    {
        var m = await db.Meetings.Include(x => x.Attendances).Include(x => x.Minute)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);
        if (m is null) return ServiceResult<MeetingDto>.Failure("Réunion introuvable.");
        if (m.Status != "scheduled") return ServiceResult<MeetingDto>.Failure("Seules les réunions planifiées peuvent démarrer.");
        m.Status = "in_progress";
        m.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Meeting {Id} started", m.Id);
        return ServiceResult<MeetingDto>.Success(ListMeetingsQueryHandler.ToDto(m));
    }
}

public class CloseMeetingCommandHandler(AppDbContext db, ILogger<CloseMeetingCommandHandler> log)
    : IRequestHandler<CloseMeetingCommand, ServiceResult<MeetingDto>>
{
    public async Task<ServiceResult<MeetingDto>> Handle(CloseMeetingCommand request, CancellationToken ct)
    {
        var m = await db.Meetings.Include(x => x.Attendances).Include(x => x.Minute)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);
        if (m is null) return ServiceResult<MeetingDto>.Failure("Réunion introuvable.");
        if (m.Status != "in_progress") return ServiceResult<MeetingDto>.Failure("Seules les réunions en cours peuvent être clôturées.");
        m.Status = "completed";
        m.EndedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Meeting {Id} closed", m.Id);
        return ServiceResult<MeetingDto>.Success(ListMeetingsQueryHandler.ToDto(m));
    }
}

public class CancelMeetingCommandHandler(AppDbContext db, ILogger<CancelMeetingCommandHandler> log)
    : IRequestHandler<CancelMeetingCommand, ServiceResult<MeetingDto>>
{
    public async Task<ServiceResult<MeetingDto>> Handle(CancelMeetingCommand request, CancellationToken ct)
    {
        var m = await db.Meetings.Include(x => x.Attendances).Include(x => x.Minute)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);
        if (m is null) return ServiceResult<MeetingDto>.Failure("Réunion introuvable.");
        if (m.Status == "completed") return ServiceResult<MeetingDto>.Failure("Impossible d'annuler une réunion terminée.");
        m.Status = "cancelled";
        await db.SaveChangesAsync(ct);
        log.LogInformation("Meeting {Id} cancelled", m.Id);
        return ServiceResult<MeetingDto>.Success(ListMeetingsQueryHandler.ToDto(m));
    }
}
