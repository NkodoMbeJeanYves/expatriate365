using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Welfare.DTOs;
using server.Application.Welfare.Queries;
using server.Infrastructure.Persistence;

namespace server.Application.Welfare.Commands;

public record RejectWelfareRequestCommand(Guid TenantId, Guid Id, Guid ReviewedBy, RejectWelfareRequestRequest Dto)
    : IRequest<ServiceResult<WelfareRequestDto>>;

public class RejectWelfareRequestValidator : AbstractValidator<RejectWelfareRequestCommand>
{
    public RejectWelfareRequestValidator()
    {
        RuleFor(x => x.Dto.Reason).NotEmpty().MaximumLength(1000);
    }
}

public class RejectWelfareRequestCommandHandler(AppDbContext db, ILogger<RejectWelfareRequestCommandHandler> log)
    : IRequestHandler<RejectWelfareRequestCommand, ServiceResult<WelfareRequestDto>>
{
    public async Task<ServiceResult<WelfareRequestDto>> Handle(RejectWelfareRequestCommand request, CancellationToken ct)
    {
        var welfare = await db.WelfareRequests
            .Include(w => w.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.Id == request.Id && w.TenantId == request.TenantId, ct);

        if (welfare is null) return ServiceResult<WelfareRequestDto>.Failure("Demande introuvable.");
        if (welfare.Status != "pending") return ServiceResult<WelfareRequestDto>.Failure("Seules les demandes en attente peuvent être rejetées.");

        welfare.Status = "rejected";
        welfare.RejectionReason = request.Dto.Reason;
        welfare.ReviewedBy = request.ReviewedBy;
        welfare.ReviewedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("WelfareRequest {Id} rejected", request.Id);

        return ServiceResult<WelfareRequestDto>.Success(ListWelfareRequestsQueryHandler.ToDto(welfare));
    }
}
