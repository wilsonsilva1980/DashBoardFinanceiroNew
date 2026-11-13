using Financeiro.Application.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/contas")]
public sealed class BankAccountsController(ICatalogService catalogs) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<BankAccountEntry>>> List(int page = 1, int pageSize = 25, string? search = null, bool includeInactive = true) =>
        Ok(await catalogs.AccountsAsync(new(page, pageSize, search, includeInactive), HttpContext.RequestAborted));
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BankAccountEntry>> Get(Guid id) => Ok(await catalogs.AccountAsync(id, HttpContext.RequestAborted));
    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<BankAccountEntry>> Create(BankAccountInput input)
    {
        var result = await catalogs.SaveAccountAsync(null, input, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }
    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BankAccountEntry>> Update(Guid id, BankAccountInput input) => Ok(await catalogs.SaveAccountAsync(id, input, HttpContext.RequestAborted));
    [Authorize(Policy = "cadastros.editar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] long version)
    {
        await catalogs.DeleteAccountAsync(id, version, HttpContext.RequestAborted);
        return NoContent();
    }
}
