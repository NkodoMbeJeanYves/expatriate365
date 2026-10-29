using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class TransferAssistanceEventConfiguration : IEntityTypeConfiguration<TransferAssistanceEvent>
{
    public void Configure(EntityTypeBuilder<TransferAssistanceEvent> b)
    {
        b.ToTable("transfer_assistance_events");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.RequestId).HasColumnName("request_id");
        b.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
        b.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        b.Property(e => e.Details).HasColumnName("details").HasMaxLength(1000);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");

        b.HasIndex(e => new { e.TenantId, e.RequestId, e.CreatedAt });
        b.HasOne(e => e.Request).WithMany().HasForeignKey(e => e.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(e => e.ActorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
