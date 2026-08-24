using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Elections.DTOs;
using server.Application.Elections.Queries;
using server.Domain.Entities;
using server.Infrastructure.Persistence;

namespace server.Application.Elections.Commands;

public record OpenElectionCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<ElectionDto>>;
public record CloseElectionCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<ElectionDto>>;
public record PublishResultsCommand(Guid TenantId, Guid Id) : IRequest<ServiceResult<ElectionDto>>;

public class OpenElectionCommandHandler(AppDbContext db, ILogger<OpenElectionCommandHandler> log)
    : IRequestHandler<OpenElectionCommand, ServiceResult<ElectionDto>>
{
    public async Task<ServiceResult<ElectionDto>> Handle(OpenElectionCommand request, CancellationToken ct)
    {
        var e = await db.Elections.Include(x => x.Candidates).Include(x => x.Votes)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);
        if (e is null) return ServiceResult<ElectionDto>.Failure("Élection introuvable.");
        if (e.Status != "draft") return ServiceResult<ElectionDto>.Failure("Seuls les brouillons peuvent être ouverts.");
        if (!e.Candidates.Any(c => c.IsActive))
            return ServiceResult<ElectionDto>.Failure("L'élection doit avoir au moins un candidat.");

        e.Status = "open";
        await db.SaveChangesAsync(ct);
        log.LogInformation("Election {Id} opened", e.Id);
        return ServiceResult<ElectionDto>.Success(ListElectionsQueryHandler.ToDto(e));
    }
}

public class CloseElectionCommandHandler(AppDbContext db, ILogger<CloseElectionCommandHandler> log)
    : IRequestHandler<CloseElectionCommand, ServiceResult<ElectionDto>>
{
    public async Task<ServiceResult<ElectionDto>> Handle(CloseElectionCommand request, CancellationToken ct)
    {
        var e = await db.Elections.Include(x => x.Candidates).Include(x => x.Votes)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);
        if (e is null) return ServiceResult<ElectionDto>.Failure("Élection introuvable.");
        if (e.Status != "open") return ServiceResult<ElectionDto>.Failure("Seules les élections ouvertes peuvent être clôturées.");

        e.Status = "closed";
        await db.SaveChangesAsync(ct);
        log.LogInformation("Election {Id} closed", e.Id);
        return ServiceResult<ElectionDto>.Success(ListElectionsQueryHandler.ToDto(e));
    }
}

public class PublishResultsCommandHandler(AppDbContext db, ILogger<PublishResultsCommandHandler> log)
    : IRequestHandler<PublishResultsCommand, ServiceResult<ElectionDto>>
{
    public async Task<ServiceResult<ElectionDto>> Handle(PublishResultsCommand request, CancellationToken ct)
    {
        var e = await db.Elections
            .Include(x => x.Candidates.Where(c => c.IsActive))
            .Include(x => x.Votes.Where(v => v.IsActive))
            .Include(x => x.Ballots)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);
        if (e is null) return ServiceResult<ElectionDto>.Failure("Élection introuvable.");
        if (e.Status != "closed") return ServiceResult<ElectionDto>.Failure("L'élection doit être clôturée avant de publier les résultats.");

        // Load encrypted vote choices from a shadow table (VoteCandidateChoices)
        // Since we preserve anonymity, we store choices separately in election_vote_choices
        var choices = await db.ElectionVoteChoices
            .Where(vc => vc.ElectionId == request.Id)
            .ToListAsync(ct);

        // Tally
        var tally = choices.GroupBy(c => c.CandidateId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Remove old ballots
        db.ElectionBallots.RemoveRange(e.Ballots);

        // Insert new ballots ranked
        int rank = 1;
        var ranked = tally.OrderByDescending(kv => kv.Value).ToList();
        foreach (var kv in ranked)
        {
            db.ElectionBallots.Add(new ElectionBallot
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ElectionId = request.Id,
                CandidateId = kv.Key,
                VoteCount = kv.Value,
                Rank = rank++,
            });
        }

        // Candidates with 0 votes
        foreach (var c in e.Candidates.Where(c => !tally.ContainsKey(c.Id)))
        {
            db.ElectionBallots.Add(new ElectionBallot
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ElectionId = request.Id,
                CandidateId = c.Id,
                VoteCount = 0,
                Rank = rank++,
            });
        }

        e.Status = "results_published";
        await db.SaveChangesAsync(ct);
        log.LogInformation("Election {Id} results published", e.Id);
        return ServiceResult<ElectionDto>.Success(ListElectionsQueryHandler.ToDto(e));
    }
}
