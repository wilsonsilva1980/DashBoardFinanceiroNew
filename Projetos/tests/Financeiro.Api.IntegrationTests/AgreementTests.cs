using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.IntegrationTests;

[Trait("Category", "Database")]
public class AgreementTests
{
    private sealed record Seed(Guid PersonId, Guid AccountId, Guid ObligationId, Guid FirstTitleId, Guid SecondTitleId);

    private static async Task<Seed> SeedAsync(HttpClient browser)
    {
        (await browser.PostAsJsonAsync("/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "agreement" })).EnsureSuccessStatusCode();
        var person = await PostAsync(browser, "/api/v1/pessoas", new { name = "Owner" });
        var institution = await PostAsync(browser, "/api/v1/instituicoes", new { name = "Bank" });
        var account = await PostAsync(browser, "/api/v1/contas", new { institutionId = institution.GetProperty("id").GetGuid(), number = "0000123", currency = "BRL", overdraftLimit = "0.00" });
        var obligation = await PostAsync(browser, "/api/v1/obrigacoes", new { description = "Loan", kind = "Single", currency = "BRL" });
        var obligationId = obligation.GetProperty("id").GetGuid();
        var first = await PostAsync(browser, "/api/v1/titulos", new { obligationId, period = "2026-11-01", occurrenceKey = "loan-a", originalAmount = "100.00", state = "Effective" });
        var second = await PostAsync(browser, "/api/v1/titulos", new { obligationId, period = "2026-11-01", occurrenceKey = "loan-b", originalAmount = "50.00", state = "Effective" });
        return new(person.GetProperty("id").GetGuid(), account.GetProperty("id").GetGuid(), obligationId, first.GetProperty("id").GetGuid(), second.GetProperty("id").GetGuid());
    }

    private static async Task<JsonElement> PostAsync(HttpClient browser, string path, object input)
    {
        var response = await browser.PostAsJsonAsync(path, input);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static object Lines(Seed seed) => new object[]
    {
        new { titleId = seed.FirstTitleId, nominalAmount = 100.00m, discount = 20.00m },
        new { titleId = seed.SecondTitleId, nominalAmount = 50.00m, discount = 0.00m }
    };

    [Fact]
    public async Task ConfirmedAgreementAppliesDiscountsAndPaymentWithoutDuplicatingPayables()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        var seed = await SeedAsync(browser);

        var simulation = await PostAsync(browser, "/api/v1/acordos/simulacoes", new
        {
            obligationId = seed.ObligationId, name = "Proposta inicial", simulationDate = "2026-11-10", notes = "Somente estudo",
            titles = Lines(seed)
        });
        Assert.Equal("130.00", simulation.GetProperty("proposedAmount").GetString());
        var untouched = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/titulos?obligationId={seed.ObligationId}");
        Assert.Equal(2, untouched.GetProperty("total").GetInt32());
        Assert.All(untouched.GetProperty("items").EnumerateArray(), title => Assert.Equal("Open", title.GetProperty("settlementStatus").GetString()));

        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/api/v1/acordos", new
        {
            obligationId = seed.ObligationId, proposalDate = "2026-11-10", validUntil = "2026-11-30",
            titles = new object[] { new { titleId = seed.FirstTitleId, nominalAmount = 99.00m, discount = 0.00m } }
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/api/v1/acordos", new
        {
            obligationId = seed.ObligationId, proposalDate = "2026-11-10", validUntil = "2026-11-30",
            titles = new object[] { new { titleId = seed.FirstTitleId, nominalAmount = 100.00m, discount = 120.00m } }
        })).StatusCode);

        var zeroNet = await PostAsync(browser, "/api/v1/acordos", new
        {
            obligationId = seed.ObligationId, proposalDate = "2026-11-10", validUntil = "2026-11-30",
            titles = new object[] { new { titleId = seed.FirstTitleId, nominalAmount = 100.00m, discount = 100.00m } }
        });
        Assert.Equal("0.00", zeroNet.GetProperty("proposedAmount").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PutAsJsonAsync($"/api/v1/acordos/{zeroNet.GetProperty("id").GetGuid()}/confirmar",
            new { accountId = seed.AccountId, payerId = seed.PersonId, paymentDate = "2026-11-12", currency = "BRL", method = "Pix", idempotencyKey = "agreement-zero", version = 1 })).StatusCode);

