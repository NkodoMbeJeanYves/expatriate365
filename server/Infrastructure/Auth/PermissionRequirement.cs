using Microsoft.AspNetCore.Authorization;

namespace server.Infrastructure.Auth;

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
