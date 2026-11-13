using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;

namespace Financeiro.Api.IntegrationTests;

[Trait("Category", "Database")]
public class IdentityPolicyTests
{
    [Fact]
    public async Task PasswordRulesAndLockoutAreEnforcedWithoutIssuingSessions()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RefreshCsrfAsync(browser);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/api/v1/auth/register", new { email = "weak@example.test", password = "weak", displayName = "Weak" })).StatusCode);
        (await browser.PostAsJsonAsync("/api/v1/auth/register", new { email = "strong@example.test", password = "TestPassword123!", displayName = "Strong" })).EnsureSuccessStatusCode();
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await browser.PostAsJsonAsync("/api/v1/auth/login", new { email = "strong@example.test", password = "WrongPassword123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.PostAsJsonAsync("/api/v1/auth/login", new { email = "strong@example.test", password = "TestPassword123!" })).StatusCode);
        await using var db = api.OpenMaintenance();
        Assert.True((await db.Users.SingleAsync()).LockoutEnd > DateTimeOffset.UtcNow);
        Assert.Equal(0, await db.UserSessions.CountAsync());
    }

    [Fact]
    public async Task MfaPolicyAndEnabledMfaCannotBeBypassedWithPasswordAlone()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RefreshCsrfAsync(browser);
        (await browser.PostAsJsonAsync("/api/v1/auth/register", new { email = "mfa@example.test", password = "TestPassword123!", displayName = "MFA" })).EnsureSuccessStatusCode();
        api.Services.GetRequiredService<IConfiguration>()["Identity:RequireMfa"] = "true";
        Assert.Equal(HttpStatusCode.Forbidden, (await browser.PostAsJsonAsync("/api/v1/auth/login", new { email = "mfa@example.test", password = "TestPassword123!" })).StatusCode);
        api.Services.GetRequiredService<IConfiguration>()["Identity:RequireMfa"] = "false";
        await using (var db = api.OpenMaintenance())
        {
            (await db.Users.SingleAsync()).TwoFactorEnabled = true;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.PostAsJsonAsync("/api/v1/auth/login", new { email = "mfa@example.test", password = "TestPassword123!" })).StatusCode);
        await using var verify = api.OpenMaintenance();
        Assert.Equal(0, await verify.UserSessions.CountAsync());
    }
    [Fact]
    public async Task ConfiguredEmailConfirmationPolicyIsHonored()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RefreshCsrfAsync(browser);
        (await browser.PostAsJsonAsync("/api/v1/auth/register", new { email = "confirm@example.test", password = "TestPassword123!", displayName = "Confirm" })).EnsureSuccessStatusCode();
        api.Services.GetRequiredService<IOptions<IdentityOptions>>().Value.SignIn.RequireConfirmedEmail = true;
        var credentials = new { email = "confirm@example.test", password = "TestPassword123!" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.PostAsJsonAsync("/api/v1/auth/login", credentials)).StatusCode);
        await using (var db = api.OpenMaintenance())
        {
            Assert.Equal(0, await db.UserSessions.CountAsync());
            (await db.Users.SingleAsync()).EmailConfirmed = true;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.NoContent, (await browser.PostAsJsonAsync("/api/v1/auth/login", credentials)).StatusCode);
    }

}
