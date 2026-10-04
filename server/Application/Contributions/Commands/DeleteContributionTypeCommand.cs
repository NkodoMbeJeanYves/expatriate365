using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Contributions.Commands;

public record DeleteContributionTypeCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<bool>>;

public class DeleteContributionTypeCommandHandler(AppDbContext db, ILogger<DeleteContributionTypeCommandHandler> log)
    : IRequestHandler<DeleteContributionTypeCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeleteContributionTypeCommand request, CancellationToken ct)
    {
        var type = await db.ContributionTypes
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.TenantId == request.TenantId, ct);

        if (type is null)
            return ServiceResult<bool>.Failure("Plan de cotisation introuvable.", "errors.contribution_type.not_found");

        var hasActiveCharges = await db.ContributionCharges
            .AnyAsync(c => c.ContributionTypeId == request.Id && c.IsActive && c.Status != "paid" && c.Status != "waived", ct);

        if (hasActiveCharges)
            return ServiceResult<bool>.Failure("Ce plan a des cotisations actives non soldées. Clôturez-les avant de supprimer.", "errors.contribution_type.has_active_charges");

        type.IsActive = false;
        type.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("ContributionType {Id} deactivated (soft-delete)", request.Id);
        return ServiceResult<bool>.Success(true);
    }
}
