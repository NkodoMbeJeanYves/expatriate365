using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Elections.DTOs;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Elections.Commands;

public record CastVoteCommand(Guid TenantId, Guid ElectionId, Guid VoterId, CastVoteRequest Dto)
    : IRequest<ServiceResult<bool>>;

public class CastVoteCommandHandler(AppDbContext db, ILogger<CastVoteCommandHandler> log)
    : IRequestHandler<CastVoteCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(CastVoteCommand request, CancellationToken ct)
    {
        var election = await db.Elections.Include(e => e.Candidates.Where(c => c.IsActive))
            .FirstOrDefaultAsync(e => e.Id == request.ElectionId && e.TenantId == request.TenantId, ct);
        if (election is null) return ServiceResult<bool>.Failure("Élection introuvable.");
        if (election.Status != "open") return ServiceResult<bool>.Failure("L'élection n'est pas ouverte au vote.");

        if (election.Type == "board")
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == request.VoterId && m.TenantId == request.TenantId && m.IsActive, ct);
            var isBoardMember = member is not null && await db.BoardMembers.AnyAsync(
                b => b.MemberId == member.Id && b.IsActive && b.StartDate <= today && (b.EndDate == null || b.EndDate >= today), ct);
            if (!isBoardMember)
                return ServiceResult<bool>.Failure("Seuls les membres actifs du bureau exécutif peuvent voter pour cette élection.");
        }

        var alreadyVoted = await db.ElectionVotes.AnyAsync(
            v => v.ElectionId == request.ElectionId && v.VoterId == request.VoterId && v.IsActive, ct);
        if (alreadyVoted) return ServiceResult<bool>.Failure("Vous avez déjà voté pour cette élection.");

        var candidateIds = request.Dto.CandidateIds
            .Select(id => Guid.TryParse(id, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue).Select(g => g!.Value).ToList();

        if (candidateIds.Count == 0) return ServiceResult<bool>.Failure("Aucun candidat sélectionné.");
        if (candidateIds.Count > election.MaxChoices)
            return ServiceResult<bool>.Failure($"Vous ne pouvez voter que pour {election.MaxChoices} candidat(s).");

        var validIds = election.Candidates.Select(c => c.Id).ToHashSet();
        if (candidateIds.Any(id => !validIds.Contains(id)))
            return ServiceResult<bool>.Failure("Un ou plusieurs candidats sont invalides.");

        // Record that voter voted (not for whom)
        db.ElectionVotes.Add(new ElectionVote
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ElectionId = request.ElectionId,
            VoterId = request.VoterId,
        });

        // Record anonymous choices
        foreach (var candidateId in candidateIds)
        {
            db.ElectionVoteChoices.Add(new ElectionVoteChoice
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ElectionId = request.ElectionId,
                CandidateId = candidateId,
            });
        }

        await db.SaveChangesAsync(ct);
        log.LogInformation("Vote cast in election {Id} by voter {VoterId}", request.ElectionId, request.VoterId);
        return ServiceResult<bool>.Success(true);
    }
}
