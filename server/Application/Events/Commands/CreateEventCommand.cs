using FluentValidation;
using MediatR;
using server.Application.Common;
using server.Application.Events.DTOs;
using server.Application.Events.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

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

public class CreateEventCommandHandler(AppDbContext db, ILogger<CreateEventCommandHandler> log)
    : IRequestHandler<CreateEventCommand, ServiceResult<EventDto>>
{
    public async Task<ServiceResult<EventDto>> Handle(CreateEventCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var start = DateTime.Parse(dto.StartDate);
        var end = DateTime.Parse(dto.EndDate);

        if (end <= start)
            return ServiceResult<EventDto>.Failure("La date de fin doit être postérieure à la date de début.");

        var ev = new Event
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Title = dto.Title,
            Description = dto.Description,
            Type = dto.Type,
            Location = dto.Location,
            StartDate = start,
            EndDate = end,
            MaxCapacity = dto.MaxCapacity,
            IsPublic = dto.IsPublic,
            Status = "draft",
        };

        db.Events.Add(ev);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Event {Id} created: {Title}", ev.Id, ev.Title);

        return ServiceResult<EventDto>.Success(ListEventsQueryHandler.ToDto(ev));
    }
}
