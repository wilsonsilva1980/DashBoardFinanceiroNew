using Financeiro.Application.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
public abstract class NamedCatalogController(ICatalogService catalogs, NamedCatalogKind kind) : ControllerBase
{
    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<NamedCatalogEntry>>> List(int page = 1, int pageSize = 25, string? search = null, bool includeInactive = true) =>
        Ok(await catalogs.ListAsync(kind, new(page, pageSize, search, includeInactive), HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.visualizar")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NamedCatalogEntry>> Get(Guid id) => Ok(await catalogs.GetAsync(kind, id, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpPost]
    public async Task<ActionResult<NamedCatalogEntry>> Create(NamedCatalogInput input)
    {
        var result = await catalogs.CreateAsync(kind, input, HttpContext.RequestAborted);
        return Created($"{Request.Path}/{result.Id}", result);
    }

    [Authorize(Policy = "cadastros.editar")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<NamedCatalogEntry>> Update(Guid id, NamedCatalogInput input) => Ok(await catalogs.UpdateAsync(kind, id, input, HttpContext.RequestAborted));

    [Authorize(Policy = "cadastros.editar")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] long version)
    {
        await catalogs.DeleteAsync(kind, id, version, HttpContext.RequestAborted);
        return NoContent();
    }
}

[Route("api/v1/pessoas")]
public sealed class PeopleController(ICatalogService catalogs) : NamedCatalogController(catalogs, NamedCatalogKind.People);
[Route("api/v1/categorias")]
public sealed class CategoriesController(ICatalogService catalogs) : NamedCatalogController(catalogs, NamedCatalogKind.Categories);
[Route("api/v1/instituicoes")]
public sealed class InstitutionsController(ICatalogService catalogs) : NamedCatalogController(catalogs, NamedCatalogKind.Institutions);
[Route("api/v1/bandeiras")]
public sealed class CardBrandsController(ICatalogService catalogs) : NamedCatalogController(catalogs, NamedCatalogKind.CardBrands);
