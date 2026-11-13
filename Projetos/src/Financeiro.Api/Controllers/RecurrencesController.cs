using Financeiro.Application.Catalogs;
using Financeiro.Application.Recurrences;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/recorrencias")]
public sealed class RecurrencesController(IRecurrenceService recurrences) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<RecurrenceEntry>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        Ok(await recurrences.ListAsync(new(page, pageSize), HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RecurrenceEntry>> Get(Guid id) => Ok(await recurrences.GetAsync(id, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<RecurrenceEntry>> Create(RecurrenceInput input)
    {
        var result = await recurrences.SaveAsync(null, input, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RecurrenceEntry>> Update(Guid id, RecurrenceInput input) => Ok(await recurrences.SaveAsync(id, input, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost("{id:guid}/gerar")]
    public async Task<ActionResult<RecurrenceGenerationResult>> Generate(Guid id, RecurrenceGenerationInput input) =>
        Ok(await recurrences.GenerateHorizonAsync(id, input, HttpContext.RequestAborted));
}
