using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace server.Infrastructure.Auth;

public class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var claim = context.User.FindFirst("permissions");
        if (claim is null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        string[]? permissions = null;
        try { permissions = JsonSerializer.Deserialize<string[]>(claim.Value); }
        catch { /* malformed claim → deny */ }

        if (permissions is not null && permissions.Contains(requirement.Permission))
            context.Succeed(requirement);
        else
            context.Fail();

        return Task.CompletedTask;
    }
}
