using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Elections.DTOs;
using server.Application.Elections.Queries;
using server.Infrastructure.Persistence;

namespace server.Application.Elections.Commands;

public record UpdateElectionCommand(Guid TenantId, Guid Id, UpdateElectionRequest Dto) : IRequest<ServiceResult<ElectionDto>>;

public class UpdateElectionCommandHandler(AppDbContext db, ILogger<UpdateElectionCommandHandler> log)
    : IRequestHandler<UpdateElectionCommand, ServiceResult<ElectionDto>>
{
    public async Task<ServiceResult<ElectionDto>> Handle(UpdateElectionCommand request, CancellationToken ct)
    {
        var e = await db.Elections.Include(x => x.Candidates).Include(x => x.Votes)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);
        if (e is null) return ServiceResult<ElectionDto>.Failure("Élection introuvable.");
        if (e.Status is "open" or "closed" or "results_published")
            return ServiceResult<ElectionDto>.Failure("Impossible de modifier une élection ouverte ou clôturée.");

        e.Title = request.Dto.Title;
        e.Description = request.Dto.Description;
        e.Type = request.Dto.Type;
        e.StartDate = request.Dto.StartDate is not null ? DateTime.Parse(request.Dto.StartDate) : null;
        e.EndDate = request.Dto.EndDate is not null ? DateTime.Parse(request.Dto.EndDate) : null;
        e.MaxChoices = request.Dto.MaxChoices;

        await db.SaveChangesAsync(ct);
        log.LogInformation("Election {Id} updated", e.Id);
        return ServiceResult<ElectionDto>.Success(ListElectionsQueryHandler.ToDto(e));
    }
}
