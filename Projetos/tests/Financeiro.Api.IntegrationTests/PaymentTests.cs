using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.IntegrationTests;

[Trait("Category", "Database")]
public class PaymentTests
{
    private sealed record Seed(Guid PersonId, Guid AccountId, Guid ObligationId, Guid FirstTitleId, Guid SecondTitleId);

    private static async Task<Seed> SeedAsync(HttpClient browser, string requestKey = "payments")
    {
        var onboarding = await browser.PostAsJsonAsync("/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey });
        onboarding.EnsureSuccessStatusCode();
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

    private static object Payment(Seed seed, string idempotencyKey, string currency = "BRL", string amount = "150.00", params (Guid TitleId, string Amount)[] allocations) => new
    {
        accountId = seed.AccountId,
        payerId = seed.PersonId,
        paymentDate = "2026-11-12",
        amount,
        currency,
        method = "Pix",
        idempotencyKey,
        allocations = allocations.Select(allocation => new { titleId = allocation.TitleId, amount = allocation.Amount })
    };

    [Fact]
    public async Task ConfirmedPaymentsAllocateExactlyOnceAndRetriesReplayTheResult()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        var seed = await SeedAsync(browser);

        var draft = await PostAsync(browser, "/api/v1/pagamentos", Payment(seed, "pay-1", allocations: [(seed.FirstTitleId, "100.00"), (seed.SecondTitleId, "50.00")]));
        Assert.Equal("Draft", draft.GetProperty("state").GetString());
        var paymentId = draft.GetProperty("id").GetGuid();

        var confirmed = await browser.PutAsJsonAsync($"/api/v1/pagamentos/{paymentId}/confirmar", new { version = 1 });
        confirmed.EnsureSuccessStatusCode();
        var payment = await confirmed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Confirmed", payment.GetProperty("state").GetString());
        Assert.Equal(2, payment.GetProperty("allocations").GetArrayLength());

        var titles = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/titulos?obligationId={seed.ObligationId}");
        var first = titles.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("occurrenceKey").GetString() == "loan-a");
        Assert.Equal("0.00", first.GetProperty("balance").GetString());
        Assert.Equal("100.00", first.GetProperty("settledAmount").GetString());
        Assert.Equal("Paid", first.GetProperty("settlementStatus").GetString());

        var replay = await browser.PutAsJsonAsync($"/api/v1/pagamentos/{paymentId}/confirmar", new { version = 1 });
        replay.EnsureSuccessStatusCode();
        Assert.Equal(paymentId, (await replay.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());
        var afterReplay = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/titulos?obligationId={seed.ObligationId}");
        var replayItems = afterReplay.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal("100.00", replayItems.Single(x => x.GetProperty("occurrenceKey").GetString() == "loan-a").GetProperty("settledAmount").GetString());
        Assert.Equal("50.00", replayItems.Single(x => x.GetProperty("occurrenceKey").GetString() == "loan-b").GetProperty("settledAmount").GetString());

        await using var db = api.OpenMaintenance();
        Assert.Equal(1, await db.Payments.CountAsync(x => x.Id == paymentId));
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(x => x.RequestKey == "pay-1"));
        Assert.Equal(1, await db.AuditRecords.CountAsync(x => x.Action == "pagamento.confirmado" && x.EntityId == paymentId));
    }

    [Fact]
    public async Task ConfirmationRejectsMismatchedTotalsAndCurrency()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        var seed = await SeedAsync(browser, "payments-invalid");

