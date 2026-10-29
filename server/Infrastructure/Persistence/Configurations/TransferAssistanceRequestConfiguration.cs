using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class TransferAssistanceRequestConfiguration : IEntityTypeConfiguration<TransferAssistanceRequest>
{
    public void Configure(EntityTypeBuilder<TransferAssistanceRequest> b)
    {
        b.ToTable("transfer_assistance_requests");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.RequesterMemberId).HasColumnName("requester_member_id");
        b.Property(e => e.OpenRequestKey).HasColumnName("open_request_key").HasMaxLength(65);
        b.Property(e => e.BeneficiaryMemberId).HasColumnName("beneficiary_member_id").IsRequired(false);
        b.Property(e => e.BeneficiaryName).HasColumnName("beneficiary_name").HasMaxLength(200).IsRequired();
        b.Property(e => e.DestinationCountry).HasColumnName("destination_country").HasMaxLength(2).IsRequired();
        b.Property(e => e.AmountMur).HasColumnName("amount_mur").HasColumnType("decimal(18,2)");
        b.Property(e => e.ReceiveCurrency).HasColumnName("receive_currency").HasMaxLength(3).IsRequired();
        b.Property(e => e.ReceiveMethod).HasColumnName("receive_method").HasMaxLength(30).IsRequired();
        b.Property(e => e.Purpose).HasColumnName("purpose").HasMaxLength(500).IsRequired();
        b.Property(e => e.BeneficiaryRelationship).HasColumnName("beneficiary_relationship").HasMaxLength(100).IsRequired();
        b.Property(e => e.AvailableFrom).HasColumnName("available_from");
        b.Property(e => e.AvailableTo).HasColumnName("available_to");
        b.Property(e => e.MeetingArea).HasColumnName("meeting_area").HasMaxLength(200).IsRequired();
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        b.Property(e => e.HelperMemberId).HasColumnName("helper_member_id");
        b.Property(e => e.WitnessMemberId).HasColumnName("witness_member_id");
        b.Property(e => e.MeetingAt).HasColumnName("meeting_at");
        b.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        b.Property(e => e.RequesterHandoverConfirmed).HasColumnName("requester_handover_confirmed");
        b.Property(e => e.HelperHandoverConfirmed).HasColumnName("helper_handover_confirmed");
        b.Property(e => e.WitnessAttested).HasColumnName("witness_attested");
        b.Property(e => e.HandoverConfirmedAt).HasColumnName("handover_confirmed_at");
        b.Property(e => e.TransferProvider).HasColumnName("transfer_provider").HasMaxLength(100);
        b.Property(e => e.TransferEvidenceReference).HasColumnName("transfer_evidence_reference").HasMaxLength(120);
        b.Property(e => e.TransferSentAt).HasColumnName("transfer_sent_at");
        b.Property(e => e.BeneficiaryConfirmation).HasColumnName("beneficiary_confirmation").HasMaxLength(30);
        b.Property(e => e.BeneficiaryConfirmedAt).HasColumnName("beneficiary_confirmed_at");
        b.Property(e => e.DisputeReason).HasColumnName("dispute_reason").HasMaxLength(1000);
        b.Property(e => e.DisputeOpenedBy).HasColumnName("dispute_opened_by");
        b.Property(e => e.DisputeOpenedAt).HasColumnName("dispute_opened_at");
        b.Property(e => e.DisputeResolution).HasColumnName("dispute_resolution").HasMaxLength(30);
        b.Property(e => e.DisputeResolvedBy).HasColumnName("dispute_resolved_by");
        b.Property(e => e.DisputeResolvedAt).HasColumnName("dispute_resolved_at");
        b.Property(e => e.SuspendedFromStatus).HasColumnName("suspended_from_status").HasMaxLength(30);
        b.Property(e => e.WorkflowVersion).HasColumnName("workflow_version").IsConcurrencyToken();
        b.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
        b.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
        b.Property(e => e.ReviewReason).HasColumnName("review_reason").HasMaxLength(1000);
        b.Property(e => e.IsActive).HasColumnName("is_active");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(e => new { e.TenantId, e.Status, e.CreatedAt });
        b.HasIndex(e => e.OpenRequestKey).IsUnique();
        b.HasIndex(e => new { e.TenantId, e.RequesterMemberId, e.CreatedAt });
        b.HasIndex(e => new { e.TenantId, e.BeneficiaryMemberId });
        b.HasIndex(e => new { e.TenantId, e.HelperMemberId, e.Status });
        b.HasIndex(e => new { e.TenantId, e.WitnessMemberId, e.Status });
        b.HasIndex(e => new { e.TenantId, e.Status, e.ExpiresAt });
        b.HasOne(e => e.RequesterMember).WithMany().HasForeignKey(e => e.RequesterMemberId);
        b.HasOne(e => e.BeneficiaryMember).WithMany().HasForeignKey(e => e.BeneficiaryMemberId);
        b.HasOne(e => e.HelperMember).WithMany().HasForeignKey(e => e.HelperMemberId);
        b.HasOne(e => e.WitnessMember).WithMany().HasForeignKey(e => e.WitnessMemberId);
    }
}
