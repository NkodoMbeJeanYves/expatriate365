using FluentValidation;
using MediatR;
using server.Application.Common;
using server.Application.Elections.DTOs;
using server.Application.Elections.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Elections.Commands;

public record CreateElectionCommand(Guid TenantId, CreateElectionRequest Dto) : IRequest<ServiceResult<ElectionDto>>;

public class CreateElectionValidator : AbstractValidator<CreateElectionCommand>
{
    public CreateElectionValidator()
    {
        RuleFor(x => x.Dto.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Dto.MaxChoices).GreaterThan(0);
    }
}

public class CreateElectionCommandHandler(AppDbContext db, ILogger<CreateElectionCommandHandler> log)
    : IRequestHandler<CreateElectionCommand, ServiceResult<ElectionDto>>
{
    public async Task<ServiceResult<ElectionDto>> Handle(CreateElectionCommand request, CancellationToken ct)
    {
        var election = new Election
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Title = request.Dto.Title,
            Description = request.Dto.Description,
            Type = request.Dto.Type,
            StartDate = request.Dto.StartDate is not null ? DateTime.Parse(request.Dto.StartDate) : null,
            EndDate = request.Dto.EndDate is not null ? DateTime.Parse(request.Dto.EndDate) : null,
            MaxChoices = request.Dto.MaxChoices,
            Status = "draft",
        };
        db.Elections.Add(election);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Election {Id} created: {Title}", election.Id, election.Title);
        return ServiceResult<ElectionDto>.Success(ListElectionsQueryHandler.ToDto(election));
    }
}
