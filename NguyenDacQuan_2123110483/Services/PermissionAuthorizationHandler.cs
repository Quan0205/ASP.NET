using Microsoft.AspNetCore.Authorization;

namespace CoffeeHRM.Services;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.HasClaim(
                claim => string.Equals(claim.Type, "permission", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(claim.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
