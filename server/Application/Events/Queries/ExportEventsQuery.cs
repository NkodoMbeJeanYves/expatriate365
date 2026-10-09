using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;
using System.Text;

namespace server.Application.Events.Queries;

public record ExportEventsQuery(Guid TenantId, string? Status = null, string? Type = null) : IRequest<byte[]>;

public class ExportEventsQueryHandler(AppDbContext db) : IRequestHandler<ExportEventsQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportEventsQuery q, CancellationToken ct)
    {
        var query = db.Events
            .Include(e => e.Registrations)
            .Where(e => e.TenantId == q.TenantId && e.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Status))
            query = query.Where(e => e.Status == q.Status);

        if (!string.IsNullOrWhiteSpace(q.Type))
            query = query.Where(e => e.Type == q.Type);

        var events = await query.OrderByDescending(e => e.StartDate).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Title,Type,Status,Start Date,End Date,Location,Max Capacity,Registered,Is Public,Created At");

        foreach (var e in events)
        {
            var registered = e.Registrations.Count(r => r.IsActive && r.Status != "cancelled");
            sb.AppendLine(
                $"{Esc(e.Title)},{Esc(e.Type)},{Esc(e.Status)}," +
                $"{e.StartDate:yyyy-MM-dd HH:mm},{e.EndDate:yyyy-MM-dd HH:mm}," +
                $"{Esc(e.Location)},{e.MaxCapacity},{registered}," +
                $"{e.IsPublic},{e.CreatedAt:yyyy-MM-dd}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string Esc(string? v) => $"\"{(v ?? "").Replace("\"", "\"\"")}\"";
}
