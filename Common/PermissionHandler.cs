using Microsoft.AspNetCore.Authorization;

namespace PropertyPortal.Common;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim("perm", requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
