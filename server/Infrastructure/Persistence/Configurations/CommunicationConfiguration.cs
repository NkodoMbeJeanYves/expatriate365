using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class CommunicationConfiguration : IEntityTypeConfiguration<Communication>
{
    public void Configure(EntityTypeBuilder<Communication> b)
    {
        b.ToTable("communications");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        b.Property(e => e.Content).HasColumnName("content").IsRequired();
        b.Property(e => e.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
        b.Property(e => e.Channel).HasColumnName("channel").HasMaxLength(50).IsRequired();
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        b.Property(e => e.Audience).HasColumnName("audience").HasMaxLength(50).IsRequired();
        b.Property(e => e.CategoryId).HasColumnName("category_id");
        b.Property(e => e.TargetMemberId).HasColumnName("target_member_id");
        b.Property(e => e.SentAt).HasColumnName("sent_at");
        b.Property(e => e.RecipientCount).HasColumnName("recipient_count").HasDefaultValue(0);
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(e => e.TenantId);
        b.HasIndex(e => e.Status);
    }
}

public class CommunicationRecipientConfiguration : IEntityTypeConfiguration<CommunicationRecipient>
{
    public void Configure(EntityTypeBuilder<CommunicationRecipient> b)
    {
        b.ToTable("communication_recipients");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        b.Property(e => e.CommunicationId).HasColumnName("communication_id").IsRequired();
        b.Property(e => e.MemberId).HasColumnName("member_id").IsRequired();
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        b.Property(e => e.ReadAt).HasColumnName("read_at");
        b.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.HasIndex(e => new { e.CommunicationId, e.MemberId }).IsUnique();
        b.HasOne(e => e.Communication).WithMany(c => c.Recipients).HasForeignKey(e => e.CommunicationId);
        b.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId);
    }
}
