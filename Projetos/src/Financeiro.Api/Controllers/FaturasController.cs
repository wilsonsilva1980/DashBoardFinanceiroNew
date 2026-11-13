using Financeiro.Application.Catalogs;
using Financeiro.Application.Obligations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/faturas")]
public sealed class FaturasController(IObligationService obligations) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<CardInvoiceEntry>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] Guid? cardId = null) =>
        Ok(await obligations.InvoicesAsync(new(page, pageSize), cardId, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CardInvoiceEntry>> Get(Guid id) => Ok(await obligations.GetInvoiceAsync(id, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<CardInvoiceEntry>> Create(CardInvoiceInput input)
    {
        var result = await obligations.SaveInvoiceAsync(null, input, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CardInvoiceEntry>> Update(Guid id, CardInvoiceInput input) => Ok(await obligations.SaveInvoiceAsync(id, input, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost("{id:guid}/itens")]
    public async Task<ActionResult<InvoicePurchaseEntry>> SavePurchase(Guid id, InvoicePurchaseInput input) =>
        Ok(await obligations.SavePurchaseAsync(null, input with { InvoiceId = id }, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}/itens/{purchaseId:guid}")]
    public async Task<ActionResult<InvoicePurchaseEntry>> UpdatePurchase(Guid id, Guid purchaseId, InvoicePurchaseInput input) =>
        Ok(await obligations.SavePurchaseAsync(purchaseId, input with { InvoiceId = id }, HttpContext.RequestAborted));
}
