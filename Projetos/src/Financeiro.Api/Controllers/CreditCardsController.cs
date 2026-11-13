using Financeiro.Application.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/cartoes")]
public sealed class CreditCardsController(ICatalogService catalogs) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<CreditCardEntry>>> List(int page = 1, int pageSize = 25, string? search = null, bool includeInactive = true) =>
        Ok(await catalogs.CardsAsync(new(page, pageSize, search, includeInactive), HttpContext.RequestAborted));
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CreditCardEntry>> Get(Guid id) => Ok(await catalogs.CardAsync(id, HttpContext.RequestAborted));
    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<CreditCardEntry>> Create(CreditCardInput input)
    {
        var result = await catalogs.SaveCardAsync(null, input, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }
    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CreditCardEntry>> Update(Guid id, CreditCardInput input) => Ok(await catalogs.SaveCardAsync(id, input, HttpContext.RequestAborted));
    [Authorize(Policy = "cadastros.editar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] long version)
    {
        await catalogs.DeleteCardAsync(id, version, HttpContext.RequestAborted);
        return NoContent();
    }
}
