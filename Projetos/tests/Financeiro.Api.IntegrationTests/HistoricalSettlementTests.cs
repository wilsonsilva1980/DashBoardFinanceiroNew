using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.IntegrationTests;

[Trait("Category", "Database")]
public class HistoricalSettlementTests
{
    private static async Task<(Guid ObligationId, Guid TitleId)> SeedAsync(HttpClient browser)
    {
        (await browser.PostAsJsonAsync("/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "history" })).EnsureSuccessStatusCode();
        var obligationResponse = await browser.PostAsJsonAsync("/api/v1/obrigacoes", new { description = "Old loan", kind = "Single", currency = "BRL" });
        obligationResponse.EnsureSuccessStatusCode();
        var obligationId = (await obligationResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var titleResponse = await browser.PostAsJsonAsync("/api/v1/titulos", new { obligationId, period = "2026-11-01", occurrenceKey = "old-loan-1", originalAmount = "100.00", state = "Effective" });
        titleResponse.EnsureSuccessStatusCode();
        return (obligationId, (await titleResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
    }

    private static async Task<JsonElement> PostAsync(HttpClient browser, string path, object input)
    {
        var response = await browser.PostAsJsonAsync(path, input);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> TitleAsync(HttpClient browser, Guid obligationId) =>
        (await browser.GetFromJsonAsync<JsonElement>($"/api/v1/titulos?obligationId={obligationId}")).GetProperty("items").EnumerateArray().Single();

    private static async Task<JsonElement> CashFlowAsync(HttpClient browser) =>
        await browser.GetFromJsonAsync<JsonElement>("/api/v1/dashboard/fluxo-caixa?from=2026-11-01&until=2026-12-01&currency=BRL");

    [Fact]
    public async Task UndatedHistoricalSettlementsReduceBalancesWithoutEnteringDatedCashFlow()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        var (obligationId, titleId) = await SeedAsync(browser);
        var route = $"/api/v1/titulos/{titleId}/liquidacoes-historicas";

        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync(route, new { amount = "0.00", justification = "Sem valor" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync(route, new { amount = "-10.00", justification = "Valor inválido" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync(route, new { amount = "10.00", justification = "" })).StatusCode);

        var undated = await PostAsync(browser, route, new { amount = "40.00", justification = "Comprovante sem data", previousReference = "legacy-1", sourceStatus = "Paid" });
        Assert.Equal(JsonValueKind.Null, undated.GetProperty("effectiveDate").ValueKind);
        Assert.Equal("legacy-1", undated.GetProperty("previousReference").GetString());
        var partial = await TitleAsync(browser, obligationId);
        Assert.Equal("60.00", partial.GetProperty("balance").GetString());
        Assert.Equal("40.00", partial.GetProperty("settledAmount").GetString());
        Assert.Equal("Partial", partial.GetProperty("settlementStatus").GetString());
        Assert.Equal("0.00", (await CashFlowAsync(browser)).GetProperty("cashOutflow").GetString());

        var dated = await PostAsync(browser, route, new { amount = "10.00", effectiveDate = "2026-11-20", justification = "Baixa comprovada" });
        Assert.Equal("2026-11-20", dated.GetProperty("effectiveDate").GetString());
        var withCashFlow = await TitleAsync(browser, obligationId);
        Assert.Equal("50.00", withCashFlow.GetProperty("balance").GetString());
        Assert.Equal("50.00", withCashFlow.GetProperty("settledAmount").GetString());
        Assert.Equal("10.00", (await CashFlowAsync(browser)).GetProperty("cashOutflow").GetString());

        var listed = await browser.GetFromJsonAsync<JsonElement>(route);
        Assert.Equal(2, listed.GetArrayLength());

        var settledId = dated.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await browser.PostAsJsonAsync($"{route}/{settledId}/revogar", new { reason = "Duplicada", version = 99 })).StatusCode);
        var revoked = await browser.PostAsJsonAsync($"{route}/{settledId}/revogar", new { reason = "Duplicada", version = 1 });
        revoked.EnsureSuccessStatusCode();
        var revokedEntry = await revoked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Duplicada", revokedEntry.GetProperty("revocationReason").GetString());
        Assert.NotEqual(JsonValueKind.Null, revokedEntry.GetProperty("revokedAt").ValueKind);

        var afterRevocation = await TitleAsync(browser, obligationId);
        Assert.Equal("60.00", afterRevocation.GetProperty("balance").GetString());
        Assert.Equal("0.00", (await CashFlowAsync(browser)).GetProperty("cashOutflow").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await browser.PostAsJsonAsync($"{route}/{settledId}/revogar", new { reason = "Duplicada", version = 2 })).StatusCode);

        var replacement = await PostAsync(browser, route, new { amount = "10.00", effectiveDate = "2026-11-25", justification = "Substitui baixa revogada", previousReference = settledId.ToString() });
        Assert.Equal(settledId.ToString(), replacement.GetProperty("previousReference").GetString());
        Assert.Equal("10.00", (await CashFlowAsync(browser)).GetProperty("cashOutflow").GetString());

        await using var db = api.OpenMaintenance();
        Assert.True(await db.AuditRecords.AnyAsync(x => x.Action == "liquidacao-historica.registrada" && x.EntityId == undated.GetProperty("id").GetGuid()));
        Assert.True(await db.AuditRecords.AnyAsync(x => x.Action == "liquidacao-historica.revogada" && x.EntityId == settledId));
        Assert.True(await db.HistoricalSettlements.AnyAsync(x => x.Id == settledId && x.RevokedAt != null));
    }
}
