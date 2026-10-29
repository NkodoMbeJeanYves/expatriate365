using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class TransferAssistanceBeneficiaryCandidateConfiguration
    : IEntityTypeConfiguration<TransferAssistanceBeneficiaryCandidate>
{
    public void Configure(EntityTypeBuilder<TransferAssistanceBeneficiaryCandidate> b)
    {
        b.ToTable("transfer_assistance_beneficiary_candidates");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.RequestId).HasColumnName("request_id");
        b.Property(e => e.MemberId).HasColumnName("member_id");
        b.Property(e => e.Message).HasColumnName("message").HasMaxLength(300);
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        b.Property(e => e.IsActive).HasColumnName("is_active");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(e => new { e.RequestId, e.MemberId }).IsUnique();
        b.HasIndex(e => new { e.TenantId, e.RequestId, e.Status });
        b.HasIndex(e => new { e.TenantId, e.MemberId, e.Status });
        b.HasOne(e => e.Request).WithMany(e => e.BeneficiaryCandidates)
            .HasForeignKey(e => e.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Member).WithMany().HasForeignKey(e => e.MemberId).OnDelete(DeleteBehavior.Restrict);
    }
}
