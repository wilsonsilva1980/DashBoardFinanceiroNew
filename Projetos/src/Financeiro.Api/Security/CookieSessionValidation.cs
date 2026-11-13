using System.Security.Claims;
using Financeiro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.Security;

public static class CookieSessionValidation
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<FinanceiroDbContext>();
        var now = DateTimeOffset.UtcNow;
        if (!Guid.TryParse(context.Principal?.FindFirstValue("sid"), out var sessionId) ||
            !Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            await RejectAsync();
            return;
        }
        var session = await db.UserSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId && x.RevokedAt == null && x.ExpiresAt > now && x.AbsoluteExpiresAt > now, context.HttpContext.RequestAborted);
        var user = await db.Users.AsNoTracking().Where(x => x.Id == userId)
            .Select(x => new { x.Active, x.SecurityStamp }).SingleOrDefaultAsync(context.HttpContext.RequestAborted);
        if (session is null || user is null || !user.Active || !string.Equals(user.SecurityStamp, session.SecurityVersion, StringComparison.Ordinal))
        {
            await RejectAsync();
            return;
        }
        if (now - session.LastAccessAt > TimeSpan.FromMinutes(5))
        {
            session.LastAccessAt = now;
            session.ExpiresAt = new[] { now.AddMinutes(30), session.AbsoluteExpiresAt }.Min();
            await db.SaveChangesAsync(context.HttpContext.RequestAborted);
            context.Properties.IssuedUtc = now;
            context.Properties.ExpiresUtc = session.ExpiresAt;
            context.ShouldRenew = true;
        }
        async Task RejectAsync()
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(context.Scheme.Name);
        }
    }
}
