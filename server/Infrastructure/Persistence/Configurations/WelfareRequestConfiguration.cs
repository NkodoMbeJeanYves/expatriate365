using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class WelfareRequestConfiguration : IEntityTypeConfiguration<WelfareRequest>
{
    public void Configure(EntityTypeBuilder<WelfareRequest> b)
    {
        b.ToTable("welfare_requests");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.MemberId).HasColumnName("member_id");
        b.Property(e => e.Type).HasColumnName("type").HasMaxLength(50).IsRequired();
        b.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000).IsRequired();
        b.Property(e => e.AmountRequested).HasColumnName("amount_requested").HasColumnType("decimal(18,2)");
        b.Property(e => e.AmountApproved).HasColumnName("amount_approved").HasColumnType("decimal(18,2)");
        b.Property(e => e.AmountPaid).HasColumnName("amount_paid").HasColumnType("decimal(18,2)");
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
        b.Property(e => e.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(1000);
        b.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
        b.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
        b.Property(e => e.PaidBy).HasColumnName("paid_by");
        b.Property(e => e.PaidAt).HasColumnName("paid_at");
        b.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(1000);
        b.Property(e => e.IsActive).HasColumnName("is_active");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId);
    }
}
