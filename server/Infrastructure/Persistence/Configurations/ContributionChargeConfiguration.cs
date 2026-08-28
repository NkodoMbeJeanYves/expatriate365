using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class ContributionChargeConfiguration : IEntityTypeConfiguration<ContributionCharge>
{
    public void Configure(EntityTypeBuilder<ContributionCharge> b)
    {
        b.ToTable("contribution_charges");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.MemberId).HasColumnName("member_id");
        b.Property(e => e.ContributionTypeId).HasColumnName("contribution_type_id");
        b.Property(e => e.DueDate).HasColumnName("due_date");
        b.Property(e => e.BaseAmount).HasColumnName("base_amount").HasColumnType("decimal(18,2)");
        b.Property(e => e.PenaltyAmount).HasColumnName("penalty_amount").HasColumnType("decimal(18,2)");
        b.Property(e => e.WaiverAmount).HasColumnName("waiver_amount").HasColumnType("decimal(18,2)");
        b.Property(e => e.AmountPaid).HasColumnName("amount_paid").HasColumnType("decimal(18,2)");
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        b.Property(e => e.IsActive).HasColumnName("is_active");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.Ignore(e => e.TotalDue);
        b.Ignore(e => e.Balance);

        // Keep the single-column index so MySQL can use it for the FK constraint.
        // MySQL requires an index whose leftmost column matches the FK column.
        // The composite unique index alone is not enough to satisfy MySQL's FK check
        // when the old index is dropped during migration.
        b.HasIndex(e => e.MemberId).HasDatabaseName("IX_contribution_charges_member_id");
        b.HasIndex(e => new { e.MemberId, e.ContributionTypeId, e.DueDate })
            .IsUnique()
            .HasDatabaseName("IX_contribution_charges_member_type_date");

        b.HasOne(e => e.Member).WithMany(m => m.Charges).HasForeignKey(e => e.MemberId);
        b.HasOne(e => e.ContributionType).WithMany(t => t.Charges).HasForeignKey(e => e.ContributionTypeId);
    }
}
