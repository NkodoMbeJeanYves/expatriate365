using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class TenantRoleConfiguration : IEntityTypeConfiguration<TenantRole>
{
    public void Configure(EntityTypeBuilder<TenantRole> builder)
    {
        builder.ToTable("tenant_roles");
        builder.HasKey(tr => tr.Id);
        builder.Property(tr => tr.Id).HasColumnName("id");
        builder.Property(tr => tr.TenantId).HasColumnName("tenant_id");
        builder.Property(tr => tr.RoleId).HasColumnName("role_id");
        builder.Property(tr => tr.Permissions).HasColumnName("permissions").HasColumnType("TEXT");
        builder.Property(tr => tr.IsCustomized).HasColumnName("is_customized").HasDefaultValue(false);
        builder.Property(tr => tr.CreatedAt).HasColumnName("created_at");
        builder.Property(tr => tr.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(tr => new { tr.TenantId, tr.RoleId }).IsUnique();

        builder.HasOne(tr => tr.Tenant)
            .WithMany()
            .HasForeignKey(tr => tr.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tr => tr.Role)
            .WithMany()
            .HasForeignKey(tr => tr.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
