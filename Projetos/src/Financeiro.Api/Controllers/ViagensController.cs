using Financeiro.Application.Catalogs;
using Financeiro.Application.Travel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/viagens")]
public sealed class ViagensController(ITravelService travel) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<TravelBudgetEntry>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        Ok(await travel.ListBudgetsAsync(new(page, pageSize), HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TravelBudgetEntry>> Get(Guid id) => Ok(await travel.GetBudgetAsync(id, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<TravelBudgetEntry>> Create(TravelBudgetInput input)
    {
        var result = await travel.SaveBudgetAsync(null, input, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TravelBudgetEntry>> Update(Guid id, TravelBudgetInput input) =>
        Ok(await travel.SaveBudgetAsync(id, input, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}/itens")]
    public async Task<ActionResult<PageResult<TravelItemEntry>>> ListItems(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        Ok(await travel.ListItemsAsync(id, new(page, pageSize), HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost("{id:guid}/itens")]
    public async Task<ActionResult<TravelItemEntry>> CreateItem(Guid id, TravelItemInput input)
    {
        var result = await travel.SaveItemAsync(null, input with { TravelId = id }, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}/itens/{itemId:guid}")]
    public async Task<ActionResult<TravelItemEntry>> UpdateItem(Guid id, Guid itemId, TravelItemInput input) =>
        Ok(await travel.SaveItemAsync(itemId, input with { TravelId = id }, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}/itens/{itemId:guid}/conversoes")]
    public async Task<ActionResult<PageResult<ItemConversionEntry>>> ListConversions(Guid id, Guid itemId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25) =>
        Ok(await travel.ListConversionsAsync(itemId, new(page, pageSize), HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost("{id:guid}/itens/{itemId:guid}/conversoes")]
    public async Task<ActionResult<ItemConversionEntry>> CreateConversion(Guid id, Guid itemId, ItemConversionInput input)
    {
        var result = await travel.SaveConversionAsync(null, input with { ItemId = itemId }, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}/itens/{itemId:guid}/conversoes/{conversionId:guid}")]
    public async Task<ActionResult<ItemConversionEntry>> UpdateConversion(Guid id, Guid itemId, Guid conversionId, ItemConversionInput input) =>
        Ok(await travel.SaveConversionAsync(conversionId, input with { ItemId = itemId }, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}/reconciliacao")]
    public async Task<ActionResult<TravelBudgetReconciliation>> Reconciliation(Guid id) =>
        Ok(await travel.GetBudgetReconciliationAsync(id, HttpContext.RequestAborted));
}
