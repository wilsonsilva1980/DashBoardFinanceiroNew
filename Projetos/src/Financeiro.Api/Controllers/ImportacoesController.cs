using Financeiro.Application.Importacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Financeiro.Api.Controllers;

[ApiController]
[Route("api/v1/importacoes")]
public sealed class ImportacoesController(IImportService imports) : ControllerBase
{
    [Authorize(Policy = "importacoes.executar")]
    [HttpPost]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ImportAnalysisResult>> Upload(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new ImportValidationException("Envie um arquivo .xlsx.");
        await using var stream = file.OpenReadStream();
        var result = await imports.AnalyzeAsync(stream, file.FileName, file.ContentType, cancellationToken);
        return Created($"{Request.Path}/{result.BatchId}", result);
    }

    [Authorize(Policy = "importacoes.visualizar")]
    [HttpGet("{batchId:guid}")]
    public Task<ImportBatchEntry> Get(Guid batchId, CancellationToken cancellationToken) =>
        imports.GetAsync(batchId, cancellationToken);

    [Authorize(Policy = "importacoes.visualizar")]
    [HttpGet("{batchId:guid}/registros")]
    public Task<Financeiro.Application.Catalogs.PageResult<ImportRowEntry>> Rows(
        Guid batchId,
        [FromQuery] ImportRowFilter filter,
        CancellationToken cancellationToken) =>
        imports.ListRowsAsync(batchId, filter, cancellationToken);

    [Authorize(Policy = "importacoes.visualizar")]
    [HttpGet("{batchId:guid}/pendencias")]
    public Task<ImportPendingSummary> Pending(Guid batchId, CancellationToken cancellationToken) =>
        imports.PendingAsync(batchId, cancellationToken);

    [Authorize(Policy = "importacoes.visualizar")]
    [HttpGet("{batchId:guid}/relatorio")]
    public async Task<IActionResult> Report(Guid batchId, [FromQuery] ImportRowFilter filter, CancellationToken cancellationToken)
    {
        var content = await imports.ExportCsvAsync(batchId, filter, cancellationToken);
        return File(content, "text/csv; charset=utf-8", $"importacao-{batchId:N}.csv");
    }

    [Authorize(Policy = "importacoes.executar")]
    [HttpPost("{batchId:guid}/registros/{rowId:guid}/decisao")]
    public Task<ImportRowEntry> Decide(
        Guid batchId,
        Guid rowId,
        ImportDecisionInput input,
        CancellationToken cancellationToken) =>
        imports.DecideAsync(batchId, rowId, input, cancellationToken);

    [Authorize(Policy = "importacoes.executar")]
    [HttpPost("{batchId:guid}/confirmar")]
    public Task<ImportConfirmationResult> Confirm(
        Guid batchId,
        [FromQuery] long version,
        CancellationToken cancellationToken) =>
        imports.ConfirmAsync(batchId, version, cancellationToken);

    [Authorize(Policy = "importacoes.executar")]
    [HttpPost("{batchId:guid}/cancelar")]
    public async Task<IActionResult> Cancel(Guid batchId, [FromQuery] long version, CancellationToken cancellationToken)
    {
        await imports.CancelAsync(batchId, version, cancellationToken);
        return NoContent();
    }
}