        var agreement = await PostAsync(browser, "/api/v1/acordos", new { obligationId = seed.ObligationId, proposalDate = "2026-11-10", validUntil = "2026-11-30", titles = Lines(seed) });
        var agreementId = agreement.GetProperty("id").GetGuid();
        Assert.Equal("Draft", agreement.GetProperty("state").GetString());
        Assert.Equal("130.00", agreement.GetProperty("proposedAmount").GetString());

        var confirmation = new { accountId = seed.AccountId, payerId = seed.PersonId, paymentDate = "2026-11-12", currency = "BRL", method = "Pix", idempotencyKey = "agreement-1", version = 1 };
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PutAsJsonAsync($"/api/v1/acordos/{agreementId}/confirmar", new { accountId = seed.AccountId, payerId = seed.PersonId, paymentDate = "2026-11-12", currency = "USD", method = "Pix", idempotencyKey = "agreement-1", version = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await browser.PutAsJsonAsync($"/api/v1/acordos/{agreementId}/confirmar", new { accountId = seed.AccountId, payerId = seed.PersonId, paymentDate = "2026-11-12", currency = "BRL", method = "Pix", idempotencyKey = "agreement-1", version = 99 })).StatusCode);

        var confirmedResponse = await browser.PutAsJsonAsync($"/api/v1/acordos/{agreementId}/confirmar", confirmation);
        confirmedResponse.EnsureSuccessStatusCode();
        var confirmed = await confirmedResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Confirmed", confirmed.GetProperty("state").GetString());
        Assert.NotEqual(JsonValueKind.Null, confirmed.GetProperty("paymentId").ValueKind);

        var replay = await browser.PutAsJsonAsync($"/api/v1/acordos/{agreementId}/confirmar", confirmation);
        replay.EnsureSuccessStatusCode();
        Assert.Equal(confirmed.GetProperty("paymentId").GetGuid(), (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("paymentId").GetGuid());

        var settled = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/titulos?obligationId={seed.ObligationId}");
        Assert.Equal(2, settled.GetProperty("total").GetInt32());
        var items = settled.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal("0.00", items.Single(x => x.GetProperty("occurrenceKey").GetString() == "loan-a").GetProperty("balance").GetString());
        Assert.Equal("Paid", items.Single(x => x.GetProperty("occurrenceKey").GetString() == "loan-a").GetProperty("settlementStatus").GetString());
        Assert.Equal("0.00", items.Single(x => x.GetProperty("occurrenceKey").GetString() == "loan-b").GetProperty("balance").GetString());
        Assert.Equal("Paid", items.Single(x => x.GetProperty("occurrenceKey").GetString() == "loan-b").GetProperty("settlementStatus").GetString());

        var cashFlow = await browser.GetFromJsonAsync<JsonElement>("/api/v1/dashboard/fluxo-caixa?from=2026-11-01&until=2026-12-01&currency=BRL");
        Assert.Equal("130.00", cashFlow.GetProperty("cashOutflow").GetString());

        await using var db = api.OpenMaintenance();
        Assert.Equal(1, await db.Payments.CountAsync(x => x.IdempotencyKey == "agreement-1"));
        Assert.Equal(0, await db.TitleAdjustments.CountAsync(x => x.AgreementId == agreementId));
        Assert.Equal(20m, (await db.AgreementTitles.SingleAsync(x => x.AgreementId == agreementId && x.TitleId == seed.FirstTitleId)).Discount);
        Assert.Equal(2, await db.PayableTitles.CountAsync(x => x.ObligationId == seed.ObligationId));
        Assert.True(await db.AuditRecords.AnyAsync(x => x.Action == "acordo.confirmado" && x.EntityId == agreementId));
    }
}
