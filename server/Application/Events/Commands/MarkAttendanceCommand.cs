using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Events.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Events.Commands;

public record MarkAttendanceCommand(Guid TenantId, Guid EventId, MarkAttendanceRequest Dto) : IRequest<ServiceResult<int>>;

public class MarkAttendanceCommandHandler(AppDbContext db, ILogger<MarkAttendanceCommandHandler> log)
    : IRequestHandler<MarkAttendanceCommand, ServiceResult<int>>
{
    private static readonly string[] ValidStatuses = ["attended", "no_show", "registered"];

    public async Task<ServiceResult<int>> Handle(MarkAttendanceCommand request, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == request.EventId && e.TenantId == request.TenantId, ct);
        if (ev is null) return ServiceResult<int>.Failure("Événement introuvable.");

        var regIds = request.Dto.Entries.Select(e => Guid.Parse(e.RegistrationId)).ToList();
        var registrations = await db.EventRegistrations
            .Where(r => regIds.Contains(r.Id) && r.EventId == request.EventId && r.TenantId == request.TenantId)
            .ToListAsync(ct);

        int updated = 0;
        foreach (var entry in request.Dto.Entries)
        {
            if (!ValidStatuses.Contains(entry.Status)) continue;
            var reg = registrations.FirstOrDefault(r => r.Id.ToString() == entry.RegistrationId);
            if (reg is null) continue;
            reg.Status = entry.Status;
            if (entry.Status == "attended") reg.AttendedAt = DateTime.UtcNow;
            updated++;
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Attendance marked for {Count} registrations on event {EventId}", updated, request.EventId);
        return ServiceResult<int>.Success(updated);
    }
}
