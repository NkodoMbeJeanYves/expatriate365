using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Members.Commands;

public record PatchMemberStatusCommand(Guid TenantId, Guid MemberId, string Status)
    : IRequest<ServiceResult<bool>>;

public class PatchMemberStatusCommandHandler(AppDbContext db, ILogger<PatchMemberStatusCommandHandler> log)
    : IRequestHandler<PatchMemberStatusCommand, ServiceResult<bool>>
{
    private static readonly string[] ValidStatuses = ["active", "suspended", "inactive", "pending"];

    public async Task<ServiceResult<bool>> Handle(PatchMemberStatusCommand request, CancellationToken ct)
    {
        if (!ValidStatuses.Contains(request.Status))
            return ServiceResult<bool>.Failure($"Statut invalide. Valeurs acceptées : {string.Join(", ", ValidStatuses)}");

        var member = await db.Members
            .FirstOrDefaultAsync(m => m.Id == request.MemberId && m.TenantId == request.TenantId, ct);

        if (member is null) return ServiceResult<bool>.Failure("Membre introuvable.");

        member.Status = request.Status;
        await db.SaveChangesAsync(ct);

        log.LogInformation("Member {MemberId} status changed to {Status}", request.MemberId, request.Status);
        return ServiceResult<bool>.Success(true);
    }
}
