using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Meetings.DTOs;
using server.Application.Meetings.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

namespace server.Application.Meetings.Commands;

public record CreateMeetingCommand(Guid TenantId, CreateMeetingRequest Dto) : IRequest<ServiceResult<MeetingDto>>;

public class CreateMeetingValidator : AbstractValidator<CreateMeetingCommand>
{
    public CreateMeetingValidator()
    {
        RuleFor(x => x.Dto.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Dto.ScheduledAt).NotEmpty();
    }
}

public class CreateMeetingCommandHandler(
    AppDbContext db,
    ILogger<CreateMeetingCommandHandler> log,
    IEmailService emailService,
    IConfiguration config)
    : IRequestHandler<CreateMeetingCommand, ServiceResult<MeetingDto>>
{
    public async Task<ServiceResult<MeetingDto>> Handle(CreateMeetingCommand request, CancellationToken ct)
    {
        var meeting = new Meeting
        {
            Id             = Guid.NewGuid(),
            TenantId       = request.TenantId,
            Title          = request.Dto.Title,
            Type           = request.Dto.Type,
            ScheduledAt    = DateTime.Parse(request.Dto.ScheduledAt),
            Location       = request.Dto.Location,
            Agenda         = request.Dto.Agenda,
            QuorumRequired = request.Dto.QuorumRequired,
            Status         = "scheduled",
        };

        db.Meetings.Add(meeting);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Meeting {Id} created: {Title}", meeting.Id, meeting.Title);

        // Notifier les membres actifs en background
        _ = NotifyMembersAsync(meeting, request.TenantId, config["App:BaseUrl"] ?? "https://app.expatriate365.mu", ct);

        return ServiceResult<MeetingDto>.Success(ListMeetingsQueryHandler.ToDto(meeting));
    }

    private async Task NotifyMembersAsync(Meeting meeting, Guid tenantId, string baseUrl, CancellationToken ct)
    {
        try
        {
            var users = await db.Users
                .Where(u => u.TenantId == tenantId && u.IsActive)
                .Select(u => new { FullName = u.FirstName + " " + u.LastName, NotifEmail = u.ContactEmail ?? u.Email })
                .ToListAsync(ct);

            var tenant    = await db.Tenants.FindAsync([tenantId], ct);
            var assocName = tenant?.Name ?? "Expatriate365";
            var agendaUrl = $"{baseUrl}/meetings/{meeting.Id}";
            var dateStr   = meeting.ScheduledAt.ToString("dddd d MMMM yyyy à HH:mm", new System.Globalization.CultureInfo("fr-FR"));
            var location  = meeting.Location ?? "À définir";

            var tasks = users.Select(u => emailService.SendAsync(
                u.NotifEmail,
                u.FullName,
                $"[{assocName}] Convocation : {meeting.Title}",
                EmailTemplates.MeetingConvocation(u.FullName, meeting.Title, dateStr, location, assocName, agendaUrl),
                ct));

            await Task.WhenAll(tasks);
            log.LogInformation("Meeting convocations sent to {Count} members for meeting {Id}", users.Count, meeting.Id);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to send meeting notifications for meeting {Id}", meeting.Id);
        }
    }
}
