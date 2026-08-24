using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Events.DTOs;
using server.Application.Events.Queries;
using server.Infrastructure.Persistence;

namespace server.Application.Events.Commands;

public record UpdateEventCommand(Guid TenantId, Guid Id, UpdateEventRequest Dto) : IRequest<ServiceResult<EventDto>>;

public class UpdateEventCommandHandler(AppDbContext db, ILogger<UpdateEventCommandHandler> log)
    : IRequestHandler<UpdateEventCommand, ServiceResult<EventDto>>
{
    public async Task<ServiceResult<EventDto>> Handle(UpdateEventCommand request, CancellationToken ct)
    {
        var ev = await db.Events.Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == request.TenantId, ct);
        if (ev is null) return ServiceResult<EventDto>.Failure("Événement introuvable.");
        if (ev.Status == "completed" || ev.Status == "cancelled")
            return ServiceResult<EventDto>.Failure("Impossible de modifier un événement terminé ou annulé.");

        var dto = request.Dto;
        var start = DateTime.Parse(dto.StartDate);
        var end = DateTime.Parse(dto.EndDate);
        if (end <= start) return ServiceResult<EventDto>.Failure("La date de fin doit être postérieure à la date de début.");

        ev.Title = dto.Title;
        ev.Description = dto.Description;
        ev.Type = dto.Type;
        ev.Location = dto.Location;
        ev.StartDate = start;
        ev.EndDate = end;
        ev.MaxCapacity = dto.MaxCapacity;
        ev.IsPublic = dto.IsPublic;

        await db.SaveChangesAsync(ct);
        log.LogInformation("Event {Id} updated", ev.Id);
        return ServiceResult<EventDto>.Success(ListEventsQueryHandler.ToDto(ev));
    }
}
