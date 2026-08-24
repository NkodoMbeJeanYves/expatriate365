using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Meetings.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Meetings.Commands;

public record RecordAttendanceCommand(Guid TenantId, Guid MeetingId, RecordAttendanceRequest Dto)
    : IRequest<ServiceResult<int>>;

public class RecordAttendanceCommandHandler(AppDbContext db, ILogger<RecordAttendanceCommandHandler> log)
    : IRequestHandler<RecordAttendanceCommand, ServiceResult<int>>
{
    private static readonly string[] ValidStatuses = ["present", "absent", "excused", "proxy"];

    public async Task<ServiceResult<int>> Handle(RecordAttendanceCommand request, CancellationToken ct)
    {
        var meeting = await db.Meetings.Include(m => m.Attendances)
            .FirstOrDefaultAsync(m => m.Id == request.MeetingId && m.TenantId == request.TenantId, ct);
        if (meeting is null) return ServiceResult<int>.Failure("Réunion introuvable.");
        if (meeting.Status is "cancelled") return ServiceResult<int>.Failure("Réunion annulée.");

        int updated = 0;
        foreach (var entry in request.Dto.Entries)
        {
            if (!Guid.TryParse(entry.MemberId, out var memberId)) continue;
            if (!ValidStatuses.Contains(entry.Status)) continue;

            var existing = meeting.Attendances.FirstOrDefault(a => a.MemberId == memberId && a.IsActive);
            if (existing is not null)
            {
                existing.Status = entry.Status;
                existing.ProxyName = entry.ProxyName;
            }
            else
            {
                db.MeetingAttendances.Add(new MeetingAttendance
                {
                    Id = Guid.NewGuid(),
                    TenantId = request.TenantId,
                    MeetingId = request.MeetingId,
                    MemberId = memberId,
                    Status = entry.Status,
                    ProxyName = entry.ProxyName,
                });
            }
            updated++;
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Recorded attendance for {Count} members on meeting {Id}", updated, request.MeetingId);
        return ServiceResult<int>.Success(updated);
    }
}
