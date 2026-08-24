using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.UserId).HasColumnName("user_id");
        b.Property(e => e.Type).HasColumnName("type").HasMaxLength(100).IsRequired();
        b.Property(e => e.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
        b.Property(e => e.Body).HasColumnName("body").HasColumnType("text");
        b.Property(e => e.IsRead).HasColumnName("is_read");
        b.Property(e => e.ReadAt).HasColumnName("read_at");
        b.Property(e => e.IsActive).HasColumnName("is_active");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(e => new { e.UserId, e.IsRead }).HasDatabaseName("IX_notifications_user_unread");
        b.HasIndex(e => e.TenantId).HasDatabaseName("IX_notifications_tenant_id");

        b.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
    }
}
