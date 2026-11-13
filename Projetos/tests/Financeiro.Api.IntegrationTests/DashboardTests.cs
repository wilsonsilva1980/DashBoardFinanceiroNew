using System.Net.Http.Json;
using System.Text.Json;

namespace Financeiro.Api.IntegrationTests;

[Trait("Category", "Database")]
public class DashboardTests
{
    [Fact]
    public async Task SummaryDistinguishesStatesAndSurfacesReconciliationSeparately()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        (await browser.PostAsJsonAsync("/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "dashboard" })).EnsureSuccessStatusCode();

        var obligation = await PostAsync(browser, "/api/v1/obrigacoes", new { description = "Loan", kind = "Single", currency = "BRL" });
        var obligationId = obligation.GetProperty("id").GetGuid();
        var effective = await PostAsync(browser, "/api/v1/titulos", new { obligationId, period = "2026-11-01", dueDate = "2026-11-10", occurrenceKey = "eff", originalAmount = "100.00", state = "Effective" });
        var forecast = await PostAsync(browser, "/api/v1/titulos", new { obligationId, period = "2026-11-01", dueDate = "2026-11-15", occurrenceKey = "fc", originalAmount = "50.00", state = "Forecast" });
        var excessTitle = await PostAsync(browser, "/api/v1/titulos", new { obligationId, period = "2026-11-01", dueDate = "2026-11-20", occurrenceKey = "ex", originalAmount = "30.00", state = "Effective" });

        var excessTitleId = excessTitle.GetProperty("id").GetGuid();
        var settlement = await browser.PostAsJsonAsync($"/api/v1/titulos/{excessTitleId}/liquidacoes-historicas", new { amount = "40.00", effectiveDate = "2026-11-05", justification = "Pago a mais" });
        settlement.EnsureSuccessStatusCode();

        var summary = await browser.GetFromJsonAsync<JsonElement>("/api/v1/dashboard/resumo?from=2026-11-01&until=2026-12-01&currency=BRL");
        Assert.Equal("BRL", summary.GetProperty("currency").GetString());
        Assert.Equal("100.00", summary.GetProperty("totals").GetProperty("effective").GetString()); // excess title fully settled -> outstanding 0, effective open title 100
        Assert.Equal("50.00", summary.GetProperty("totals").GetProperty("forecast").GetString());
        Assert.True(summary.GetProperty("monthlyEvolution").GetArrayLength() >= 1);
        Assert.True(summary.GetProperty("byDueDate").GetArrayLength() >= 1);

        var indicators = summary.GetProperty("reconciliationIndicators").EnumerateArray().ToArray();
        Assert.Contains(indicators, i => i.GetProperty("kind").GetString() == "ExcessCredit" && i.GetProperty("scope").GetString() == "PayableTitle");
        // Reconciliation indicators must be separate from normal payable totals.
        Assert.DoesNotContain("-10.00", summary.GetRawText());
    }

    private static async Task<JsonElement> PostAsync(HttpClient browser, string path, object input)
    {
        var response = await browser.PostAsJsonAsync(path, input);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
