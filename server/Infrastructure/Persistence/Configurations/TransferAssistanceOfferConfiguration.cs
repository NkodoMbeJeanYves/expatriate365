using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class TransferAssistanceOfferConfiguration : IEntityTypeConfiguration<TransferAssistanceOffer>
{
    public void Configure(EntityTypeBuilder<TransferAssistanceOffer> b)
    {
        b.ToTable("transfer_assistance_offers");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.RequestId).HasColumnName("request_id");
        b.Property(e => e.HelperMemberId).HasColumnName("helper_member_id");
        b.Property(e => e.Availability).HasColumnName("availability").HasMaxLength(200).IsRequired();
        b.Property(e => e.Message).HasColumnName("message").HasMaxLength(500).IsRequired();
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        b.Property(e => e.CreatedAt).HasColumnName("created_at");

        b.HasIndex(e => new { e.RequestId, e.HelperMemberId }).IsUnique();
        b.HasIndex(e => new { e.TenantId, e.RequestId, e.Status });
        b.HasOne(e => e.Request).WithMany(e => e.Offers).HasForeignKey(e => e.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.HelperMember).WithMany().HasForeignKey(e => e.HelperMemberId).OnDelete(DeleteBehavior.Restrict);
    }
}
