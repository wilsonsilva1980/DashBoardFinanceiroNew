using Financeiro.Application.Catalogs;
using Financeiro.Application.Travel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/cotacoes")]
public sealed class CotacoesController(ITravelService travel) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<ExchangeRateEntry>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        Ok(await travel.ListRatesAsync(new(page, pageSize), HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<ExchangeRateEntry>> Create(ExchangeRateInput input)
    {
        var result = await travel.SaveRateAsync(null, input, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExchangeRateEntry>> Update(Guid id, ExchangeRateInput input) =>
        Ok(await travel.SaveRateAsync(id, input, HttpContext.RequestAborted));
}
