using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Financeiro.Api.IntegrationTests;

[Trait("Category", "Database")]
public class TravelCurrencyTests
{
    [Fact]
    public async Task ConversionUsesCapturedRateWithExplicitRoundingAndReturnsEvidence()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        (await browser.PostAsJsonAsync("/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "initial" })).EnsureSuccessStatusCode();

        var budget = await PostJson(browser, "/api/v1/viagens", new { name = "Trip", startDate = "2026-11-01", endDate = "2026-11-10" });
        var travelId = budget.GetProperty("id").GetGuid();

        var item = await PostJson(browser, $"/api/v1/viagens/{travelId}/itens", new
        {
            travelId,
            description = "Hotel",
            state = "Budget",
            currency = "USD",
            baseAmount = "10.01",
            fees = "0.02"
        });
        var itemId = item.GetProperty("id").GetGuid();
        Assert.Equal("USD", item.GetProperty("currency").GetString());
        Assert.Equal("10.01", item.GetProperty("baseAmount").GetString());
        Assert.Equal("0.02", item.GetProperty("fees").GetString());

        var rate = await PostJson(browser, "/api/v1/cotacoes", new
        {
            sourceCurrency = "usd",
            destinationCurrency = "brl",
            rateDate = "2026-11-05",
            rate = "5.333",
            source = "Manual"
        });
        var rateId = rate.GetProperty("id").GetGuid();
        Assert.Equal("USD", rate.GetProperty("sourceCurrency").GetString());
        Assert.Equal("BRL", rate.GetProperty("destinationCurrency").GetString());

        var conversion = await PostJson(browser, $"/api/v1/viagens/{travelId}/itens/{itemId}/conversoes", new
        {
            itemId,
            rateId,
            destinationCurrency = "brl",
            mode = "Calculated"
        });

        // (10.01 + 0.02) * 5.333 = 53.48999 -> 53.49 (AwayFromZero, 2 decimals)
        Assert.Equal("53.49", conversion.GetProperty("reportedAmount").GetString());
        Assert.Equal(rateId, conversion.GetProperty("rateId").GetGuid());
        Assert.Equal("5.333", conversion.GetProperty("appliedRate").GetString());
        Assert.Equal("2026-11-05", conversion.GetProperty("appliedRateDate").GetString());
        Assert.Equal("Manual", conversion.GetProperty("appliedRateSource").GetString());
        Assert.Equal("Calculated", conversion.GetProperty("mode").GetString());

        var list = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/viagens/{travelId}/itens/{itemId}/conversoes");
        Assert.Equal(1, list.GetProperty("total").GetInt32());
        var listed = list.GetProperty("items")[0];
        Assert.Equal("53.49", listed.GetProperty("reportedAmount").GetString());
        Assert.Equal("5.333", listed.GetProperty("appliedRate").GetString());
        Assert.Equal("2026-11-05", listed.GetProperty("appliedRateDate").GetString());
        Assert.Equal("Manual", listed.GetProperty("appliedRateSource").GetString());
    }

    [Fact]
    public async Task ReconciliationShowsSourceTotalMismatchAndNegativeTravelBalance()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        (await browser.PostAsJsonAsync("/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "travel-recon" })).EnsureSuccessStatusCode();

        var budget = await PostJson(browser, "/api/v1/viagens", new { name = "Trip", startDate = "2026-11-01", endDate = "2026-11-10" });
        var travelId = budget.GetProperty("id").GetGuid();

        var mismatchItem = await PostJson(browser, $"/api/v1/viagens/{travelId}/itens", new
        {
            travelId,
            description = "Taxi",
            state = "Budget",
            currency = "USD",
            baseAmount = "10.00",
            fees = "0.00"
        });
        var mismatchItemId = mismatchItem.GetProperty("id").GetGuid();
        var rate = await PostJson(browser, "/api/v1/cotacoes", new { sourceCurrency = "USD", destinationCurrency = "BRL", rateDate = "2026-11-05", rate = "5.00", source = "Manual" });
        var rateId = rate.GetProperty("id").GetGuid();
        await PostJson(browser, $"/api/v1/viagens/{travelId}/itens/{mismatchItemId}/conversoes", new
        {
            itemId = mismatchItemId,
            rateId,
            destinationCurrency = "BRL",
            mode = "Reported",
            reportedAmount = "60.00"
        });

        var obligation = await PostJson(browser, "/api/v1/obrigacoes", new { description = "Passeio", kind = "Single", currency = "BRL" });
        var obligationId = obligation.GetProperty("id").GetGuid();
        var title = await PostJson(browser, "/api/v1/titulos", new { obligationId, period = "2026-11-01", occurrenceKey = "trip-title", originalAmount = "100.00", state = "Effective" });
        var titleId = title.GetProperty("id").GetGuid();
        await PostJson(browser, $"/api/v1/titulos/{titleId}/liquidacoes-historicas", new { amount = "120.00", effectiveDate = "2026-11-06", justification = "Pago a mais" });
        await PostJson(browser, $"/api/v1/viagens/{travelId}/itens", new
        {
            travelId,
            obligationId,
            description = "Passeio pago",
            state = "Effective",
            currency = "BRL",
            baseAmount = "100.00",
            fees = "0.00"
        });

        var reconciliation = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/viagens/{travelId}/reconciliacao");
        var discrepancies = reconciliation.GetProperty("discrepancies").EnumerateArray().ToArray();
        Assert.Contains(discrepancies, d => d.GetProperty("kind").GetString() == "SourceTotalMismatch");
        Assert.Contains(discrepancies, d => d.GetProperty("kind").GetString() == "NegativeTravelBalance");
        Assert.StartsWith("-", reconciliation.GetProperty("balance").GetString());
    }

    [Fact]
    public async Task UnauthorizedUsersCannotViewOrMutateAnotherSpacesTravelData()
    {
        await using var api = await TestApi.CreateAsync();
        using var owner = api.Browser();
        await TestApi.RegisterAndLoginAsync(owner, "owner@example.test");
        (await owner.PostAsJsonAsync("/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "travel-owner" })).EnsureSuccessStatusCode();
        var budget = await PostJson(owner, "/api/v1/viagens", new { name = "Private trip" });
        var travelId = budget.GetProperty("id").GetGuid();

        using var intruder = api.Browser();
        await TestApi.RegisterAndLoginAsync(intruder, "intruder@example.test");
        (await intruder.PostAsJsonAsync("/api/v1/context/onboarding", new { tenantName = "Other", spaceName = "Other", currency = "BRL", requestKey = "travel-intruder" })).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/v1/viagens/{travelId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.GetAsync($"/api/v1/viagens/{travelId}/reconciliacao")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PostAsJsonAsync($"/api/v1/viagens/{travelId}/itens", new { travelId, description = "Hack", state = "Budget", currency = "USD", baseAmount = "1.00", fees = "0.00" })).StatusCode);

        var list = await intruder.GetFromJsonAsync<JsonElement>("/api/v1/viagens");
        Assert.Equal(0, list.GetProperty("total").GetInt32());
    }

    private static async Task<JsonElement> PostJson(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
