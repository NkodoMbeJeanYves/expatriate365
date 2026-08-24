using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Welfare.DTOs;
using server.Application.Welfare.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Welfare.Commands;

public record CreateWelfareRequestCommand(Guid TenantId, CreateWelfareRequestRequest Dto)
    : IRequest<ServiceResult<WelfareRequestDto>>;

public class CreateWelfareRequestValidator : AbstractValidator<CreateWelfareRequestCommand>
{
    private static readonly string[] ValidTypes = ["death", "illness", "birth", "financial_hardship", "education", "other"];

    public CreateWelfareRequestValidator()
    {
        RuleFor(x => x.Dto.MemberId).NotEmpty();
        RuleFor(x => x.Dto.Type).Must(t => ValidTypes.Contains(t)).WithMessage("Type d'aide invalide.");
        RuleFor(x => x.Dto.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Dto.AmountRequested).GreaterThan(0);
    }
}

public class CreateWelfareRequestCommandHandler(AppDbContext db, ILogger<CreateWelfareRequestCommandHandler> log)
    : IRequestHandler<CreateWelfareRequestCommand, ServiceResult<WelfareRequestDto>>
{
    public async Task<ServiceResult<WelfareRequestDto>> Handle(CreateWelfareRequestCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(request.Dto.MemberId, out var memberId))
            return ServiceResult<WelfareRequestDto>.Failure("MemberId invalide.");

        var member = await db.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.TenantId == request.TenantId, ct);
        if (member is null) return ServiceResult<WelfareRequestDto>.Failure("Membre introuvable.");

        var welfare = new WelfareRequest
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            MemberId = memberId,
            Type = request.Dto.Type,
            Description = request.Dto.Description,
            AmountRequested = request.Dto.AmountRequested,
            Notes = request.Dto.Notes,
            Status = "pending",
        };

        db.WelfareRequests.Add(welfare);
        await db.SaveChangesAsync(ct);

        log.LogInformation("WelfareRequest {Id} created for member {MemberId}", welfare.Id, memberId);

        return ServiceResult<WelfareRequestDto>.Success(ListWelfareRequestsQueryHandler.ToDto(welfare));
    }
}
