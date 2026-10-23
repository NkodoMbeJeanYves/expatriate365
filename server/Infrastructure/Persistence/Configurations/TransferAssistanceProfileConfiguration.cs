using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class TransferAssistanceProfileConfiguration : IEntityTypeConfiguration<TransferAssistanceProfile>
{
    public void Configure(EntityTypeBuilder<TransferAssistanceProfile> b)
    {
        b.ToTable("transfer_assistance_profiles");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.MemberId).HasColumnName("member_id");
        b.Property(e => e.HelperOptedIn).HasColumnName("helper_opted_in").IsConcurrencyToken();
        b.Property(e => e.HelperVerificationStatus).HasColumnName("helper_verification_status").HasMaxLength(30).IsRequired().IsConcurrencyToken();
        b.Property(e => e.HelperReviewedBy).HasColumnName("helper_reviewed_by");
        b.Property(e => e.HelperReviewedAt).HasColumnName("helper_reviewed_at");
        b.Property(e => e.HelperVerificationReason).HasColumnName("helper_verification_reason").HasMaxLength(1000);
        b.Property(e => e.WitnessOptedIn).HasColumnName("witness_opted_in").IsConcurrencyToken();
        b.Property(e => e.WitnessVerificationStatus).HasColumnName("witness_verification_status").HasMaxLength(30).IsRequired().IsConcurrencyToken();
        b.Property(e => e.WitnessReviewedBy).HasColumnName("witness_reviewed_by");
        b.Property(e => e.WitnessReviewedAt).HasColumnName("witness_reviewed_at");
        b.Property(e => e.WitnessVerificationReason).HasColumnName("witness_verification_reason").HasMaxLength(1000);
        b.Property(e => e.IsActive).HasColumnName("is_active");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(e => new { e.TenantId, e.MemberId }).IsUnique();
        b.HasIndex(e => new { e.TenantId, e.HelperVerificationStatus, e.WitnessVerificationStatus });
        b.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId);
    }
}
