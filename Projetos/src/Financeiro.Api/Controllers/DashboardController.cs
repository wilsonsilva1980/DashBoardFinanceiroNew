using System.ComponentModel.DataAnnotations;
using Financeiro.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
public sealed class DashboardController(IDashboardQueries dashboard) : ControllerBase
{
    [Authorize(Policy = "dashboard.visualizar")]
    [HttpGet("fluxo-caixa")]
    [ProducesResponseType(typeof(CashFlowSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Resumo de fluxo de caixa por período")]
    [EndpointDescription("Retorna saídas de caixa datadas no período. Posições de conta não são tratadas como movimentação; renda permanece desconhecida quando não há livro de receitas.")]
    public async Task<ActionResult<CashFlowSummary>> CashFlow([FromQuery, Required] DateOnly from, [FromQuery, Required] DateOnly until, [FromQuery, Required] string currency) =>
        Ok(await dashboard.CashFlowAsync(from, until, currency, HttpContext.RequestAborted));

    [Authorize(Policy = "dashboard.visualizar")]
    [HttpGet("resumo")]
    [ProducesResponseType(typeof(DashboardSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [EndpointSummary("Resumo financeiro do espaço ativo")]
    [EndpointDescription("Retorna totais, evolução mensal, quebras por categoria/responsável/vencimento e posições, distinguindo estados efetivo/previsão/em aberto/parcial/pago/vencido. Indicadores de reconciliação são apresentados separadamente e não compõem totais normais.")]
    public async Task<ActionResult<DashboardSummary>> Summary([FromQuery, Required] DateOnly from, [FromQuery, Required] DateOnly until, [FromQuery, Required] string currency) =>
        Ok(await dashboard.SummaryAsync(from, until, currency, HttpContext.RequestAborted));
}
