namespace server.Application.Admin.DTOs;

public record AdminUserDto(
    string Id, string Email, string FullName, string? Phone,
    string Role, string Status,
    bool IsActive, string? LastLoginAt, string CreatedAt, string? UpdatedAt);

public record AdminStatsDto(
    int TotalUsers, int ActiveUsers, int InactiveUsers);

public record InviteUserRequest(
    string Email, string FirstName, string LastName, string? Phone, string Role);

public record ChangeRoleRequest(string Role);

public record ChangeStatusRequest(string Status);
