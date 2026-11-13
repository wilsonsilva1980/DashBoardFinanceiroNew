using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Api.IntegrationTests;

[Trait("Category", "Database")]
public class ObligationTests
{
    private static async Task<JsonElement> PostAsync(HttpClient browser, string path, object input)
    {
        var response = await browser.PostAsJsonAsync(path, input);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Task<HttpResponseMessage> TransitionAsync(HttpClient browser, JsonElement obligation, Guid personId, Guid titleId, string state, long version) =>
        browser.PutAsJsonAsync("/api/v1/titulos/" + titleId, new
        {
            obligationId = obligation.GetProperty("id").GetGuid(),
            responsibleId = personId,
            period = "2026-12-01",
            dueDate = "2026-12-15",
            installmentNumber = 1,
            occurrenceKey = "insurance-2026-12",
            originalAmount = "120.50",
            state,
            version
        });

    [Fact]
    public async Task ObligationsTitlesAndPlanningEntriesSupportLifecycleTransitions()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        await PostAsync(browser, "/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "obligation" });
        var person = await PostAsync(browser, "/api/v1/pessoas", new { name = "Owner" });
        var category = await PostAsync(browser, "/api/v1/categorias", new { name = "Home" });
        var personId = person.GetProperty("id").GetGuid();
        var obligation = await PostAsync(browser, "/api/v1/obrigacoes", new
        {
            categoryId = category.GetProperty("id").GetGuid(),
            responsibleId = personId,
            description = "Insurance",
            creditor = "Insurer",
            kind = "Installment",
            dueDay = 15,
            installmentCount = 2,
            currency = "BRL"
        });
        var title = await PostAsync(browser, "/api/v1/titulos", new
        {
            obligationId = obligation.GetProperty("id").GetGuid(),
            responsibleId = personId,
            period = "2026-12-01",
            dueDate = "2026-12-15",
            installmentNumber = 1,
            occurrenceKey = "insurance-2026-12",
            originalAmount = "120.50"
        });
        Assert.Equal("Draft", title.GetProperty("state").GetString());
        var titleId = title.GetProperty("id").GetGuid();
        var forecast = await TransitionAsync(browser, obligation, personId, titleId, "Forecast", 1);
        forecast.EnsureSuccessStatusCode();

        var effective = await TransitionAsync(browser, obligation, personId, titleId, "Effective", 2);
        effective.EnsureSuccessStatusCode();
        var cancelled = await TransitionAsync(browser, obligation, personId, titleId, "Cancelled", 3);
        cancelled.EnsureSuccessStatusCode();
        var cancelledTitle = await cancelled.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Null, cancelledTitle.GetProperty("cancelledAt").ValueKind);
        Assert.Equal(HttpStatusCode.BadRequest, (await TransitionAsync(browser, obligation, personId, titleId, "Effective", 4)).StatusCode);

        var planning = await browser.PutAsJsonAsync("/api/v1/titulos/" + titleId + "/planejamentos", new { personId, amount = "120.50" });
        planning.EnsureSuccessStatusCode();
        var planningEntry = await planning.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("120.50", planningEntry.GetProperty("amount").GetString());
        var titles = await browser.GetFromJsonAsync<JsonElement>("/api/v1/titulos?obligationId=" + obligation.GetProperty("id").GetGuid());
        Assert.Equal(1, titles.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task ForecastTitlesStayOutOfStandardTotalsAndUnknownDueDatesAreNotOverdue()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        await PostAsync(browser, "/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "balance" });
        var obligation = await PostAsync(browser, "/api/v1/obrigacoes", new { description = "Rent", kind = "Single", currency = "BRL" });
        var obligationId = obligation.GetProperty("id").GetGuid();
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1).ToString("yyyy-MM-dd");

        await PostAsync(browser, "/api/v1/titulos", new { obligationId, period = "2026-11-01", dueDate = yesterday, occurrenceKey = "rent-overdue", originalAmount = "30.00", state = "Effective" });
        await PostAsync(browser, "/api/v1/titulos", new { obligationId, period = "2026-11-01", occurrenceKey = "rent-undated", originalAmount = "50.00", state = "Effective" });
        await PostAsync(browser, "/api/v1/titulos", new { obligationId, period = "2026-12-01", occurrenceKey = "rent-forecast", originalAmount = "70.00", state = "Forecast" });

        var standard = await browser.GetFromJsonAsync<JsonElement>("/api/v1/titulos?obligationId=" + obligationId);
        Assert.Equal(2, standard.GetProperty("total").GetInt32());
        var items = standard.GetProperty("items").EnumerateArray().ToArray();
        var overdue = items.Single(x => x.GetProperty("occurrenceKey").GetString() == "rent-overdue");
        Assert.Equal("Overdue", overdue.GetProperty("settlementStatus").GetString());
        Assert.Equal("30.00", overdue.GetProperty("balance").GetString());
        var undated = items.Single(x => x.GetProperty("occurrenceKey").GetString() == "rent-undated");
        Assert.Equal(JsonValueKind.Null, undated.GetProperty("dueDate").ValueKind);
        Assert.Equal("Open", undated.GetProperty("settlementStatus").GetString());
        Assert.Equal("50.00", undated.GetProperty("balance").GetString());

        var planning = await browser.GetFromJsonAsync<JsonElement>("/api/v1/titulos?obligationId=" + obligationId + "&includeForecast=true");
        Assert.Equal(3, planning.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task ConcurrentEditsConflictAndOnlyUnreferencedDraftsAreDeleted()
    {
        await using var api = await TestApi.CreateAsync();
        using var browser = api.Browser();
        await TestApi.RegisterAndLoginAsync(browser, "owner@example.test");
        await PostAsync(browser, "/api/v1/context/onboarding", new { tenantName = "Family", spaceName = "Main", currency = "BRL", requestKey = "delete" });
        var person = await PostAsync(browser, "/api/v1/pessoas", new { name = "Owner" });
        var personId = person.GetProperty("id").GetGuid();
        var obligation = await PostAsync(browser, "/api/v1/obrigacoes", new { description = "Internet", kind = "Single", currency = "BRL" });
        var obligationId = obligation.GetProperty("id").GetGuid();

        var stale = await browser.PutAsJsonAsync("/api/v1/obrigacoes/" + obligationId, new { description = "Internet", kind = "Single", currency = "BRL", version = 99 });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        var draft = await PostAsync(browser, "/api/v1/titulos", new { obligationId, period = "2026-11-01", occurrenceKey = "internet-draft", originalAmount = "80.00" });
        var draftId = draft.GetProperty("id").GetGuid();
        (await browser.PutAsJsonAsync($"/api/v1/titulos/{draftId}/planejamentos", new { personId, amount = "80.00" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await browser.DeleteAsync($"/api/v1/titulos/{draftId}?version=1")).StatusCode);

        var effective = await PostAsync(browser, "/api/v1/titulos", new { obligationId, period = "2026-11-01", occurrenceKey = "internet-effective", originalAmount = "80.00", state = "Effective" });
        var effectiveId = effective.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await browser.DeleteAsync($"/api/v1/titulos/{effectiveId}?version=1")).StatusCode);
        var cancel = await browser.PutAsJsonAsync($"/api/v1/titulos/{effectiveId}", new { obligationId, period = "2026-11-01", occurrenceKey = "internet-effective", originalAmount = "80.00", state = "Cancelled", version = 1 });
        cancel.EnsureSuccessStatusCode();

        var spare = await PostAsync(browser, "/api/v1/titulos", new { obligationId, period = "2026-11-01", occurrenceKey = "internet-spare", originalAmount = "10.00" });
        var spareId = spare.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await browser.DeleteAsync($"/api/v1/titulos/{spareId}?version=1")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync($"/api/v1/titulos/{spareId}")).StatusCode);

        await using var db = api.OpenMaintenance();
        Assert.True(await db.AuditRecords.AnyAsync(x => x.Action == "titulo.excluido" && x.EntityId == spareId));
        Assert.True(await db.AuditRecords.AnyAsync(x => x.Action == "titulo.cancelado" && x.EntityId == effectiveId));
    }
}
