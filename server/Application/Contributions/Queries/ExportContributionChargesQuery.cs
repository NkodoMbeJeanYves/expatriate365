using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;
using System.Text;

namespace server.Application.Contributions.Queries;

public record ExportContributionChargesQuery(
    Guid TenantId, string? MemberId = null, string? TypeId = null, string? Status = null
) : IRequest<byte[]>;

public class ExportContributionChargesQueryHandler(AppDbContext db)
    : IRequestHandler<ExportContributionChargesQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportContributionChargesQuery q, CancellationToken ct)
    {
        var query = db.ContributionCharges
            .Include(c => c.Member).ThenInclude(m => m.User)
            .Include(c => c.ContributionType)
            .Where(c => c.TenantId == q.TenantId && c.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.MemberId) && Guid.TryParse(q.MemberId, out var mid))
            query = query.Where(c => c.MemberId == mid);

        if (!string.IsNullOrWhiteSpace(q.TypeId) && Guid.TryParse(q.TypeId, out var tid))
            query = query.Where(c => c.ContributionTypeId == tid);

        if (!string.IsNullOrWhiteSpace(q.Status))
            query = query.Where(c => c.Status == q.Status);

        var charges = await query.OrderByDescending(c => c.DueDate).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Member,Membership #,Plan,Due Date,Total Due,Amount Paid,Balance,Status");

        foreach (var c in charges)
        {
            sb.AppendLine(
                $"{Esc(c.Member.User.FirstName + " " + c.Member.User.LastName)}," +
                $"{Esc(c.Member.MembershipNumber)}," +
                $"{Esc(c.ContributionType.Name)}," +
                $"{c.DueDate:yyyy-MM-dd}," +
                $"{c.TotalDue},{c.AmountPaid},{c.Balance}," +
                $"{Esc(c.Status)}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Esc(string? v) => $"\"{(v ?? "").Replace("\"", "\"\"")}\"";
}
