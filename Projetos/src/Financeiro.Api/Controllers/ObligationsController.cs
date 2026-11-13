using Financeiro.Application.Catalogs;
using Financeiro.Application.Obligations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/obrigacoes")]
public sealed class ObligationsController(IObligationService obligations) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<ObligationEntry>>> List(int page = 1, int pageSize = 25, bool includeInactive = true) =>
        Ok(await obligations.ListAsync(new(page, pageSize, IncludeInactive: includeInactive), HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ObligationEntry>> Get(Guid id) => Ok(await obligations.GetAsync(id, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<ObligationEntry>> Create(ObligationInput input)
    {
        var result = await obligations.SaveAsync(null, input, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ObligationEntry>> Update(Guid id, ObligationInput input) => Ok(await obligations.SaveAsync(id, input, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] long version)
    {
        await obligations.DeleteAsync(id, version, HttpContext.RequestAborted);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/titulos")]
public sealed class PayableTitlesController(IObligationService obligations) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<PayableTitleEntry>>> List(Guid? obligationId = null, int page = 1, int pageSize = 25, bool includeForecast = false, DateOnly? asOf = null, Guid? cardId = null) =>
        Ok(await obligations.ListTitlesAsync(new(page, pageSize), obligationId, includeForecast, asOf, cardId, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PayableTitleEntry>> Get(Guid id) => Ok(await obligations.GetTitleAsync(id, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<PayableTitleEntry>> Create(PayableTitleInput input)
    {
        var result = await obligations.SaveTitleAsync(null, input, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PayableTitleEntry>> Update(Guid id, PayableTitleInput input) => Ok(await obligations.SaveTitleAsync(id, input, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] long version)
    {
        await obligations.DeleteTitleAsync(id, version, HttpContext.RequestAborted);
        return NoContent();
    }

    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}/planejamentos")]
    public async Task<ActionResult<IReadOnlyList<PlanningEntryEntry>>> PlanningEntries(Guid id) =>
        Ok(await obligations.PlanningEntriesAsync(id, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}/planejamentos")]
    public async Task<ActionResult<PlanningEntryEntry>> SavePlanningEntry(Guid id, PlanningEntryInput input) =>
        Ok(await obligations.SavePlanningEntryAsync(id, input, HttpContext.RequestAborted));
}
