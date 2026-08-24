using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Infrastructure.Persistence;
using System.Text;

namespace server.Application.Members.Queries;

public record ExportMembersQuery(Guid TenantId, string? Status = null) : IRequest<byte[]>;

public class ExportMembersQueryHandler(AppDbContext db) : IRequestHandler<ExportMembersQuery, byte[]>
{
    public async Task<byte[]> Handle(ExportMembersQuery q, CancellationToken ct)
    {
        var query = db.Members.Include(m => m.User).Include(m => m.Category)
            .Where(m => m.TenantId == q.TenantId && m.IsActive);

        if (!string.IsNullOrWhiteSpace(q.Status))
            query = query.Where(m => m.Status == q.Status);

        var members = await query.OrderBy(m => m.User.LastName).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Numéro,Prénom,Nom,Email,Téléphone,Statut,Catégorie,Date d'adhésion");
        foreach (var m in members)
        {
            sb.AppendLine($"{m.MembershipNumber},{m.User.FirstName},{m.User.LastName},{m.User.Email}," +
                          $"{m.User.Phone},{m.Status},{m.Category?.Name},{m.JoinedDate:yyyy-MM-dd}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}
