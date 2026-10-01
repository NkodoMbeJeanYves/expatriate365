using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Events.DTOs;
using server.Application.Events.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Events.Commands;

public record CreateEventCommand(Guid TenantId, CreateEventRequest Dto) : IRequest<ServiceResult<EventDto>>;

public class CreateEventValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventValidator()
    {
        RuleFor(x => x.Dto.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Dto.StartDate).NotEmpty();
        RuleFor(x => x.Dto.EndDate).NotEmpty();
    }
}

public class CreateEventCommandHandler(
    AppDbContext db,
    ILogger<CreateEventCommandHandler> log,
    IEmailService emailService,
    IConfiguration config)
    : IRequestHandler<CreateEventCommand, ServiceResult<EventDto>>
{
    public async Task<ServiceResult<EventDto>> Handle(CreateEventCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var start = DateTime.Parse(dto.StartDate);
        var end   = DateTime.Parse(dto.EndDate);

        if (end <= start)
            return ServiceResult<EventDto>.Failure("La date de fin doit être postérieure à la date de début.");

        var ev = new Event
        {
            Id          = Guid.NewGuid(),
            TenantId    = request.TenantId,
            Title       = dto.Title,
            Description = dto.Description,
            Type        = dto.Type,
            Location    = dto.Location,
            StartDate   = start,
            EndDate     = end,
            MaxCapacity = dto.MaxCapacity,
            IsPublic    = dto.IsPublic,
            Status      = "draft",
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Event {Id} created: {Title}", ev.Id, ev.Title);

        // Notifier les membres actifs de l'association en background
        _ = NotifyMembersAsync(ev, request.TenantId, config["FrontendBaseUrl"] ?? config["App:BaseUrl"] ?? "https://app.expatriate365.mu", ct);

        return ServiceResult<EventDto>.Success(ListEventsQueryHandler.ToDto(ev));
    }

    private async Task NotifyMembersAsync(Event ev, Guid tenantId, string baseUrl, CancellationToken ct)
    {
        try
        {
            var users = await db.Users
                .Where(u => u.TenantId == tenantId && u.IsActive && u.ContactEmail != null && u.ContactEmail != "")
                .Select(u => new { FullName = u.FirstName + " " + u.LastName, NotifEmail = u.ContactEmail! })
                .ToListAsync(ct);

            var tenant = await db.Tenants.FindAsync([tenantId], ct);
            var assocName  = tenant?.Name ?? "Expatriate365";
            var eventUrl   = $"{baseUrl}/events/{ev.Id}";
            var dateStr    = ev.StartDate.ToString("dddd d MMMM yyyy à HH:mm", new System.Globalization.CultureInfo("fr-FR"));
            var location   = ev.Location ?? "À définir";

            var tasks = users.Select(u => emailService.SendAsync(
                u.NotifEmail,
                u.FullName,
                $"[{assocName}] Événement : {ev.Title}",
                EmailTemplates.EventInvite(u.FullName, ev.Title, dateStr, location, assocName, eventUrl),
                ct));

            await Task.WhenAll(tasks);
            log.LogInformation("Event notifications sent to {Count} members for event {Id}", users.Count, ev.Id);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to send event notifications for event {Id}", ev.Id);
        }
    }
}
