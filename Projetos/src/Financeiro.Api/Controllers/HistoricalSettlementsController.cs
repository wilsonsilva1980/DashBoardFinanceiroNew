using Financeiro.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/titulos/{titleId:guid}/liquidacoes-historicas")]
public sealed class HistoricalSettlementsController(IHistoricalSettlementService settlements) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HistoricalSettlementEntry>>> List(Guid titleId) =>
        Ok(await settlements.ListAsync(titleId, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<HistoricalSettlementEntry>> Create(Guid titleId, HistoricalSettlementInput input)
    {
        var result = await settlements.CreateAsync(titleId, input, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost("{settlementId:guid}/revogar")]
    public async Task<ActionResult<HistoricalSettlementEntry>> Revoke(Guid titleId, Guid settlementId, HistoricalSettlementRevocationInput input) =>
        Ok(await settlements.RevokeAsync(titleId, settlementId, input, HttpContext.RequestAborted));
}
