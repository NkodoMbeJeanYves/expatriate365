using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Communications.Commands;

public record MarkReadCommand(Guid TenantId, Guid CommunicationId, Guid MemberId)
    : IRequest<ServiceResult<bool>>;

public class MarkReadCommandHandler(AppDbContext db)
    : IRequestHandler<MarkReadCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(MarkReadCommand request, CancellationToken ct)
    {
        var recipient = await db.CommunicationRecipients
            .FirstOrDefaultAsync(r =>
                r.CommunicationId == request.CommunicationId &&
                r.MemberId == request.MemberId &&
                r.TenantId == request.TenantId, ct);

        if (recipient is null)
            return ServiceResult<bool>.Failure("Destinataire introuvable.");

        if (recipient.Status == "read")
            return ServiceResult<bool>.Success(true);

        recipient.Status = "read";
        recipient.ReadAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return ServiceResult<bool>.Success(true);
    }
}
