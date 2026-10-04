using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Events.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Events.Commands;

public record RegisterToEventCommand(Guid TenantId, Guid EventId, RegisterToEventRequest Dto) : IRequest<ServiceResult<EventRegistrationDto>>;
public record CancelRegistrationCommand(Guid TenantId, Guid EventId, Guid RegistrationId) : IRequest<ServiceResult<EventRegistrationDto>>;

public class RegisterToEventValidator : AbstractValidator<RegisterToEventCommand>
{
    public RegisterToEventValidator() { RuleFor(x => x.Dto.MemberId).NotEmpty(); }
}

public class RegisterToEventCommandHandler(AppDbContext db, ILogger<RegisterToEventCommandHandler> log)
    : IRequestHandler<RegisterToEventCommand, ServiceResult<EventRegistrationDto>>
{
    public async Task<ServiceResult<EventRegistrationDto>> Handle(RegisterToEventCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(request.Dto.MemberId, out var memberId))
            return ServiceResult<EventRegistrationDto>.Failure("MemberId invalide.", "errors.common.invalid_id");

        var ev = await db.Events.Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.EventId && e.TenantId == request.TenantId, ct);
        if (ev is null) return ServiceResult<EventRegistrationDto>.Failure("Événement introuvable.", "errors.event.not_found");
        if (ev.Status != "published") return ServiceResult<EventRegistrationDto>.Failure("L'événement n'est pas ouvert aux inscriptions.", "errors.event.not_open");

        var activeCount = ev.Registrations.Count(r => r.Status is "registered" or "attended" && r.IsActive);
        var isFull = ev.MaxCapacity.HasValue && activeCount >= ev.MaxCapacity.Value;

        var existing = ev.Registrations.FirstOrDefault(r => r.MemberId == memberId && r.IsActive);
        if (existing is not null && existing.Status is "registered" or "waitlisted" or "attended")
            return ServiceResult<EventRegistrationDto>.Failure("Ce membre est déjà inscrit.", "errors.event.already_registered");

        var member = await db.Members.Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.TenantId == request.TenantId, ct);
        if (member is null) return ServiceResult<EventRegistrationDto>.Failure("Membre introuvable.", "errors.member.not_found");

        var status = isFull ? "waitlisted" : "registered";
        var reg = new EventRegistration
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            EventId = request.EventId,
            MemberId = memberId,
            Status = status,
        };

        db.EventRegistrations.Add(reg);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Member {MemberId} {Status} to event {EventId}", memberId, status, request.EventId);

        return ServiceResult<EventRegistrationDto>.Success(new EventRegistrationDto(
            reg.Id.ToString(), reg.EventId.ToString(), reg.MemberId.ToString(),
            $"{member.User.FirstName} {member.User.LastName}", member.MembershipNumber,
            reg.Status, null, reg.CreatedAt.ToString("O")));
    }
}

public class CancelRegistrationCommandHandler(AppDbContext db, ILogger<CancelRegistrationCommandHandler> log, INotificationService notif)
    : IRequestHandler<CancelRegistrationCommand, ServiceResult<EventRegistrationDto>>
{
    public async Task<ServiceResult<EventRegistrationDto>> Handle(CancelRegistrationCommand request, CancellationToken ct)
    {
        var reg = await db.EventRegistrations
            .Include(r => r.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Id == request.RegistrationId && r.EventId == request.EventId && r.TenantId == request.TenantId, ct);

        if (reg is null) return ServiceResult<EventRegistrationDto>.Failure("Inscription introuvable.", "errors.registration.not_found");
        if (reg.Status == "cancelled") return ServiceResult<EventRegistrationDto>.Failure("Inscription déjà annulée.", "errors.registration.already_cancelled");

        reg.Status = "cancelled";
        reg.UpdatedAt = DateTime.UtcNow;

        // Promote first waitlisted member when a spot frees up
        var ev = await db.Events.Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.EventId && e.TenantId == request.TenantId, ct);
        if (ev?.MaxCapacity.HasValue == true)
        {
            var activeCount = ev.Registrations.Count(r => r.Status is "registered" or "attended" && r.IsActive && r.Id != request.RegistrationId);
            if (activeCount < ev.MaxCapacity.Value)
            {
                var nextWaiting = ev.Registrations
                    .Where(r => r.Status == "waitlisted" && r.IsActive)
                    .OrderBy(r => r.CreatedAt)
                    .FirstOrDefault();
                if (nextWaiting is not null)
                {
                    nextWaiting.Status = "registered";
                    nextWaiting.UpdatedAt = DateTime.UtcNow;
                    log.LogInformation("Promoted waitlisted member {MemberId} to registered for event {EventId}", nextWaiting.MemberId, request.EventId);

                    // Notify promoted member
                    var promotedMember = await db.Members.Include(m => m.User)
                        .FirstOrDefaultAsync(m => m.Id == nextWaiting.MemberId && m.TenantId == request.TenantId, ct);
                    if (promotedMember is not null)
                    {
                        var lang = promotedMember.User.PreferredLanguage ?? "fr";
                        _ = notif.NotifyAsync(request.TenantId, promotedMember.UserId,
                            "event_invite",
                            lang == "fr" ? "Place disponible — vous êtes inscrit(e) !" : "Spot available — you are registered!",
                            lang == "fr"
                                ? "Une place s'est libérée. Vous avez été transféré(e) de la liste d'attente vers les inscrits."
                                : "A spot has opened up. You have been moved from the waitlist to registered.",
                            ct);
                    }
                }
            }
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Registration {Id} cancelled", reg.Id);

        return ServiceResult<EventRegistrationDto>.Success(new EventRegistrationDto(
            reg.Id.ToString(), reg.EventId.ToString(), reg.MemberId.ToString(),
            $"{reg.Member.User.FirstName} {reg.Member.User.LastName}", reg.Member.MembershipNumber,
            reg.Status, null, reg.CreatedAt.ToString("O")));
    }
}
