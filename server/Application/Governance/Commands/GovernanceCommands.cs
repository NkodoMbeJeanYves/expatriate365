using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Governance.DTOs;
using server.Application.Governance.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Governance.Commands;

// ── Board Roles ──────────────────────────────────────────────────────────────

public record CreateBoardRoleCommand(Guid TenantId, CreateBoardRoleRequest Request)
    : IRequest<ServiceResult<BoardRoleDto>>;

public class CreateBoardRoleCommandHandler(AppDbContext db, ILogger<CreateBoardRoleCommandHandler> log)
    : IRequestHandler<CreateBoardRoleCommand, ServiceResult<BoardRoleDto>>
{
    public async Task<ServiceResult<BoardRoleDto>> Handle(CreateBoardRoleCommand request, CancellationToken ct)
    {
        var req = request.Request;
        var name = req.Name.ToLowerInvariant().Trim();

        if (await db.BoardRoles.AnyAsync(r => r.TenantId == request.TenantId && r.Name == name, ct))
            return ServiceResult<BoardRoleDto>.Failure("Un rôle avec ce nom existe déjà.", "errors.role.name_taken");

        var role = new BoardRole
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Name = name,
            Label = req.Label.Trim(),
        };
        db.BoardRoles.Add(role);
        await db.SaveChangesAsync(ct);

        log.LogInformation("BoardRole '{Name}' created for tenant {TenantId}", name, request.TenantId);
        return ServiceResult<BoardRoleDto>.Success(ToDto(role));
    }

    internal static BoardRoleDto ToDto(BoardRole r) => new(
        r.Id.ToString(), r.TenantId.ToString(), r.Name, r.Label, r.IsActive,
        r.CreatedAt.ToString("O"), r.UpdatedAt?.ToString("O"));
}

public record UpdateBoardRoleCommand(Guid TenantId, Guid RoleId, UpdateBoardRoleRequest Request)
    : IRequest<ServiceResult<BoardRoleDto>>;

public class UpdateBoardRoleCommandHandler(AppDbContext db, ILogger<UpdateBoardRoleCommandHandler> log)
    : IRequestHandler<UpdateBoardRoleCommand, ServiceResult<BoardRoleDto>>
{
    public async Task<ServiceResult<BoardRoleDto>> Handle(UpdateBoardRoleCommand request, CancellationToken ct)
    {
        var role = await db.BoardRoles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.TenantId == request.TenantId, ct);
        if (role is null) return ServiceResult<BoardRoleDto>.Failure("Rôle introuvable.", "errors.role.not_found");

        var name = request.Request.Name.ToLowerInvariant().Trim();
        if (name != role.Name && await db.BoardRoles.AnyAsync(
                r => r.TenantId == request.TenantId && r.Name == name && r.Id != request.RoleId, ct))
            return ServiceResult<BoardRoleDto>.Failure("Un rôle avec ce nom existe déjà.", "errors.role.name_taken");

        role.Name = name;
        role.Label = request.Request.Label.Trim();
        role.IsActive = request.Request.IsActive;
        await db.SaveChangesAsync(ct);

        log.LogInformation("BoardRole {RoleId} updated for tenant {TenantId}", request.RoleId, request.TenantId);
        return ServiceResult<BoardRoleDto>.Success(CreateBoardRoleCommandHandler.ToDto(role));
    }
}

public record DeleteBoardRoleCommand(Guid TenantId, Guid RoleId) : IRequest<ServiceResult<bool>>;

public class DeleteBoardRoleCommandHandler(AppDbContext db, ILogger<DeleteBoardRoleCommandHandler> log)
    : IRequestHandler<DeleteBoardRoleCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeleteBoardRoleCommand request, CancellationToken ct)
    {
        var role = await db.BoardRoles
            .FirstOrDefaultAsync(r => r.Id == request.RoleId && r.TenantId == request.TenantId, ct);
        if (role is null) return ServiceResult<bool>.Failure("Rôle introuvable.", "errors.role.not_found");

        var inUse = await db.BoardMembers
            .AnyAsync(b => b.RoleId == request.RoleId && b.IsActive, ct);
        if (inUse)
            return ServiceResult<bool>.Failure("Ce rôle est utilisé par des membres actifs du bureau.", "errors.role.in_use");

        role.IsActive = false;
        await db.SaveChangesAsync(ct);

        log.LogInformation("BoardRole {RoleId} deactivated for tenant {TenantId}", request.RoleId, request.TenantId);
        return ServiceResult<bool>.Success(true);
    }
}

// ── Board Members ────────────────────────────────────────────────────────────

public record CreateBoardMemberCommand(Guid TenantId, CreateBoardMemberRequest Request)
    : IRequest<ServiceResult<BoardMemberDto>>;

