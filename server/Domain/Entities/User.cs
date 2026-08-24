namespace server.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "member";
    public DateTime? EmailVerifiedAt { get; set; }
    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }
    public string Status { get; set; } = "active";
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public Tenant? Tenant { get; set; }
    public ICollection<Member> Members { get; set; } = [];
}
