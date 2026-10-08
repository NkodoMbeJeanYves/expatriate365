using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Welfare.DTOs;
using server.Application.Welfare.Queries;
using server.Infrastructure.Persistence;
using server.Infrastructure.Services;

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

public class RejectWelfareRequestCommandHandler(AppDbContext db, ILogger<RejectWelfareRequestCommandHandler> log, INotificationService notif)
    : IRequestHandler<RejectWelfareRequestCommand, ServiceResult<WelfareRequestDto>>
{
    public async Task<ServiceResult<WelfareRequestDto>> Handle(RejectWelfareRequestCommand request, CancellationToken ct)
    {
        var welfare = await db.WelfareRequests
            .Include(w => w.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.Id == request.Id && w.TenantId == request.TenantId, ct);

        if (welfare is null) return ServiceResult<WelfareRequestDto>.Failure("Demande introuvable.", "errors.welfare.not_found");
        if (welfare.Status != "pending") return ServiceResult<WelfareRequestDto>.Failure("Seules les demandes en attente peuvent être rejetées.", "errors.welfare.invalid_status");

        welfare.Status = "rejected";
        welfare.RejectionReason = request.Dto.Reason;
        welfare.ReviewedBy = request.ReviewedBy;
        welfare.ReviewedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        log.LogInformation("WelfareRequest {Id} rejected", request.Id);

        var lang = welfare.Member.User.PreferredLanguage ?? "fr";
        _ = notif.NotifyAsync(welfare.TenantId, welfare.Member.UserId,
            "welfare_update",
            lang == "fr" ? "Demande d'aide refusée" : "Welfare request rejected",
            lang == "fr"
                ? $"Votre demande d'aide a été refusée. Motif : {request.Dto.Reason}"
                : $"Your welfare request has been rejected. Reason: {request.Dto.Reason}",
            ct, "welfare", welfare.Id.ToString());

        return ServiceResult<WelfareRequestDto>.Success(ListWelfareRequestsQueryHandler.ToDto(welfare));
    }
}
