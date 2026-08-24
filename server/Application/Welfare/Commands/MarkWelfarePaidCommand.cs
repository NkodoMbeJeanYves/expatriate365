using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Welfare.DTOs;
using server.Application.Welfare.Queries;
using server.Infrastructure.Persistence;

namespace server.Application.Welfare.Commands;

public record MarkWelfarePaidCommand(Guid TenantId, Guid Id, Guid PaidBy)
    : IRequest<ServiceResult<WelfareRequestDto>>;

public class MarkWelfarePaidCommandHandler(AppDbContext db, ILogger<MarkWelfarePaidCommandHandler> log)
    : IRequestHandler<MarkWelfarePaidCommand, ServiceResult<WelfareRequestDto>>
{
    public async Task<ServiceResult<WelfareRequestDto>> Handle(MarkWelfarePaidCommand request, CancellationToken ct)
    {
        var welfare = await db.WelfareRequests
            .Include(w => w.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.Id == request.Id && w.TenantId == request.TenantId, ct);

        if (welfare is null) return ServiceResult<WelfareRequestDto>.Failure("Demande introuvable.");
        if (welfare.Status != "approved") return ServiceResult<WelfareRequestDto>.Failure("Seules les demandes approuvées peuvent être marquées versées.");

        welfare.Status = "paid";
        welfare.AmountPaid = welfare.AmountApproved;
        welfare.PaidBy = request.PaidBy;
        welfare.PaidAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("WelfareRequest {Id} marked paid", request.Id);

        return ServiceResult<WelfareRequestDto>.Success(ListWelfareRequestsQueryHandler.ToDto(welfare));
    }
}
