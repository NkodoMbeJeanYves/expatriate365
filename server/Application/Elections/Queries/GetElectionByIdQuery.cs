using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Application.Elections.DTOs;
using server.Infrastructure.Persistence;

namespace server.Application.Elections.Queries;

public record GetElectionByIdQuery(Guid TenantId, Guid Id)
    : IRequest<ServiceResult<(ElectionDto Election, List<ElectionCandidateDto> Candidates, List<ElectionResultDto> Results)>>;

public class GetElectionByIdQueryHandler(AppDbContext db)
    : IRequestHandler<GetElectionByIdQuery, ServiceResult<(ElectionDto, List<ElectionCandidateDto>, List<ElectionResultDto>)>>
{
    public async Task<ServiceResult<(ElectionDto, List<ElectionCandidateDto>, List<ElectionResultDto>)>> Handle(
        GetElectionByIdQuery request, CancellationToken ct)
    {
        var e = await db.Elections
            .Include(x => x.Candidates.Where(c => c.IsActive)).ThenInclude(c => c.Member).ThenInclude(m => m.User)
            .Include(x => x.Votes.Where(v => v.IsActive))
            .Include(x => x.Ballots.Where(b => b.IsActive)).ThenInclude(b => b.Candidate).ThenInclude(c => c.Member).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (e is null)
            return ServiceResult<(ElectionDto, List<ElectionCandidateDto>, List<ElectionResultDto>)>.Failure("Élection introuvable.");

        var totalVotes = e.Votes.Count;
        var candidates = e.Candidates.OrderBy(c => c.DisplayOrder).Select(c =>
        {
            var ballot = e.Ballots.FirstOrDefault(b => b.CandidateId == c.Id);
            return new ElectionCandidateDto(
                c.Id.ToString(), c.ElectionId.ToString(), c.MemberId.ToString(),
                $"{c.Member.User.FirstName} {c.Member.User.LastName}", c.Member.MembershipNumber,
                c.Statement, c.DisplayOrder, ballot?.VoteCount ?? 0, ballot?.Rank ?? 0);
        }).ToList();

        var results = e.Ballots.OrderBy(b => b.Rank).Select(b =>
        {
            var pct = totalVotes > 0 ? Math.Round((double)b.VoteCount / totalVotes * 100, 1) : 0;
            return new ElectionResultDto(
                b.CandidateId.ToString(),
                $"{b.Candidate.Member.User.FirstName} {b.Candidate.Member.User.LastName}",
                b.Candidate.Member.MembershipNumber,
                b.VoteCount, b.Rank, pct);
        }).ToList();

        return ServiceResult<(ElectionDto, List<ElectionCandidateDto>, List<ElectionResultDto>)>
            .Success((ListElectionsQueryHandler.ToDto(e), candidates, results));
    }
}
