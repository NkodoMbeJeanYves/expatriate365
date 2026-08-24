using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Elections.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Elections.Commands;

public record AddCandidateCommand(Guid TenantId, Guid ElectionId, AddCandidateRequest Dto)
    : IRequest<ServiceResult<ElectionCandidateDto>>;

public record RemoveCandidateCommand(Guid TenantId, Guid ElectionId, Guid CandidateId)
    : IRequest<ServiceResult<bool>>;

public class AddCandidateCommandHandler(AppDbContext db, ILogger<AddCandidateCommandHandler> log)
    : IRequestHandler<AddCandidateCommand, ServiceResult<ElectionCandidateDto>>
{
    public async Task<ServiceResult<ElectionCandidateDto>> Handle(AddCandidateCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(request.Dto.MemberId, out var memberId))
            return ServiceResult<ElectionCandidateDto>.Failure("MemberId invalide.");

        var election = await db.Elections.Include(e => e.Candidates)
            .FirstOrDefaultAsync(e => e.Id == request.ElectionId && e.TenantId == request.TenantId, ct);
        if (election is null) return ServiceResult<ElectionCandidateDto>.Failure("Élection introuvable.");
        if (election.Status != "draft") return ServiceResult<ElectionCandidateDto>.Failure("Les candidats ne peuvent être ajoutés qu'aux élections en brouillon.");
        if (election.Candidates.Any(c => c.MemberId == memberId && c.IsActive))
            return ServiceResult<ElectionCandidateDto>.Failure("Ce membre est déjà candidat.");

        var member = await db.Members.Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.TenantId == request.TenantId, ct);
        if (member is null) return ServiceResult<ElectionCandidateDto>.Failure("Membre introuvable.");

        var candidate = new ElectionCandidate
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ElectionId = request.ElectionId,
            MemberId = memberId,
            Statement = request.Dto.Statement,
            DisplayOrder = request.Dto.DisplayOrder,
        };

        db.ElectionCandidates.Add(candidate);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Candidate {MemberId} added to election {Id}", memberId, request.ElectionId);

        return ServiceResult<ElectionCandidateDto>.Success(new ElectionCandidateDto(
            candidate.Id.ToString(), candidate.ElectionId.ToString(), candidate.MemberId.ToString(),
            $"{member.User.FirstName} {member.User.LastName}", member.MembershipNumber,
            candidate.Statement, candidate.DisplayOrder, 0, 0));
    }
}

public class RemoveCandidateCommandHandler(AppDbContext db, ILogger<RemoveCandidateCommandHandler> log)
    : IRequestHandler<RemoveCandidateCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(RemoveCandidateCommand request, CancellationToken ct)
    {
        var candidate = await db.ElectionCandidates
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId && c.ElectionId == request.ElectionId && c.TenantId == request.TenantId, ct);
        if (candidate is null) return ServiceResult<bool>.Failure("Candidat introuvable.");

        var election = await db.Elections.FindAsync([request.ElectionId], ct);
        if (election?.Status != "draft") return ServiceResult<bool>.Failure("Les candidats ne peuvent être retirés qu'en phase brouillon.");

        candidate.IsActive = false;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Candidate {Id} removed from election {ElectionId}", request.CandidateId, request.ElectionId);
        return ServiceResult<bool>.Success(true);
    }
}
