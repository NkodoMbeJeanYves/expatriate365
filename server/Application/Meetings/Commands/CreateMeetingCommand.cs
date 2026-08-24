using FluentValidation;
using MediatR;
using server.Application.Common;
using server.Application.Meetings.DTOs;
using server.Application.Meetings.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

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

public class CreateMeetingCommandHandler(AppDbContext db, ILogger<CreateMeetingCommandHandler> log)
    : IRequestHandler<CreateMeetingCommand, ServiceResult<MeetingDto>>
{
    public async Task<ServiceResult<MeetingDto>> Handle(CreateMeetingCommand request, CancellationToken ct)
    {
        var meeting = new Meeting
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Title = request.Dto.Title,
            Type = request.Dto.Type,
            ScheduledAt = DateTime.Parse(request.Dto.ScheduledAt),
            Location = request.Dto.Location,
            Agenda = request.Dto.Agenda,
            QuorumRequired = request.Dto.QuorumRequired,
            Status = "scheduled",
        };

        db.Meetings.Add(meeting);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Meeting {Id} created: {Title}", meeting.Id, meeting.Title);
        return ServiceResult<MeetingDto>.Success(ListMeetingsQueryHandler.ToDto(meeting));
    }
}
