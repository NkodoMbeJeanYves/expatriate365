namespace server.Application.Members.DTOs;

public record MemberDto(
    string Id,
    string TenantId,
    string UserId,
    string MembershipNumber,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Status,
    string? CategoryId,
    string? CategoryName,
    string JoinedDate,
    string? ExpiryDate,
    string? PhotoUrl,
    string? Address,
    string? Profession,
    string? DateOfBirth,
    string? Gender,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    bool IsActive,
    string CreatedAt,
    string? UpdatedAt,
    string? EmailVerifiedAt,
    string Role
);

public record MemberListItemDto(
    string Id,
    string MembershipNumber,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string Status,
    string? CategoryId,
    string? CategoryName,
    string JoinedDate,
    string? PhotoUrl,
    bool IsActive
);

public record CreateMemberRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string? CategoryId,
    string JoinedDate,
    string? ExpiryDate,
    string? PhotoUrl,
    string? Address,
    string? Profession,
    string? DateOfBirth,
    string? Gender,
    string? EmergencyContactName,
    string? EmergencyContactPhone
);

public record UpdateMemberRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? CategoryId,
    string? ExpiryDate,
    string? PhotoUrl,
    string? Address,
    string? Profession,
    string? DateOfBirth,
    string? Gender,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    bool IsActive
);

public record PatchMemberStatusRequest(string Status);

public record MembershipCategoryDto(
    string Id,
    string Name,
    string? Description,
    decimal ContributionRate,
    bool VotingRights,
    bool WelfareEligible,
    bool IsActive
);

public record CreateCategoryRequest(
    string Name,
    string? Description,
    decimal ContributionRate,
    bool VotingRights = true,
    bool WelfareEligible = true
);
