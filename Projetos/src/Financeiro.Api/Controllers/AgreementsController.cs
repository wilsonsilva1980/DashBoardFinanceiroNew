using Financeiro.Application.Catalogs;
using Financeiro.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/acordos")]
public sealed class AgreementsController(IAgreementService agreements) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<AgreementEntry>>> List(int page = 1, int pageSize = 25) =>
        Ok(await agreements.ListAsync(new(page, pageSize), HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost("simulacoes")]
    public async Task<ActionResult<AgreementSimulationEntry>> Simulate(AgreementSimulationInput input) =>
        Created($"{Request.Path}", await agreements.SimulateAsync(input, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgreementEntry>> Get(Guid id) => Ok(await agreements.GetAsync(id, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<AgreementEntry>> Create(AgreementInput input)
    {
        var result = await agreements.CreateAsync(input, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}/confirmar")]
    public async Task<ActionResult<AgreementEntry>> Confirm(Guid id, AgreementConfirmationInput input) =>
        Ok(await agreements.ConfirmAsync(id, input, HttpContext.RequestAborted));
}
