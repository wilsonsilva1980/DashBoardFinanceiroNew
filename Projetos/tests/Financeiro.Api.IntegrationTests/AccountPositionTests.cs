using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Financeiro.Api.IntegrationTests;

[Trait("Category", "Database")]
public class AccountPositionTests
{
    [Fact]
    public async Task PositionsHaveExplicitDatesAndRejectForeignAccountsAndNegativeUsage()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        (await browser.PostAsJsonAsync("/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "initial" })).EnsureSuccessStatusCode();
        var institutionResponse = await browser.PostAsJsonAsync("/api/v1/instituicoes", new { name = "Bank" });
        institutionResponse.EnsureSuccessStatusCode();
        var institutionId = (await institutionResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var accountResponse = await browser.PostAsJsonAsync("/api/v1/contas", new { institutionId, agency = "0001", number = "0000123", currency = "BRL", overdraftLimit = "100.00" });
        accountResponse.EnsureSuccessStatusCode();
        var accountId = (await accountResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var input = new { accountId, positionDate = "2026-11-09", balance = "10.00", savingsBalance = "500.00", overdraftUsed = "20.00" };
        var response = await browser.PostAsJsonAsync("/api/v1/posicoes-conta", input);
        response.EnsureSuccessStatusCode();
        var position = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("80.00", position.GetProperty("availableOverdraft").GetString());
        Assert.Equal("500.00", position.GetProperty("savingsBalance").GetString());
        var list = await browser.GetFromJsonAsync<JsonElement>("/api/v1/posicoes-conta?accountId=" + accountId);
        Assert.Equal(1, list.GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/api/v1/posicoes-conta", new { accountId, balance = "10.00" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/api/v1/posicoes-conta", new { accountId = Guid.NewGuid(), positionDate = "2026-11-09", balance = "10.00" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/api/v1/posicoes-conta", new { accountId, positionDate = "2026-11-09", overdraftUsed = "-1.00" })).StatusCode);
        var preserved = await browser.GetFromJsonAsync<JsonElement>("/api/v1/posicoes-conta?accountId=" + accountId);
        Assert.Equal(1, preserved.GetProperty("total").GetInt32());
    }
}
