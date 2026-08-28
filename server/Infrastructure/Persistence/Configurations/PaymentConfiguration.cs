using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id");
        b.Property(e => e.TenantId).HasColumnName("tenant_id");
        b.Property(e => e.MemberId).HasColumnName("member_id");
        b.Property(e => e.ChargeId).HasColumnName("charge_id");
        b.Property(e => e.ReceiptNumber).HasColumnName("receipt_number").HasMaxLength(50).IsRequired();
        b.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)");
        b.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(3);
        b.Property(e => e.PaymentMethodId).HasColumnName("payment_method_id");
        b.Property(e => e.PaymentGateway).HasColumnName("payment_gateway").HasMaxLength(100);
        b.Property(e => e.GatewayTransactionId).HasColumnName("gateway_transaction_id").HasMaxLength(200);
        b.Property(e => e.GatewayReference).HasColumnName("gateway_reference").HasMaxLength(200);
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
        b.Property(e => e.ConfirmedAt).HasColumnName("confirmed_at");
        b.Property(e => e.ConfirmedBy).HasColumnName("confirmed_by");
        b.Property(e => e.ReversedAt).HasColumnName("reversed_at");
        b.Property(e => e.ReversedBy).HasColumnName("reversed_by");
        b.Property(e => e.ReversalReason).HasColumnName("reversal_reason").HasMaxLength(500);
        b.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(1000);
        b.Property(e => e.PaymentDate).HasColumnName("payment_date");
        b.Property(e => e.IsActive).HasColumnName("is_active");
        b.Property(e => e.CreatedAt).HasColumnName("created_at");
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        b.HasOne(e => e.Member).WithMany(m => m.Payments).HasForeignKey(e => e.MemberId);
        b.HasOne(e => e.Charge).WithMany(c => c.Payments).HasForeignKey(e => e.ChargeId);
    }
}
