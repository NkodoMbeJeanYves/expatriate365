using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class EmailOutboxMessageConfiguration : IEntityTypeConfiguration<EmailOutboxMessage>
{
    public void Configure(EntityTypeBuilder<EmailOutboxMessage> b)
    {
        b.ToTable("email_outbox");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.UserId).HasColumnName("user_id");
        b.Property(e => e.ToEmail).HasColumnName("to_email").HasMaxLength(256).IsRequired();
        b.Property(e => e.ToName).HasColumnName("to_name").HasMaxLength(200).IsRequired();
        b.Property(e => e.Language).HasColumnName("language").HasMaxLength(10).IsRequired();
        b.Property(e => e.Template).HasColumnName("template").HasMaxLength(100).IsRequired();
        b.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
        b.Property(e => e.DedupKey).HasColumnName("dedup_key").HasMaxLength(200).IsRequired();
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        b.Property(e => e.Attempts).HasColumnName("attempts");
        b.Property(e => e.NextAttemptAt).HasColumnName("next_attempt_at");
        b.Property(e => e.LastError).HasColumnName("last_error").HasMaxLength(500);
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.SentAt).HasColumnName("sent_at");

        b.HasIndex(e => e.DedupKey);
        b.HasIndex(e => new { e.Status, e.NextAttemptAt });
    }
}
