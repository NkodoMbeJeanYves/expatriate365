using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Meetings.DTOs;
using server.Application.Meetings.Queries;
using server.Infrastructure.Persistence;

namespace server.Application.Meetings.Commands;

public record UpdateMeetingCommand(Guid TenantId, Guid Id, UpdateMeetingRequest Dto) : IRequest<ServiceResult<MeetingDto>>;

public class UpdateMeetingCommandHandler(AppDbContext db, ILogger<UpdateMeetingCommandHandler> log)
    : IRequestHandler<UpdateMeetingCommand, ServiceResult<MeetingDto>>
{
    public async Task<ServiceResult<MeetingDto>> Handle(UpdateMeetingCommand request, CancellationToken ct)
    {
        var m = await db.Meetings.Include(x => x.Attendances).Include(x => x.Minute)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);
        if (m is null) return ServiceResult<MeetingDto>.Failure("Réunion introuvable.");
        if (m.Status is "completed" or "cancelled")
            return ServiceResult<MeetingDto>.Failure("Impossible de modifier une réunion terminée ou annulée.");

        m.Title = request.Dto.Title;
        m.Type = request.Dto.Type;
        m.ScheduledAt = DateTime.Parse(request.Dto.ScheduledAt);
        m.Location = request.Dto.Location;
        m.Agenda = request.Dto.Agenda;
        m.QuorumRequired = request.Dto.QuorumRequired;

        await db.SaveChangesAsync(ct);
        log.LogInformation("Meeting {Id} updated", m.Id);
        return ServiceResult<MeetingDto>.Success(ListMeetingsQueryHandler.ToDto(m));
    }
}
