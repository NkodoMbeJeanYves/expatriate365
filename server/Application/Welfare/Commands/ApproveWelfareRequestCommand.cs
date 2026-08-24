using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Welfare.DTOs;
using server.Application.Welfare.Queries;
using server.Infrastructure.Persistence;

namespace server.Application.Welfare.Commands;

public record ApproveWelfareRequestCommand(Guid TenantId, Guid Id, Guid ReviewedBy, ApproveWelfareRequestRequest Dto)
    : IRequest<ServiceResult<WelfareRequestDto>>;

public class ApproveWelfareRequestValidator : AbstractValidator<ApproveWelfareRequestCommand>
{
    public ApproveWelfareRequestValidator()
    {
        RuleFor(x => x.Dto.AmountApproved).GreaterThan(0);
    }
}

public class ApproveWelfareRequestCommandHandler(AppDbContext db, ILogger<ApproveWelfareRequestCommandHandler> log)
    : IRequestHandler<ApproveWelfareRequestCommand, ServiceResult<WelfareRequestDto>>
{
    public async Task<ServiceResult<WelfareRequestDto>> Handle(ApproveWelfareRequestCommand request, CancellationToken ct)
    {
        var welfare = await db.WelfareRequests
            .Include(w => w.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.Id == request.Id && w.TenantId == request.TenantId, ct);

        if (welfare is null) return ServiceResult<WelfareRequestDto>.Failure("Demande introuvable.");
        if (welfare.Status != "pending") return ServiceResult<WelfareRequestDto>.Failure("Seules les demandes en attente peuvent être approuvées.");

        welfare.Status = "approved";
        welfare.AmountApproved = request.Dto.AmountApproved;
        welfare.Notes = request.Dto.Notes ?? welfare.Notes;
        welfare.ReviewedBy = request.ReviewedBy;
        welfare.ReviewedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("WelfareRequest {Id} approved for {Amount}", request.Id, request.Dto.AmountApproved);

        return ServiceResult<WelfareRequestDto>.Success(ListWelfareRequestsQueryHandler.ToDto(welfare));
    }
}
