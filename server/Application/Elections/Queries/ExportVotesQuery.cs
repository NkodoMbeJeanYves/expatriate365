using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;
using System.Text;

namespace server.Application.Elections.Queries;

public record ExportVotesQuery(Guid TenantId, Guid ElectionId) : IRequest<byte[]>;

public class ExportVotesQueryHandler(AppDbContext db) : IRequestHandler<ExportVotesQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportVotesQuery q, CancellationToken ct)
    {
        var election = await db.Elections
            .FirstOrDefaultAsync(e => e.Id == q.ElectionId && e.TenantId == q.TenantId && e.IsActive, ct);

        if (election is null) return [];

        var candidates = await db.ElectionCandidates
            .Include(c => c.Member).ThenInclude(m => m.User)
            .Where(c => c.ElectionId == q.ElectionId && c.TenantId == q.TenantId && c.IsActive)
            .ToListAsync(ct);

        var choices = await db.ElectionVoteChoices
            .Where(vc => vc.ElectionId == q.ElectionId && vc.TenantId == q.TenantId && vc.IsActive)
            .ToListAsync(ct);

        var totalVotes = await db.ElectionVotes
            .CountAsync(v => v.ElectionId == q.ElectionId && v.TenantId == q.TenantId && v.IsActive, ct);

        var sb = new StringBuilder();
        sb.AppendLine($"Election: {election.Title}");
        sb.AppendLine($"Status: {election.Status}");
        sb.AppendLine($"Total voters: {totalVotes}");
        sb.AppendLine();
        sb.AppendLine("Rank,Candidate,Votes Received,Percentage");

        var results = candidates
            .Select(c => new {
                Name = $"{c.Member.User.FirstName} {c.Member.User.LastName}",
                Votes = choices.Count(ch => ch.CandidateId == c.Id),
            })
            .OrderByDescending(r => r.Votes)
            .Select((r, i) => (Rank: i + 1, r.Name, r.Votes))
            .ToList();

        foreach (var r in results)
        {
            var pct = totalVotes > 0 ? Math.Round((double)r.Votes / totalVotes * 100, 1) : 0;
            sb.AppendLine($"{r.Rank},{Esc(r.Name)},{r.Votes},{pct}%");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Esc(string? v) => $"\"{(v ?? "").Replace("\"", "\"\"")}\"";
}
