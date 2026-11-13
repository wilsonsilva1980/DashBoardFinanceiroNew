using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Identity;
using Financeiro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, FinanceiroDbContext db, FinancialScope scope, IConfiguration configuration) : ControllerBase
{
    public sealed record RegisterInput([Required, EmailAddress] string Email, [Required] string Password, [Required, MaxLength(120)] string DisplayName);
    public sealed record LoginInput([Required, EmailAddress] string Email, [Required] string Password, string? TwoFactorCode);

    [AllowAnonymous]
    [HttpGet("csrf")]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterInput input)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = input.Email.Trim(), Email = input.Email.Trim(), DisplayName = input.DisplayName.Trim(), LockoutEnabled = true };
        var result = await users.CreateAsync(user, input.Password);
        if (!result.Succeeded)
            return ValidationProblem(new ValidationProblemDetails(result.Errors.GroupBy(x => x.Code).ToDictionary(g => g.Key, g => g.Select(x => x.Description).ToArray())));
        return StatusCode(StatusCodes.Status201Created, new { user.Id });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        var user = await users.FindByEmailAsync(input.Email.Trim());
        if (user is null || !user.Active) return Unauthorized();
        if (!await signIn.CanSignInAsync(user) || await users.IsLockedOutAsync(user)) return Unauthorized();
        if (!await users.CheckPasswordAsync(user, input.Password))
        {
            await users.AccessFailedAsync(user);
            return Unauthorized();
        }
        if (configuration.GetValue<bool>("Identity:RequireMfa") && !user.TwoFactorEnabled)
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "MFA enrollment required.");
        if (user.TwoFactorEnabled && (string.IsNullOrWhiteSpace(input.TwoFactorCode) ||
            !await users.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, input.TwoFactorCode)))
        {
            await users.AccessFailedAsync(user);
            return Unauthorized();
        }
        await users.ResetAccessFailedCountAsync(user);
        var now = DateTimeOffset.UtcNow;
        var session = new UserSession { UserId = user.Id, CreatedAt = now, LastAccessAt = now, ExpiresAt = now.AddMinutes(30), AbsoluteExpiresAt = now.AddHours(8), SecurityVersion = user.SecurityStamp! };
        db.UserSessions.Add(session);
        user.LastLoginAt = now;
        await db.SaveChangesAsync(HttpContext.RequestAborted);
        var principal = await signIn.CreateUserPrincipalAsync(user);
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim("sid", session.Id.ToString()));
        await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal, new AuthenticationProperties { IssuedUtc = now, ExpiresUtc = session.ExpiresAt, IsPersistent = false, AllowRefresh = true });
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionId = Guid.Parse(User.FindFirstValue("sid")!);
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var session = await db.UserSessions.SingleAsync(x => x.Id == sessionId && x.UserId == userId, HttpContext.RequestAborted);
        session.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(HttpContext.RequestAborted);
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        Response.Headers.CacheControl = "no-store";
        var user = await users.GetUserAsync(User);
        return Ok(new { user!.Id, user.DisplayName, user.Email, scope.TenantId, scope.SpaceId });
    }
}