public class CreateBoardMemberCommandHandler(AppDbContext db)
    : IRequestHandler<CreateBoardMemberCommand, ServiceResult<BoardMemberDto>>
{
    public async Task<ServiceResult<BoardMemberDto>> Handle(CreateBoardMemberCommand request, CancellationToken ct)
    {
        var req = request.Request;

        Guid? roleId = null;
        BoardRole? boardRole = null;
        if (!string.IsNullOrWhiteSpace(req.RoleId) && Guid.TryParse(req.RoleId, out var parsedRoleId))
        {
            boardRole = await db.BoardRoles
                .FirstOrDefaultAsync(r => r.Id == parsedRoleId && r.TenantId == request.TenantId && r.IsActive, ct);
            if (boardRole is null)
                return ServiceResult<BoardMemberDto>.Failure("Rôle de bureau introuvable.", "errors.board_role.not_found");
            roleId = parsedRoleId;
        }

        var bm = new BoardMember
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            MemberId = Guid.Parse(req.MemberId),
            RoleId = roleId,
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
            bm.Member.MembershipNumber,
            roleId?.ToString(), boardRole?.Name, boardRole?.Label,
            bm.StartDate.ToString("yyyy-MM-dd"), bm.EndDate?.ToString("yyyy-MM-dd"),
            bm.Notes, bm.CreatedAt.ToString("O"), null));
    }
}

public record UpdateBoardMemberRequest(
    string? RoleId,
    string? StartDate,
    string? EndDate,
    bool? IsActive,
    string? Notes);

public record UpdateBoardMemberCommand(Guid TenantId, Guid BoardMemberId, UpdateBoardMemberRequest Dto)
    : IRequest<ServiceResult<BoardMemberDto>>;

public class UpdateBoardMemberCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateBoardMemberCommand, ServiceResult<BoardMemberDto>>
{
    public async Task<ServiceResult<BoardMemberDto>> Handle(UpdateBoardMemberCommand request, CancellationToken ct)
    {
        var bm = await db.BoardMembers
            .Include(b => b.Member).ThenInclude(m => m.User)
            .Include(b => b.BoardRole)
            .FirstOrDefaultAsync(b => b.Id == request.BoardMemberId && b.TenantId == request.TenantId, ct);

        if (bm is null) return ServiceResult<BoardMemberDto>.Failure("Board member not found.", "errors.board_member.not_found");

        var dto = request.Dto;

        if (dto.RoleId is not null)
        {
            if (Guid.TryParse(dto.RoleId, out var roleGuid))
            {
                var role = await db.BoardRoles
                    .FirstOrDefaultAsync(r => r.Id == roleGuid && r.TenantId == request.TenantId && r.IsActive, ct);
                if (role is null) return ServiceResult<BoardMemberDto>.Failure("Board role not found.", "errors.board_role.not_found");
                bm.RoleId = roleGuid;
                bm.BoardRole = role;
            }
            else
            {
                bm.RoleId = null;
                bm.BoardRole = null;
            }
        }
        if (dto.StartDate is not null) bm.StartDate = DateOnly.Parse(dto.StartDate);
        if (dto.EndDate is not null) bm.EndDate = DateOnly.Parse(dto.EndDate);
        if (dto.IsActive is not null) bm.IsActive = dto.IsActive.Value;
        if (dto.Notes is not null) bm.Notes = dto.Notes;
        bm.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return ServiceResult<BoardMemberDto>.Success(new BoardMemberDto(
            bm.Id.ToString(), bm.TenantId.ToString(), bm.MemberId.ToString(),
            $"{bm.Member.User.FirstName} {bm.Member.User.LastName}",
            bm.Member.MembershipNumber,
            bm.RoleId?.ToString(), bm.BoardRole?.Name, bm.BoardRole?.Label,
            bm.StartDate.ToString("yyyy-MM-dd"), bm.EndDate?.ToString("yyyy-MM-dd"),
            bm.Notes, bm.CreatedAt.ToString("O"), bm.UpdatedAt?.ToString("O")));
    }
}

public record DeleteBoardMemberCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<bool>>;

public class DeleteBoardMemberCommandHandler(AppDbContext db)
    : IRequestHandler<DeleteBoardMemberCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeleteBoardMemberCommand request, CancellationToken ct)
    {
        var bm = await db.BoardMembers
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.TenantId == request.TenantId, ct);
        if (bm is null) return ServiceResult<bool>.Failure("Membre introuvable.", "errors.member.not_found");
        bm.IsActive = false;
        await db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }
}

// ── Resolutions ──────────────────────────────────────────────────────────────

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
        var res = await db.Resolutions
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == request.TenantId, ct);
        if (res is null) return ServiceResult<ResolutionDto>.Failure("Résolution introuvable.", "errors.resolution.not_found");

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
        var res = await db.Resolutions
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == request.TenantId, ct);
        if (res is null) return ServiceResult<bool>.Failure("Résolution introuvable.", "errors.resolution.not_found");
        res.IsActive = false;
        await db.SaveChangesAsync(ct);
        return ServiceResult<bool>.Success(true);
    }
}
