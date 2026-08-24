using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Events.DTOs;
using server.Application.Events.Queries;
using server.Infrastructure.Persistence;

namespace server.Application.Events.Commands;

public record PublishEventCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<EventDto>>;
public record CompleteEventCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<EventDto>>;
public record CancelEventCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<EventDto>>;

public class PublishEventCommandHandler(AppDbContext db, ILogger<PublishEventCommandHandler> log)
    : IRequestHandler<PublishEventCommand, ServiceResult<EventDto>>
{
    public async Task<ServiceResult<EventDto>> Handle(PublishEventCommand request, CancellationToken ct)
    {
        var ev = await db.Events.Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == request.TenantId, ct);
        if (ev is null) return ServiceResult<EventDto>.Failure("Événement introuvable.");
        if (ev.Status != "draft") return ServiceResult<EventDto>.Failure("Seuls les brouillons peuvent être publiés.");
        ev.Status = "published";
        await db.SaveChangesAsync(ct);
        log.LogInformation("Event {Id} published", ev.Id);
        return ServiceResult<EventDto>.Success(ListEventsQueryHandler.ToDto(ev));
    }
}

public class CompleteEventCommandHandler(AppDbContext db, ILogger<CompleteEventCommandHandler> log)
    : IRequestHandler<CompleteEventCommand, ServiceResult<EventDto>>
{
    public async Task<ServiceResult<EventDto>> Handle(CompleteEventCommand request, CancellationToken ct)
    {
        var ev = await db.Events.Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == request.TenantId, ct);
        if (ev is null) return ServiceResult<EventDto>.Failure("Événement introuvable.");
        if (ev.Status == "completed" || ev.Status == "cancelled")
            return ServiceResult<EventDto>.Failure("Événement déjà terminé ou annulé.");
        ev.Status = "completed";
        await db.SaveChangesAsync(ct);
        log.LogInformation("Event {Id} completed", ev.Id);
        return ServiceResult<EventDto>.Success(ListEventsQueryHandler.ToDto(ev));
    }
}

public class CancelEventCommandHandler(AppDbContext db, ILogger<CancelEventCommandHandler> log)
    : IRequestHandler<CancelEventCommand, ServiceResult<EventDto>>
{
    public async Task<ServiceResult<EventDto>> Handle(CancelEventCommand request, CancellationToken ct)
    {
        var ev = await db.Events.Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == request.TenantId, ct);
        if (ev is null) return ServiceResult<EventDto>.Failure("Événement introuvable.");
        if (ev.Status == "completed") return ServiceResult<EventDto>.Failure("Impossible d'annuler un événement terminé.");
        ev.Status = "cancelled";
        await db.SaveChangesAsync(ct);
        log.LogInformation("Event {Id} cancelled", ev.Id);
        return ServiceResult<EventDto>.Success(ListEventsQueryHandler.ToDto(ev));
    }
}
