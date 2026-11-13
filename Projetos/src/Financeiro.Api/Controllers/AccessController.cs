using Financeiro.Infrastructure.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/access")]
public sealed class AccessController(AccessManagementService access) : ControllerBase
{
    public sealed record ChangeInput(long Version);

    [Authorize(Policy = "usuarios.gerenciar")]
    [HttpPost("space-members/{userId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateSpaceMember(Guid userId, ChangeInput input)
    {
        await access.DeactivateSpaceMemberAsync(userId, input.Version, HttpContext.RequestAborted);
        return NoContent();
    }

    [Authorize(Policy = "FinancialScope")]
    [HttpPost("tenant-members/{userId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateTenantMember(Guid userId, ChangeInput input)
    {
        await access.DeactivateTenantMemberAsync(userId, input.Version, HttpContext.RequestAborted);
        return NoContent();
    }
}
