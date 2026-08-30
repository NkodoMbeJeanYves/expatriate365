using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        b.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(100);
        b.Property(e => e.EntityId).HasColumnName("entity_id").HasMaxLength(36);
        b.Property(e => e.Meta).HasColumnName("meta");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");

        b.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(e => e.UserId);
        b.HasIndex(e => e.TenantId);
        b.HasIndex(e => e.CreatedAt);
    }
}
