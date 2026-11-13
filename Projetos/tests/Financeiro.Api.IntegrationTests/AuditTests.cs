using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.IntegrationTests;

[Trait("Category", "Database")]
public class AuditTests
{
    [Fact]
    public async Task UserWithoutAuditPermissionGetsForbiddenWithoutAuditData()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "audit-no-permission@example.test");

        Assert.Equal(HttpStatusCode.Forbidden, (await browser.GetAsync("/api/v1/auditoria")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await browser.GetAsync("/api/v1/auditoria/eventos")).StatusCode);
    }

    [Fact]
    public async Task AuthorizedUserCanFilterAuditAndInvalidQueriesAreRejected()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "audit-owner@example.test");
        (await browser.PostAsJsonAsync("/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "audit-onboarding" })).EnsureSuccessStatusCode();

        var response = await browser.GetFromJsonAsync<JsonElement>("/api/v1/auditoria?action=onboarding.confirmed&page=1&pageSize=10");
        Assert.Equal(1, response.GetProperty("total").GetInt32());
        var item = response.GetProperty("items")[0];
        Assert.Equal("onboarding.confirmed", item.GetProperty("action").GetString());
        Assert.Equal("Onboarding confirmado", item.GetProperty("actionLabel").GetString());
        Assert.Equal("Tenant", item.GetProperty("entity").GetString());
        Assert.True(item.GetProperty("changes").ValueKind == JsonValueKind.Object);

        var events = await browser.GetFromJsonAsync<Dictionary<string, string>>("/api/v1/auditoria/eventos");
        Assert.Equal("Pagamento confirmado", events!["pagamento.confirmado"]);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.GetAsync("/api/v1/auditoria?action=unknown.event&pageSize=101")).StatusCode);
    }

    [Fact]
    public async Task AuditListingIsPaginatedAndNeverReturnsAnotherSpace()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "audit-scope@example.test");
        var context = await (await browser.PostAsJsonAsync("/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "audit-scope" })).Content.ReadFromJsonAsync<JsonElement>();

        await using (var db = api.OpenMaintenance())
        {
            var tenantId = context.GetProperty("tenantId").GetGuid();
            var spaceId = context.GetProperty("spaceId").GetGuid();
            var userId = (await db.Users.SingleAsync(x => x.Email == "audit-scope@example.test")).Id;
            var otherSpace = new FinancialSpace { TenantId = tenantId, Name = "Other", NormalizedName = "OTHER" };
            db.Add(otherSpace);
            db.AddRange(
                Enumerable.Range(1, 3).Select(index => new AuditRecord
                {
                    TenantId = tenantId,
                    SpaceId = spaceId,
                    UserId = userId,
                    Action = "onboarding.confirmed",
                    Entity = "Tenant",
                    EntityId = tenantId,
                    Instant = DateTimeOffset.UtcNow.AddMinutes(index),
                    CorrelationId = $"audit-page-{index}",
                    ChangesJson = "{}"
                }));
            db.Add(new AuditRecord
            {
                TenantId = tenantId,
                SpaceId = otherSpace.Id,
                UserId = userId,
                Action = "onboarding.confirmed",
                Entity = "Tenant",
                EntityId = tenantId,
                Instant = DateTimeOffset.UtcNow.AddMinutes(10),
                CorrelationId = "audit-foreign-space",
                ChangesJson = "{}"
            });
            await db.SaveChangesAsync();
        }

        var page = await browser.GetFromJsonAsync<JsonElement>("/api/v1/auditoria?action=onboarding.confirmed&page=1&pageSize=2");
        Assert.Equal(4, page.GetProperty("total").GetInt32());
        Assert.Equal(2, page.GetProperty("items").GetArrayLength());
        Assert.DoesNotContain("audit-foreign-space", page.GetProperty("items").GetRawText(), StringComparison.Ordinal);
    }
}
