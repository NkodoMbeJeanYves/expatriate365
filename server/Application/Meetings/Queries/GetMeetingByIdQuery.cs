using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Meetings.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Meetings.Queries;

public record GetMeetingByIdQuery(Guid TenantId, Guid Id)
    : IRequest<ServiceResult<(MeetingDto Meeting, List<MeetingAttendanceDto> Attendances, MeetingMinuteDto? Minute)>>;

public class GetMeetingByIdQueryHandler(AppDbContext db)
    : IRequestHandler<GetMeetingByIdQuery, ServiceResult<(MeetingDto, List<MeetingAttendanceDto>, MeetingMinuteDto?)>>
{
    public async Task<ServiceResult<(MeetingDto, List<MeetingAttendanceDto>, MeetingMinuteDto?)>> Handle(
        GetMeetingByIdQuery request, CancellationToken ct)
    {
        var m = await db.Meetings
            .Include(x => x.Attendances).ThenInclude(a => a.Member).ThenInclude(mb => mb.User)
            .Include(x => x.Minute)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (m is null)
            return ServiceResult<(MeetingDto, List<MeetingAttendanceDto>, MeetingMinuteDto?)>.Failure("Réunion introuvable.");

        var attendances = m.Attendances.Where(a => a.IsActive).Select(a => new MeetingAttendanceDto(
            a.Id.ToString(), a.MeetingId.ToString(), a.MemberId.ToString(),
            $"{a.Member.User.FirstName} {a.Member.User.LastName}",
            a.Member.MembershipNumber, a.Status, a.ProxyName, a.CreatedAt.ToString("O"))).ToList();

        MeetingMinuteDto? minute = m.Minute is null ? null : new MeetingMinuteDto(
            m.Minute.Id.ToString(), m.Minute.MeetingId.ToString(), m.Minute.Content,
            m.Minute.Decisions, m.Minute.AttachmentUrl, m.Minute.IsApproved,
            m.Minute.ApprovedAt?.ToString("O"), m.Minute.CreatedAt.ToString("O"),
            m.Minute.UpdatedAt?.ToString("O"));

        return ServiceResult<(MeetingDto, List<MeetingAttendanceDto>, MeetingMinuteDto?)>.Success(
            (ListMeetingsQueryHandler.ToDto(m), attendances, minute));
    }
}
