using System.Security.Claims;
using AppRoles = server.Application.Common.Roles;

namespace server.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool IsSuperAdmin(this ClaimsPrincipal principal) =>
        (principal.FindFirstValue("role")
         ?? principal.FindFirstValue(ClaimTypes.Role)) == AppRoles.SuperAdmin;
}
