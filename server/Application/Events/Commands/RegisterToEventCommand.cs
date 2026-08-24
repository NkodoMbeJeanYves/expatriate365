using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Events.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

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
            return ServiceResult<EventRegistrationDto>.Failure("MemberId invalide.");

        var ev = await db.Events.Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == request.EventId && e.TenantId == request.TenantId, ct);
        if (ev is null) return ServiceResult<EventRegistrationDto>.Failure("Événement introuvable.");
        if (ev.Status != "published") return ServiceResult<EventRegistrationDto>.Failure("L'événement n'est pas ouvert aux inscriptions.");

        var activeCount = ev.Registrations.Count(r => r.Status != "cancelled" && r.IsActive);
        if (ev.MaxCapacity.HasValue && activeCount >= ev.MaxCapacity.Value)
            return ServiceResult<EventRegistrationDto>.Failure("Capacité maximale atteinte.");

        var existing = ev.Registrations.FirstOrDefault(r => r.MemberId == memberId && r.IsActive);
        if (existing is not null && existing.Status != "cancelled")
            return ServiceResult<EventRegistrationDto>.Failure("Ce membre est déjà inscrit.");

        var member = await db.Members.Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.TenantId == request.TenantId, ct);
        if (member is null) return ServiceResult<EventRegistrationDto>.Failure("Membre introuvable.");

        var reg = new EventRegistration
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            EventId = request.EventId,
            MemberId = memberId,
            Status = "registered",
        };

        db.EventRegistrations.Add(reg);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Member {MemberId} registered to event {EventId}", memberId, request.EventId);

        return ServiceResult<EventRegistrationDto>.Success(new EventRegistrationDto(
            reg.Id.ToString(), reg.EventId.ToString(), reg.MemberId.ToString(),
            $"{member.User.FirstName} {member.User.LastName}", member.MembershipNumber,
            reg.Status, null, reg.CreatedAt.ToString("O")));
    }
}

public class CancelRegistrationCommandHandler(AppDbContext db, ILogger<CancelRegistrationCommandHandler> log)
    : IRequestHandler<CancelRegistrationCommand, ServiceResult<EventRegistrationDto>>
{
    public async Task<ServiceResult<EventRegistrationDto>> Handle(CancelRegistrationCommand request, CancellationToken ct)
    {
        var reg = await db.EventRegistrations
            .Include(r => r.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(r => r.Id == request.RegistrationId && r.EventId == request.EventId && r.TenantId == request.TenantId, ct);

        if (reg is null) return ServiceResult<EventRegistrationDto>.Failure("Inscription introuvable.");
        if (reg.Status == "cancelled") return ServiceResult<EventRegistrationDto>.Failure("Inscription déjà annulée.");

        reg.Status = "cancelled";
        await db.SaveChangesAsync(ct);
        log.LogInformation("Registration {Id} cancelled", reg.Id);

        return ServiceResult<EventRegistrationDto>.Success(new EventRegistrationDto(
            reg.Id.ToString(), reg.EventId.ToString(), reg.MemberId.ToString(),
            $"{reg.Member.User.FirstName} {reg.Member.User.LastName}", reg.Member.MembershipNumber,
            reg.Status, null, reg.CreatedAt.ToString("O")));
    }
}
