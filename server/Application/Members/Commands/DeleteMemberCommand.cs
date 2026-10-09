using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Members.Commands;

public record DeleteMemberCommand(Guid TenantId, Guid MemberId) : IRequest<ServiceResult<bool>>;

public class DeleteMemberCommandHandler(AppDbContext db, ILogger<DeleteMemberCommandHandler> log)
    : IRequestHandler<DeleteMemberCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeleteMemberCommand request, CancellationToken ct)
    {
        var member = await db.Members
            .Include(m => m.Payments)
            .FirstOrDefaultAsync(m => m.Id == request.MemberId && m.TenantId == request.TenantId, ct);

        if (member is null) return ServiceResult<bool>.Failure("Member not found.", "errors.member.not_found");

        var hasPendingPayments = member.Payments.Any(p => p.Status == "pending" || p.Status == "confirmed");
        if (hasPendingPayments)
            return ServiceResult<bool>.Failure("Member has active payments and cannot be deleted.", "errors.member.has_payments");

        member.IsActive = false;
        member.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("Member {MemberId} soft-deleted by tenant {TenantId}", request.MemberId, request.TenantId);
        return ServiceResult<bool>.Success(true);
    }
}
