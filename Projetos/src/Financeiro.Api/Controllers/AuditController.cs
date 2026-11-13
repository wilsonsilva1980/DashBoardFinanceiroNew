using Financeiro.Application.Access;
using Financeiro.Application.Catalogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/auditoria")]
public sealed class AuditController(IAuditQueryService audit) : ControllerBase
{
    [Authorize(Policy = "auditoria.visualizar")]
    [HttpGet]
    public async Task<ActionResult<PageResult<AuditEntry>>> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? until,
        [FromQuery] string? action,
        [FromQuery] string? entity,
        [FromQuery] Guid? entityId,
        [FromQuery] Guid? userId,
        [FromQuery] string? correlationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25) =>
        Ok(await audit.ListAsync(new(from, until, action, entity, entityId, userId, correlationId, page, pageSize), HttpContext.RequestAborted));

    [Authorize(Policy = "auditoria.visualizar")]
    [HttpGet("eventos")]
    public ActionResult<IReadOnlyDictionary<string, string>> Events() => Ok(audit.Events());
}
