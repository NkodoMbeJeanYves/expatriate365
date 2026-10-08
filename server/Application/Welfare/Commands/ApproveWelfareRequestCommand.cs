using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Welfare.DTOs;
using server.Application.Welfare.Queries;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

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

public class ApproveWelfareRequestCommandHandler(AppDbContext db, ILogger<ApproveWelfareRequestCommandHandler> log, INotificationService notif)
    : IRequestHandler<ApproveWelfareRequestCommand, ServiceResult<WelfareRequestDto>>
{
    public async Task<ServiceResult<WelfareRequestDto>> Handle(ApproveWelfareRequestCommand request, CancellationToken ct)
    {
        var welfare = await db.WelfareRequests
            .Include(w => w.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.Id == request.Id && w.TenantId == request.TenantId, ct);

        if (welfare is null) return ServiceResult<WelfareRequestDto>.Failure("Demande introuvable.", "errors.welfare.not_found");
        if (welfare.Status != "pending") return ServiceResult<WelfareRequestDto>.Failure("Seules les demandes en attente peuvent être approuvées.", "errors.welfare.invalid_status");

        welfare.Status = "approved";
        welfare.AmountApproved = request.Dto.AmountApproved;
        welfare.Notes = request.Dto.Notes ?? welfare.Notes;
        welfare.ReviewedBy = request.ReviewedBy;
        welfare.ReviewedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("WelfareRequest {Id} approved for {Amount}", request.Id, request.Dto.AmountApproved);

        var lang = welfare.Member.User.PreferredLanguage ?? "fr";
        _ = notif.NotifyAsync(welfare.TenantId, welfare.Member.UserId,
            "welfare_update",
            lang == "fr" ? "Demande d'aide approuvée" : "Welfare request approved",
            lang == "fr"
                ? $"Votre demande d'aide a été approuvée. Montant accordé : {request.Dto.AmountApproved}."
                : $"Your welfare request has been approved. Amount granted: {request.Dto.AmountApproved}.",
            ct, "welfare", welfare.Id.ToString());

        return ServiceResult<WelfareRequestDto>.Success(ListWelfareRequestsQueryHandler.ToDto(welfare));
    }
}
