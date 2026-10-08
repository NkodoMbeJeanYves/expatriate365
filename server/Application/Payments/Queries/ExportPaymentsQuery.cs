using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;
using System.Text;

namespace server.Application.Payments.Queries;

public record ExportPaymentsQuery(Guid TenantId, string? Status = null, string? From = null, string? To = null) : IRequest<byte[]>;

public class ExportPaymentsQueryHandler(AppDbContext db) : IRequestHandler<ExportPaymentsQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportPaymentsQuery q, CancellationToken ct)
    {
        var query = db.Payments
            .Include(p => p.Member).ThenInclude(m => m.User)
            .Include(p => p.Charge).ThenInclude(c => c.ContributionType)
            .Where(p => p.TenantId == q.TenantId && p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Status))
            query = query.Where(p => p.Status == q.Status);

        if (DateOnly.TryParse(q.From, out var fromDate))
            query = query.Where(p => p.PaymentDate >= fromDate);

        if (DateOnly.TryParse(q.To, out var toDate))
            query = query.Where(p => p.PaymentDate <= toDate);

        var payments = await query.OrderByDescending(p => p.PaymentDate).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Receipt #,Date,Member,Membership #,Contribution Plan,Amount,Currency,Method,Status,Confirmed At");

        foreach (var p in payments)
        {
            sb.AppendLine(
                $"{Esc(p.ReceiptNumber)},{p.PaymentDate:yyyy-MM-dd}," +
                $"{Esc(p.Member.User.FirstName + " " + p.Member.User.LastName)}," +
                $"{Esc(p.Member.MembershipNumber)}," +
                $"{Esc(p.Charge?.ContributionType?.Name)}," +
                $"{p.Amount},{Esc(p.Currency)},{Esc(p.PaymentGateway)},{Esc(p.Status)}," +
                $"{(p.ConfirmedAt.HasValue ? p.ConfirmedAt.Value.ToString("yyyy-MM-dd") : "")}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Esc(string? v) => $"\"{(v ?? "").Replace("\"", "\"\"")}\"";
}