        var mismatch = await PostAsync(browser, "/api/v1/pagamentos", Payment(seed, "pay-mismatch", amount: "100.00", allocations: [(seed.FirstTitleId, "40.00"), (seed.SecondTitleId, "30.00")]));
        var mismatchId = mismatch.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PutAsJsonAsync($"/api/v1/pagamentos/{mismatchId}/confirmar", new { version = 1 })).StatusCode);
        var stillDraft = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/pagamentos/{mismatchId}");
        Assert.Equal("Draft", stillDraft.GetProperty("state").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/api/v1/pagamentos",
            Payment(seed, "pay-currency", currency: "USD", amount: "10.00", allocations: [(seed.FirstTitleId, "10.00")]))).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsJsonAsync("/api/v1/pagamentos",
            Payment(seed, "pay-negative", amount: "10.00", allocations: [(seed.FirstTitleId, "-10.00")]))).StatusCode);
    }

    [Fact]
    public async Task ConcurrentConfirmationsCreateOneSettlementPerRequestIdentity()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        var seed = await SeedAsync(browser, "payments-concurrent");

        var draft = await PostAsync(browser, "/api/v1/pagamentos", Payment(seed, "retry-key", amount: "100.00", allocations: [(seed.FirstTitleId, "100.00")]));
        var paymentId = draft.GetProperty("id").GetGuid();

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ =>
            browser.PutAsJsonAsync($"/api/v1/pagamentos/{paymentId}/confirmar", new { version = 1 })));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        foreach (var response in responses)
            Assert.Equal(paymentId, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid());

        var titles = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/titulos?obligationId={seed.ObligationId}");
        var first = titles.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("occurrenceKey").GetString() == "loan-a");
        Assert.Equal("100.00", first.GetProperty("settledAmount").GetString());
        Assert.Equal("0.00", first.GetProperty("balance").GetString());

        await using var db = api.OpenMaintenance();
        Assert.Equal(1, await db.Payments.CountAsync(x => x.IdempotencyKey == "retry-key"));
        Assert.Equal(1, await db.IdempotencyRecords.CountAsync(x => x.RequestKey == "retry-key"));
        Assert.Equal(1, await db.AuditRecords.CountAsync(x => x.Action == "pagamento.confirmado" && x.EntityId == paymentId));
    }

    [Fact]
    public async Task ReversingAConfirmedPaymentRestoresTitleBalancesWithoutDeletingHistory()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        var seed = await SeedAsync(browser, "payments-reverse");

        var draft = await PostAsync(browser, "/api/v1/pagamentos", Payment(seed, "reverse-key", amount: "150.00", allocations: [(seed.FirstTitleId, "100.00"), (seed.SecondTitleId, "50.00")]));
        var paymentId = draft.GetProperty("id").GetGuid();
        (await browser.PutAsJsonAsync($"/api/v1/pagamentos/{paymentId}/confirmar", new { version = 1 })).EnsureSuccessStatusCode();

        var paid = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/titulos?obligationId={seed.ObligationId}");
        Assert.All(paid.GetProperty("items").EnumerateArray(), title => Assert.Equal("Paid", title.GetProperty("settlementStatus").GetString()));

        var draftPayment = await PostAsync(browser, "/api/v1/pagamentos", Payment(seed, "reverse-draft", amount: "10.00", allocations: [(seed.FirstTitleId, "10.00")]));
        var draftPaymentId = draftPayment.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await browser.PutAsJsonAsync($"/api/v1/pagamentos/{draftPaymentId}/reverter", new { reason = "Engano", version = 1 })).StatusCode);

        Assert.Equal(HttpStatusCode.Conflict, (await browser.PutAsJsonAsync($"/api/v1/pagamentos/{paymentId}/reverter", new { reason = "Estorno", version = 1 })).StatusCode);

        var reversed = await browser.PutAsJsonAsync($"/api/v1/pagamentos/{paymentId}/reverter", new { reason = "Estorno solicitado", version = 2 });
        reversed.EnsureSuccessStatusCode();
        var payment = await reversed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Reversed", payment.GetProperty("state").GetString());
        Assert.Equal("Estorno solicitado", payment.GetProperty("reversalReason").GetString());
        Assert.NotEqual(JsonValueKind.Null, payment.GetProperty("reversedAt").ValueKind);
        Assert.Equal(2, payment.GetProperty("allocations").GetArrayLength());

        var restored = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/titulos?obligationId={seed.ObligationId}");
        var first = restored.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("occurrenceKey").GetString() == "loan-a");
        Assert.Equal("0.00", first.GetProperty("settledAmount").GetString());
        Assert.Equal("100.00", first.GetProperty("balance").GetString());
        Assert.Equal("Open", first.GetProperty("settlementStatus").GetString());
        var second = restored.GetProperty("items").EnumerateArray().Single(x => x.GetProperty("occurrenceKey").GetString() == "loan-b");
        Assert.Equal("0.00", second.GetProperty("settledAmount").GetString());
        Assert.Equal("50.00", second.GetProperty("balance").GetString());
        Assert.Equal("Open", second.GetProperty("settlementStatus").GetString());

        Assert.Equal(HttpStatusCode.Conflict, (await browser.PutAsJsonAsync($"/api/v1/pagamentos/{paymentId}/reverter", new { reason = "Estorno repetido", version = 3 })).StatusCode);

        var history = await browser.GetFromJsonAsync<JsonElement>($"/api/v1/pagamentos/{paymentId}");
        Assert.Equal("Reversed", history.GetProperty("state").GetString());
        Assert.Equal(2, history.GetProperty("allocations").GetArrayLength());

        await using var db = api.OpenMaintenance();
        Assert.Equal(1, await db.Payments.CountAsync(x => x.Id == paymentId));
        Assert.Equal(2, await db.PaymentAllocations.CountAsync(x => x.PaymentId == paymentId));
        Assert.Equal(1, await db.AuditRecords.CountAsync(x => x.Action == "pagamento.revertido" && x.EntityId == paymentId));
    }
}
