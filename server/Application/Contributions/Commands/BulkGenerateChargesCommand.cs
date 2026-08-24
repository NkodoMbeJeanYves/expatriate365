using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Contributions.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Contributions.Commands;

public record BulkGenerateChargesCommand(Guid TenantId, BulkGenerateRequest Dto)
    : IRequest<ServiceResult<int>>;

public class BulkGenerateChargesValidator : AbstractValidator<BulkGenerateChargesCommand>
{
    public BulkGenerateChargesValidator()
    {
        RuleFor(x => x.Dto.ContributionTypeId).NotEmpty();
        RuleFor(x => x.Dto.DueDate).NotEmpty();
    }
}

public class BulkGenerateChargesCommandHandler(AppDbContext db, ILogger<BulkGenerateChargesCommandHandler> log)
    : IRequestHandler<BulkGenerateChargesCommand, ServiceResult<int>>
{
    public async Task<ServiceResult<int>> Handle(BulkGenerateChargesCommand request, CancellationToken ct)
    {
        var dto = request.Dto;

        if (!Guid.TryParse(dto.ContributionTypeId, out var typeId))
            return ServiceResult<int>.Failure("ContributionTypeId invalide.");

        var type = await db.ContributionTypes
            .FirstOrDefaultAsync(t => t.Id == typeId && t.TenantId == request.TenantId, ct);
        if (type is null) return ServiceResult<int>.Failure("Plan de cotisation introuvable.");

        var dueDate = DateOnly.Parse(dto.DueDate);

        var activeMembers = await db.Members
            .Where(m => m.TenantId == request.TenantId && m.Status == "active" && m.IsActive)
            .Select(m => m.Id)
            .ToListAsync(ct);

        var existingMemberIds = await db.ContributionCharges
            .Where(c => c.TenantId == request.TenantId
                     && c.ContributionTypeId == typeId
                     && c.DueDate == dueDate
                     && c.IsActive)
            .Select(c => c.MemberId)
            .ToListAsync(ct);

        var toGenerate = activeMembers.Except(existingMemberIds).ToList();

        var charges = toGenerate.Select(memberId => new ContributionCharge
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            MemberId = memberId,
            ContributionTypeId = typeId,
            DueDate = dueDate,
            BaseAmount = type.BaseAmount,
            PenaltyAmount = 0,
            WaiverAmount = 0,
            AmountPaid = 0,
            Status = "pending",
        }).ToList();

        db.ContributionCharges.AddRange(charges);
        await db.SaveChangesAsync(ct);

        log.LogInformation("Bulk generated {Count} charges for type {TypeId}, due {DueDate}",
            charges.Count, typeId, dueDate);

        return ServiceResult<int>.Success(charges.Count);
    }
}
