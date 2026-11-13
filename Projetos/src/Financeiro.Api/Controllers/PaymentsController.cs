using Financeiro.Application.Catalogs;
using Financeiro.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/pagamentos")]
public sealed class PaymentsController(IPaymentService payments) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<PaymentEntry>>> List(int page = 1, int pageSize = 25) =>
        Ok(await payments.ListAsync(new(page, pageSize), HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentEntry>> Get(Guid id) => Ok(await payments.GetAsync(id, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<PaymentEntry>> Create(PaymentInput input)
    {
        var result = await payments.CreateDraftAsync(input, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}/confirmar")]
    public async Task<ActionResult<PaymentEntry>> Confirm(Guid id, PaymentConfirmationInput input) =>
        Ok(await payments.ConfirmAsync(id, input, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}/reverter")]
    public async Task<ActionResult<PaymentEntry>> Reverse(Guid id, PaymentReversalInput input) =>
        Ok(await payments.ReverseAsync(id, input, HttpContext.RequestAborted));
}
