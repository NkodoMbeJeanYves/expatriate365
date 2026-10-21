using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class TransferAssistanceWitnessInvitationConfiguration : IEntityTypeConfiguration<TransferAssistanceWitnessInvitation>
{
    public void Configure(EntityTypeBuilder<TransferAssistanceWitnessInvitation> b)
    {
        b.ToTable("transfer_assistance_witness_invitations");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.RequestId).HasColumnName("request_id");
        b.Property(e => e.WitnessMemberId).HasColumnName("witness_member_id");
        b.Property(e => e.InvitedByUserId).HasColumnName("invited_by_user_id");
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        b.Property(e => e.ConflictDeclared).HasColumnName("conflict_declared");
        b.Property(e => e.ConflictReason).HasColumnName("conflict_reason").HasMaxLength(500);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        b.Property(e => e.RespondedAt).HasColumnName("responded_at");

        b.HasIndex(e => new { e.TenantId, e.RequestId, e.Status });
        b.HasIndex(e => new { e.TenantId, e.WitnessMemberId, e.Status });
        b.HasOne(e => e.Request).WithMany(e => e.WitnessInvitations).HasForeignKey(e => e.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.WitnessMember).WithMany().HasForeignKey(e => e.WitnessMemberId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(e => e.InvitedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
