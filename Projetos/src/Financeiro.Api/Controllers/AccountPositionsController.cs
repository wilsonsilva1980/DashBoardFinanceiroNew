using Financeiro.Application.Accounts;
using Financeiro.Application.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/posicoes-conta")]
public sealed class AccountPositionsController(IAccountPositionService positions) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<AccountPositionEntry>>> List(Guid? accountId = null, int page = 1, int pageSize = 25) =>
        Ok(await positions.ListAsync(new(page, pageSize), accountId, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<AccountPositionEntry>> Create(AccountPositionInput input)
    {
        var result = await positions.CreateAsync(input, HttpContext.RequestAborted);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
