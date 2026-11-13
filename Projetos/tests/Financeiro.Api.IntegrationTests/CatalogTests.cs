using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.IntegrationTests;

[Trait("Category", "Database")]
public class CatalogTests
{
    private static async Task<JsonElement> PostAsync(HttpClient browser, string path, object input)
    {
        var response = await browser.PostAsJsonAsync(path, input);
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
    private static Task<JsonElement> OnboardAsync(HttpClient browser, string key) => PostAsync(browser, "/api/v1/context/onboarding", new { tenantName = "Family " + key, spaceName = "Main", currency = "BRL", requestKey = key });

    [Theory]
    [InlineData("pessoas")]
    [InlineData("categorias")]
    [InlineData("instituicoes")]
    [InlineData("bandeiras")]
    public async Task NamedCatalogsSupportVersionedCrudAndDeactivation(string path)
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        await OnboardAsync(browser, "initial");
        var route = "/api/v1/" + path;
        var created = await PostAsync(browser, route, new { name = "Original", code = "00012" });
        var id = created.GetProperty("id").GetGuid();
        var change = await browser.PutAsJsonAsync(route + "/" + id, new { name = "Updated", code = "00012", active = false, version = 1 });
        change.EnsureSuccessStatusCode();
        var updated = await change.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, updated.GetProperty("version").GetInt64());
        Assert.False(updated.GetProperty("active").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await browser.PutAsJsonAsync(route + "/" + id, new { name = "Stale", version = 1 })).StatusCode);
        var page = await browser.GetFromJsonAsync<JsonElement>(route + "?pageSize=1");
        Assert.Equal(1, page.GetProperty("total").GetInt32());
        Assert.Single(page.GetProperty("items").EnumerateArray());
        var active = await browser.GetFromJsonAsync<JsonElement>(route + "?includeInactive=false");
        Assert.Equal(0, active.GetProperty("total").GetInt32());
        if (path == "instituicoes") Assert.Equal("00012", updated.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await browser.DeleteAsync(route + "/" + id + "?version=2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync(route + "/" + id)).StatusCode);
    }

    [Fact]
    public async Task CardBrandsAreIndependentAcrossSpacesAndForeignReferencesAreRejected()
    {
        await using var api = await TestApi.CreateAsync();
        using var first = api.Browser();
        using var second = api.Browser();
        await TestApi.RegisterAndLoginAsync(first, "first@example.test");
        await TestApi.RegisterAndLoginAsync(second, "second@example.test");
        var firstContext = await OnboardAsync(first, "first");
        var secondContext = await OnboardAsync(second, "second");
        var firstBrand = await PostAsync(first, "/api/v1/bandeiras", new { name = "Visa", tenantId = secondContext.GetProperty("tenantId").GetGuid(), spaceId = secondContext.GetProperty("spaceId").GetGuid() });
        var foreignBrand = await PostAsync(second, "/api/v1/bandeiras", new { name = "Visa" });
        var foreignId = foreignBrand.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await first.GetAsync("/api/v1/bandeiras/" + foreignId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await first.PutAsJsonAsync("/api/v1/bandeiras/" + foreignId, new { name = "Stolen", version = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await first.DeleteAsync("/api/v1/bandeiras/" + foreignId + "?version=1")).StatusCode);
        var institution = await PostAsync(first, "/api/v1/instituicoes", new { name = "Local bank" });
        Assert.Equal(HttpStatusCode.BadRequest, (await first.PostAsJsonAsync("/api/v1/cartoes", new { institutionId = institution.GetProperty("id").GetGuid(), brandId = foreignId, name = "Foreign brand" })).StatusCode);
        await using var maintenance = api.OpenMaintenance();
        var stored = await maintenance.CardBrands.SingleAsync(x => x.Id == firstBrand.GetProperty("id").GetGuid());
        Assert.Equal(firstContext.GetProperty("tenantId").GetGuid(), stored.TenantId);
        var extraSpace = new FinancialSpace { TenantId = stored.TenantId, Name = "Second", NormalizedName = "SECOND" };
        var userId = (await maintenance.Users.SingleAsync(x => x.Email == "first@example.test")).Id;
        var role = new SpaceRole { TenantId = stored.TenantId, SpaceId = extraSpace.Id, Name = "Admin", NormalizedName = "ADMIN", Active = true };
        maintenance.AddRange(extraSpace, role, new SpaceMember { TenantId = stored.TenantId, SpaceId = extraSpace.Id, UserId = userId },
            new MemberRole { TenantId = stored.TenantId, SpaceId = extraSpace.Id, UserId = userId, RoleId = role.Id });
        foreach (var permission in await maintenance.Permissions.ToArrayAsync())
            maintenance.Add(new RolePermission { TenantId = stored.TenantId, SpaceId = extraSpace.Id, RoleId = role.Id, PermissionId = permission.Id });
        await maintenance.SaveChangesAsync();
        (await first.PostAsJsonAsync("/api/v1/context/select", new { tenantId = stored.TenantId, spaceId = extraSpace.Id })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await first.GetAsync("/api/v1/bandeiras/" + stored.Id)).StatusCode);
        var sameName = await PostAsync(first, "/api/v1/bandeiras", new { name = "Visa" });
        Assert.NotEqual(stored.Id, sameName.GetProperty("id").GetGuid());
        var foreignInstitution = institution.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await first.PostAsJsonAsync("/api/v1/contas", new { institutionId = foreignInstitution, currency = "BRL", overdraftLimit = "0.00" })).StatusCode);
        var changed = await first.PutAsJsonAsync("/api/v1/bandeiras/" + sameName.GetProperty("id").GetGuid(), new { name = "Local edit", active = false, version = 1 });
        changed.EnsureSuccessStatusCode();
        var secondUnchanged = await second.GetFromJsonAsync<JsonElement>("/api/v1/bandeiras/" + foreignId);
        Assert.Equal("Visa", secondUnchanged.GetProperty("name").GetString());
        Assert.True(secondUnchanged.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task LiteralAccountIdentifiersAndReferencedCatalogHistoryArePreserved()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        var scope = await OnboardAsync(browser, "initial");
        var institution = await PostAsync(browser, "/api/v1/instituicoes", new { name = "Bank", code = "000" });
        var person = await PostAsync(browser, "/api/v1/pessoas", new { name = "Owner" });
        var brand = await PostAsync(browser, "/api/v1/bandeiras", new { name = "Visa" });
        var institutionId = institution.GetProperty("id").GetGuid();
        var personId = person.GetProperty("id").GetGuid();
        var brandId = brand.GetProperty("id").GetGuid();
        var accountInput = new { institutionId, holderId = personId, type = "Current", agency = "0001-A", number = "00000123", checkDigit = "X-00", currency = "BRL", overdraftLimit = "12345678901234567.89" };
        var account = await PostAsync(browser, "/api/v1/contas", accountInput);
        var accountId = account.GetProperty("id").GetGuid();
        var read = await browser.GetFromJsonAsync<JsonElement>("/api/v1/contas/" + accountId);
        Assert.Equal(accountInput.agency, read.GetProperty("agency").GetString());
        Assert.Equal(accountInput.number, read.GetProperty("number").GetString());
        Assert.Equal(accountInput.checkDigit, read.GetProperty("checkDigit").GetString());
        Assert.Equal(accountInput.overdraftLimit, read.GetProperty("overdraftLimit").GetString());
        var card = await PostAsync(browser, "/api/v1/cartoes", new { institutionId, brandId, holderId = personId, name = "Card", dueDay = 10, creditLimit = "1000.01" });
        var cardId = card.GetProperty("id").GetGuid();
        Assert.Equal(JsonValueKind.Null, card.GetProperty("availableCredit").ValueKind);
        Assert.Equal(HttpStatusCode.Conflict, (await browser.DeleteAsync("/api/v1/bandeiras/" + brandId + "?version=1")).StatusCode);
        await using (var db = api.OpenMaintenance())
        {
            var tenantId = scope.GetProperty("tenantId").GetGuid();
            var spaceId = scope.GetProperty("spaceId").GetGuid();
            var obligation = new Obligation { TenantId = tenantId, SpaceId = spaceId, Description = "Referenced" };
            db.AddRange(obligation,
                new Payment { TenantId = tenantId, SpaceId = spaceId, AccountId = accountId, PayerId = personId, PaymentDate = new DateOnly(2026, 11, 9), Amount = 1m, Method = "Transfer", IdempotencyKey = "test" },
                new PayableTitle { TenantId = tenantId, SpaceId = spaceId, ObligationId = obligation.Id, CardId = cardId, Period = new DateOnly(2026, 11, 1), OriginalAmount = 10m, OccurrenceKey = "test" });
            await db.SaveChangesAsync();
        }
        foreach (var (path, id) in new[] { ("contas", accountId), ("cartoes", cardId), ("instituicoes", institutionId), ("pessoas", personId) })
        {
            var deleted = await browser.DeleteAsync($"/api/v1/{path}/{id}?version=1");
            Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
            var problem = await deleted.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains("Desative", problem.GetProperty("detail").GetString());
        }
        var deactivated = await browser.PutAsJsonAsync("/api/v1/contas/" + accountId, new { institutionId, holderId = personId, type = "Current", agency = accountInput.agency, number = accountInput.number, checkDigit = accountInput.checkDigit, currency = "BRL", overdraftLimit = accountInput.overdraftLimit, active = false, version = 1 });
        deactivated.EnsureSuccessStatusCode();
        await using var verify = api.OpenMaintenance();
        Assert.False((await verify.BankAccounts.SingleAsync(x => x.Id == accountId)).Active);
        Assert.Equal(1, await verify.Payments.CountAsync());
        Assert.Equal(1, await verify.PayableTitles.CountAsync());
    }
}
