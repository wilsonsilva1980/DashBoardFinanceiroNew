using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.IntegrationTests;

[Trait("Category", "Database")]
public class AuthenticationTests
{
    [Fact]
    public async Task BrowserMutationsRequireCsrfAndLogoutRevokesCopiedCookies()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/v1/auth/me")).StatusCode);
        var register = new { email = "owner@example.test", password = "TestPassword123!", displayName = "Owner" };
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/api/v1/auth/register", register)).StatusCode);
        await using (var db = api.OpenMaintenance()) Assert.Equal(0, await db.Users.CountAsync());
        var cookie = await TestApi.RegisterAndLoginAsync(browser, register.email);
        browser.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        var onboarding = new { tenantName = "Family", spaceName = "Personal", currency = "BRL", requestKey = "onboard-once" };
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/api/v1/context/onboarding", onboarding)).StatusCode);
        await using (var db = api.OpenMaintenance()) Assert.Equal(0, await db.Tenants.CountAsync());
        browser.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid");
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/api/v1/auth/logout", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/api/v1/auth/me")).StatusCode);
        await TestApi.RefreshCsrfAsync(browser);
        var first = await browser.PostAsJsonAsync("/api/v1/context/onboarding", onboarding);
        first.EnsureSuccessStatusCode();
        var context = await first.Content.ReadFromJsonAsync<JsonElement>();
        var retry = await browser.PostAsJsonAsync("/api/v1/context/onboarding", onboarding);
        retry.EnsureSuccessStatusCode();
        Assert.Equal(context.GetProperty("tenantId").GetGuid(), (await retry.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tenantId").GetGuid());
        await using (var db = api.OpenMaintenance())
        {
            Assert.Equal(1, await db.Tenants.CountAsync());
            Assert.Equal(4, await db.SpaceRoles.CountAsync());
        }
        var foreign = await browser.PostAsJsonAsync("/api/v1/context/select", new { tenantId = Guid.NewGuid(), spaceId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
        Assert.Equal("application/problem+json", foreign.Content.Headers.ContentType?.MediaType);
        var logout = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var replay = api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = false, AllowAutoRedirect = false });
        replay.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/v1/auth/me")).StatusCode);
    }

    [Fact]
    public async Task ExpiredSessionsAndChangedSecurityStampsAreRejected()
    {
        await using var api = await TestApi.CreateAsync();
        using var expiredBrowser = api.Browser();
        var csrfCheck = await expiredBrowser.GetAsync("/api/v1/auth/csrf");
        Assert.True(csrfCheck.IsSuccessStatusCode, api.LastException?.ToString());
        await TestApi.RegisterAndLoginAsync(expiredBrowser, "expired@example.test");
        await using (var db = api.OpenMaintenance())
        {
            var session = await db.UserSessions.SingleAsync();
            session.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await expiredBrowser.GetAsync("/api/v1/auth/me")).StatusCode);
        using var changedBrowser = api.Browser();
        await TestApi.RegisterAndLoginAsync(changedBrowser, "stamp@example.test");
        await using (var db = api.OpenMaintenance())
        {
            var user = await db.Users.SingleAsync(x => x.Email == "stamp@example.test");
            var session = await db.UserSessions.SingleAsync(x => x.UserId == user.Id);
            session.SecurityVersion = "CaseSensitiveStamp";
            user.SecurityStamp = "casesensitivestamp";
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await changedBrowser.GetAsync("/api/v1/auth/me")).StatusCode);
    }
}
