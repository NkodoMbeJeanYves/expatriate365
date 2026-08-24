using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using server.Domain.Entities;

namespace server.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasKey(e => e.Id);
        b.Property(e => e.Email).HasMaxLength(255).IsRequired();
        b.HasIndex(e => e.Email).IsUnique();
        b.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
        b.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        b.Property(e => e.LastName).HasMaxLength(100).IsRequired();
        b.Property(e => e.Phone).HasMaxLength(20);
        b.Property(e => e.Role).HasMaxLength(50).IsRequired();
        b.Property(e => e.Status).HasMaxLength(20).IsRequired();
        b.Property(e => e.MfaSecret).HasMaxLength(64);
        b.Property(e => e.RefreshTokenHash).HasMaxLength(128);
        b.Ignore(e => e.FullName);
    }
}
