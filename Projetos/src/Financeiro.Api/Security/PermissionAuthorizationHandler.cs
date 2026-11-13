using Financeiro.Infrastructure.Access;
using Microsoft.AspNetCore.Authorization;

namespace Financeiro.Api.Security;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler(PermissionEvaluator permissions) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true && await permissions.HasAsync(requirement.Permission))
            context.Succeed(requirement);
    }
}
