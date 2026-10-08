using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;
using System.Text;

namespace server.Application.Finances.Queries;

public record ExportFinanceTransactionsQuery(
    Guid TenantId, string? Type = null, string? Status = null, string? From = null, string? To = null
) : IRequest<byte[]>;

public class ExportFinanceTransactionsQueryHandler(AppDbContext db)
    : IRequestHandler<ExportFinanceTransactionsQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportFinanceTransactionsQuery q, CancellationToken ct)
    {
        DateOnly? from = q.From is not null ? DateOnly.Parse(q.From) : null;
        DateOnly? to   = q.To   is not null ? DateOnly.Parse(q.To)   : null;

        var paymentsQ = db.Payments
            .Where(p => p.TenantId == q.TenantId && p.IsActive)
            .Where(p => q.Status == null || p.Status == q.Status)
            .Where(p => from == null || p.PaymentDate >= from)
            .Where(p => to   == null || p.PaymentDate <= to)
            .Select(p => new TxRow(
                "contribution",
                p.Member.User.FirstName + " " + p.Member.User.LastName,
                p.Member.MembershipNumber,
                p.Charge.ContributionType.Name,
                p.Amount, p.Currency, p.Status,
                p.PaymentDate, p.Notes));

        var welfareQ = db.WelfareRequests
            .Where(w => w.TenantId == q.TenantId && w.IsActive && w.AmountPaid.HasValue && w.AmountPaid > 0)
            .Where(w => q.Status == null || w.Status == q.Status)
            .Where(w => from == null || (w.PaidAt != null && DateOnly.FromDateTime(w.PaidAt.Value) >= from))
            .Where(w => to   == null || (w.PaidAt != null && DateOnly.FromDateTime(w.PaidAt.Value) <= to))
            .Select(w => new TxRow(
                "welfare",
                w.Member.User.FirstName + " " + w.Member.User.LastName,
                w.Member.MembershipNumber,
                w.Type,
                -(w.AmountPaid ?? 0), "EUR", w.Status,
                w.PaidAt.HasValue ? DateOnly.FromDateTime(w.PaidAt.Value) : DateOnly.MinValue,
                w.Type));

        var payments = q.Type == "welfare"       ? [] : await paymentsQ.ToListAsync(ct);
        var welfare  = q.Type == "contribution"  ? [] : await welfareQ.ToListAsync(ct);

        var rows = payments.Concat(welfare).OrderByDescending(t => t.Date).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("Type,Member,Membership #,Description,Date,Amount,Currency,Status");

        foreach (var r in rows)
        {
            sb.AppendLine(
                $"{Esc(r.Type)},{Esc(r.MemberName)},{Esc(r.MembershipNumber)}," +
                $"{Esc(r.Description)},{r.Date:yyyy-MM-dd}," +
                $"{r.Amount},{Esc(r.Currency)},{Esc(r.Status)}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Esc(string? v) => $"\"{(v ?? "").Replace("\"", "\"\"")}\"";

    private record TxRow(
        string Type, string MemberName, string MembershipNumber,
        string? Description, decimal Amount, string Currency, string Status,
        DateOnly Date, string? Notes);
}
