using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Governance.DTOs;
using server.Application.Governance.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Governance.Commands;

// Board Members
public record CreateBoardMemberCommand(Guid TenantId, CreateBoardMemberRequest Request)
    : IRequest<ServiceResult<BoardMemberDto>>;

public class CreateBoardMemberCommandHandler(AppDbContext db)
    : IRequestHandler<CreateBoardMemberCommand, ServiceResult<BoardMemberDto>>
{
    public async Task<ServiceResult<BoardMemberDto>> Handle(CreateBoardMemberCommand request, CancellationToken ct)
    {
        var req = request.Request;
        var bm = new BoardMember
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            MemberId = Guid.Parse(req.MemberId),
            Role = req.Role,
            StartDate = DateOnly.Parse(req.StartDate),
            EndDate = req.EndDate is not null ? DateOnly.Parse(req.EndDate) : null,
            Notes = req.Notes,
        };
        db.BoardMembers.Add(bm);
        await db.SaveChangesAsync(ct);
        await db.Entry(bm).Reference(b => b.Member).Query().Include(m => m.User).LoadAsync(ct);

        return ServiceResult<BoardMemberDto>.Success(new BoardMemberDto(
            bm.Id.ToString(), bm.TenantId.ToString(), bm.MemberId.ToString(),
            $"{bm.Member.User.FirstName} {bm.Member.User.LastName}",
            bm.Member.MembershipNumber, bm.Role,
            bm.StartDate.ToString("O"), bm.EndDate?.ToString("O"), bm.Notes,
            bm.CreatedAt.ToString("O"), null));
    }
}

public record DeleteBoardMemberCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<bool>>;

public class DeleteBoardMemberCommandHandler(AppDbContext db)
    : IRequestHandler<DeleteBoardMemberCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeleteBoardMemberCommand request, CancellationToken ct)
    {
        var bm = await db.BoardMembers.FirstOrDefaultAsync(b => b.Id == request.Id && b.TenantId == request.TenantId, ct);
        if (bm is null) return ServiceResult<bool>.Failure("Membre introuvable.");
        bm.IsActive = false;
        await db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }
}

// Resolutions
public record CreateResolutionCommand(Guid TenantId, CreateResolutionRequest Request)
    : IRequest<ServiceResult<ResolutionDto>>;

public class CreateResolutionCommandHandler(AppDbContext db)
    : IRequestHandler<CreateResolutionCommand, ServiceResult<ResolutionDto>>
{
    public async Task<ServiceResult<ResolutionDto>> Handle(CreateResolutionCommand request, CancellationToken ct)
    {
        var req = request.Request;
        var res = new Resolution
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Title = req.Title,
            Content = req.Content,
            Status = "draft",
            MeetingId = req.MeetingId,
        };
        db.Resolutions.Add(res);
        await db.SaveChangesAsync(ct);
        return ServiceResult<ResolutionDto>.Success(ListResolutionsQueryHandler.ToDto(res));
    }
}

public record AdoptResolutionCommand(Guid TenantId, Guid Id, AdoptResolutionRequest Request)
    : IRequest<ServiceResult<ResolutionDto>>;

public class AdoptResolutionCommandHandler(AppDbContext db)
    : IRequestHandler<AdoptResolutionCommand, ServiceResult<ResolutionDto>>
{
    public async Task<ServiceResult<ResolutionDto>> Handle(AdoptResolutionCommand request, CancellationToken ct)
    {
        var res = await db.Resolutions.FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == request.TenantId, ct);
        if (res is null) return ServiceResult<ResolutionDto>.Failure("Résolution introuvable.");

        var req = request.Request;
        res.Status = "adopted";
        res.AdoptedAt = DateOnly.Parse(req.AdoptedAt);
        res.VotesFor = req.VotesFor;
        res.VotesAgainst = req.VotesAgainst;
        res.Abstentions = req.Abstentions;
        await db.SaveChangesAsync(ct);
        return ServiceResult<ResolutionDto>.Success(ListResolutionsQueryHandler.ToDto(res));
    }
}

public record DeleteResolutionCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<bool>>;

public class DeleteResolutionCommandHandler(AppDbContext db)
    : IRequestHandler<DeleteResolutionCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeleteResolutionCommand request, CancellationToken ct)
    {
        var res = await db.Resolutions.FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == request.TenantId, ct);
        if (res is null) return ServiceResult<bool>.Failure("Résolution introuvable.");
        res.IsActive = false;
        await db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }
}
