using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Events.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Events.Queries;

public record GetEventByIdQuery(Guid TenantId, Guid Id) : IRequest<ServiceResult<(EventDto Event, List<EventRegistrationDto> Registrations)>>;

public class GetEventByIdQueryHandler(AppDbContext db)
    : IRequestHandler<GetEventByIdQuery, ServiceResult<(EventDto, List<EventRegistrationDto>)>>
{
    public async Task<ServiceResult<(EventDto, List<EventRegistrationDto>)>> Handle(GetEventByIdQuery request, CancellationToken ct)
    {
        var ev = await db.Events
            .Include(e => e.Registrations).ThenInclude(r => r.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == request.TenantId, ct);

        if (ev is null) return ServiceResult<(EventDto, List<EventRegistrationDto>)>.Failure("Événement introuvable.");

        var regs = ev.Registrations
            .Where(r => r.IsActive)
            .Select(r => new EventRegistrationDto(
                r.Id.ToString(), r.EventId.ToString(), r.MemberId.ToString(),
                $"{r.Member.User.FirstName} {r.Member.User.LastName}", r.Member.MembershipNumber,
                r.Status, r.AttendedAt?.ToString("O"), r.CreatedAt.ToString("O")))
            .ToList();

        return ServiceResult<(EventDto, List<EventRegistrationDto>)>.Success((ListEventsQueryHandler.ToDto(ev), regs));
    }
}
