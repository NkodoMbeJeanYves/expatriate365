using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Common;
using server.Infrastructure.Persistence;

namespace server.Application.Members.Commands;

public record DeleteCategoryCommand(Guid TenantId, Guid CategoryId)
    : IRequest<ServiceResult<bool>>;

public class DeleteCategoryCommandHandler(AppDbContext db)
    : IRequestHandler<DeleteCategoryCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var cat = await db.MembershipCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.TenantId == request.TenantId, ct);

        if (cat is null) return ServiceResult<bool>.Failure("Catégorie introuvable.");

        var hasMembers = await db.Members.AnyAsync(m => m.CategoryId == request.CategoryId && m.IsActive, ct);
        if (hasMembers) return ServiceResult<bool>.Failure("Cette catégorie est utilisée par des membres actifs.");

        cat.IsActive = false;
        cat.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return ServiceResult<bool>.Success(true);
    }
}
