using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Financeiro.Application.Access;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/context")]
public sealed class ContextController(FinanceiroDbContext db, ScopeResolver resolver, IOnboardingService onboarding) : ControllerBase
{
    public sealed record SelectInput(Guid TenantId, Guid SpaceId);

    [HttpPost("onboarding")]
    public async Task<ActionResult<OnboardingResult>> Onboard(OnboardingRequest request)
    {
        var result = await onboarding.OnboardAsync(request, HttpContext.RequestAborted);
        await resolver.SelectAsync(SessionId, UserId, result.TenantId, result.SpaceId, HttpContext.RequestAborted);
        return Ok(result);
    }

    [HttpGet("spaces")]
    public async Task<IActionResult> Spaces()
    {
        Response.Headers.CacheControl = "no-store";
        var userId = UserId;
        var query = from member in db.SpaceMembers.IgnoreQueryFilters().AsNoTracking()
                    join space in db.FinancialSpaces.AsNoTracking() on new { member.TenantId, Id = member.SpaceId } equals new { space.TenantId, space.Id }
                    join tenantMember in db.TenantMembers.AsNoTracking() on new { member.TenantId, member.UserId } equals new { tenantMember.TenantId, tenantMember.UserId }
                    join tenant in db.Tenants.AsNoTracking() on member.TenantId equals tenant.Id
                    where member.UserId == userId && member.Active && space.Active && tenantMember.Active && tenant.Status == "Active"
                    select new { space.TenantId, SpaceId = space.Id, space.Name, space.Currency };
        return Ok(await query.OrderBy(x => x.Name).ToListAsync(HttpContext.RequestAborted));
    }

    [Authorize(Policy = "FinancialScope")]
    [HttpGet("permissions")]
    public async Task<IActionResult> Permissions([FromServices] PermissionEvaluator permissions)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await permissions.EffectiveAsync(HttpContext.RequestAborted));
    }

    [HttpPost("select")]
    public async Task<IActionResult> Select(SelectInput request)
    {
        await resolver.SelectAsync(SessionId, UserId, request.TenantId, request.SpaceId, HttpContext.RequestAborted);
        return NoContent();
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Guid SessionId => Guid.Parse(User.FindFirstValue("sid")!);
}
