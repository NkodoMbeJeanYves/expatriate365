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

public record ReviewElectionCandidateCommand(
    Guid TenantId, Guid ElectionId, Guid CandidateId, Guid ReviewerId,
    ReviewElectionCandidateRequest Dto)
    : IRequest<ServiceResult<ElectionCandidateDto>>;

public class AddCandidateCommandHandler(AppDbContext db, ILogger<AddCandidateCommandHandler> log)
    : IRequestHandler<AddCandidateCommand, ServiceResult<ElectionCandidateDto>>
{
    public async Task<ServiceResult<ElectionCandidateDto>> Handle(AddCandidateCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(request.Dto.MemberId, out var memberId))
            return ServiceResult<ElectionCandidateDto>.Failure("MemberId invalide.", "errors.common.invalid_id");

        var election = await db.Elections.Include(e => e.Candidates)
            .FirstOrDefaultAsync(e => e.Id == request.ElectionId && e.TenantId == request.TenantId, ct);
        if (election is null) return ServiceResult<ElectionCandidateDto>.Failure("Élection introuvable.", "errors.election.not_found");
        if (election.Status != "draft") return ServiceResult<ElectionCandidateDto>.Failure("Les candidats ne peuvent être ajoutés qu'aux élections en brouillon.", "errors.election.invalid_status");
        if (election.Candidates.Any(c => c.MemberId == memberId && c.IsActive))
            return ServiceResult<ElectionCandidateDto>.Failure("Ce membre est déjà candidat.", "errors.candidate.already_exists");

        var member = await db.Members.Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.TenantId == request.TenantId, ct);
        if (member is null) return ServiceResult<ElectionCandidateDto>.Failure("Membre introuvable.", "errors.member.not_found");

        var candidate = new ElectionCandidate
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ElectionId = request.ElectionId,
            MemberId = memberId,
            Statement = request.Dto.Statement,
            DisplayOrder = request.Dto.DisplayOrder,
            Status = "pending",
        };

        db.ElectionCandidates.Add(candidate);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Candidate {MemberId} added to election {Id}", memberId, request.ElectionId);

        return ServiceResult<ElectionCandidateDto>.Success(new ElectionCandidateDto(
            candidate.Id.ToString(), candidate.ElectionId.ToString(), candidate.MemberId.ToString(),
            $"{member.User.FirstName} {member.User.LastName}", member.MembershipNumber,
            candidate.Statement, candidate.DisplayOrder, 0, 0, candidate.Status, null, null));
    }
}

public class ReviewElectionCandidateCommandHandler(AppDbContext db, ILogger<ReviewElectionCandidateCommandHandler> log)
    : IRequestHandler<ReviewElectionCandidateCommand, ServiceResult<ElectionCandidateDto>>
{
    public async Task<ServiceResult<ElectionCandidateDto>> Handle(ReviewElectionCandidateCommand request, CancellationToken ct)
    {
        var decision = request.Dto.Decision?.Trim().ToLowerInvariant();
        if (decision is not ("approved" or "rejected"))
            return ServiceResult<ElectionCandidateDto>.Failure("Invalid candidate decision.", "errors.candidate.invalid_decision");

        var reason = request.Dto.Reason?.Trim();
        if (decision == "rejected" && string.IsNullOrWhiteSpace(reason))
            return ServiceResult<ElectionCandidateDto>.Failure("A rejection reason is required.", "errors.candidate.review_reason_required");
        if (reason?.Length > 1000)
            return ServiceResult<ElectionCandidateDto>.Failure("The decision reason is too long.", "errors.candidate.review_reason_too_long");

        var candidate = await db.ElectionCandidates
            .Include(c => c.Election)
            .Include(c => c.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId
                && c.ElectionId == request.ElectionId
                && c.TenantId == request.TenantId
                && c.IsActive, ct);
        if (candidate is null)
            return ServiceResult<ElectionCandidateDto>.Failure("Candidate not found.", "errors.candidate.not_found");
        if (candidate.Election.Status != "draft")
            return ServiceResult<ElectionCandidateDto>.Failure("Candidates can only be reviewed before the election opens.", "errors.election.invalid_status");
        if (candidate.Status != "pending")
            return ServiceResult<ElectionCandidateDto>.Failure("This candidate has already been reviewed.", "errors.candidate.already_reviewed");
        if (candidate.Member.UserId == request.ReviewerId)
            return ServiceResult<ElectionCandidateDto>.Failure("You cannot review your own candidacy.", "errors.candidate.cannot_review_own");

        candidate.Status = decision;
        candidate.ReviewReason = decision == "rejected" ? reason : null;
        candidate.ReviewedBy = request.ReviewerId;
        candidate.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation(
            "Candidate {CandidateId} in election {ElectionId} was {Decision} by reviewer {ReviewerId}",
            candidate.Id, candidate.ElectionId, decision, request.ReviewerId);

        return ServiceResult<ElectionCandidateDto>.Success(new ElectionCandidateDto(
            candidate.Id.ToString(), candidate.ElectionId.ToString(), candidate.MemberId.ToString(),
            $"{candidate.Member.User.FirstName} {candidate.Member.User.LastName}",
            candidate.Member.MembershipNumber, candidate.Statement, candidate.DisplayOrder,
            0, 0, candidate.Status, candidate.ReviewReason, candidate.ReviewedAt?.ToString("O")));
    }
}

public class RemoveCandidateCommandHandler(AppDbContext db, ILogger<RemoveCandidateCommandHandler> log)
    : IRequestHandler<RemoveCandidateCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(RemoveCandidateCommand request, CancellationToken ct)
    {
        var candidate = await db.ElectionCandidates
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId && c.ElectionId == request.ElectionId && c.TenantId == request.TenantId, ct);
        if (candidate is null) return ServiceResult<bool>.Failure("Candidat introuvable.", "errors.candidate.not_found");

        var election = await db.Elections.FindAsync([request.ElectionId], ct);
        if (election?.Status != "draft") return ServiceResult<bool>.Failure("Les candidats ne peuvent être retirés qu'en phase brouillon.", "errors.election.invalid_status");

        candidate.IsActive = false;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Candidate {Id} removed from election {ElectionId}", request.CandidateId, request.ElectionId);
        return ServiceResult<bool>.Success(true);
    }
}
