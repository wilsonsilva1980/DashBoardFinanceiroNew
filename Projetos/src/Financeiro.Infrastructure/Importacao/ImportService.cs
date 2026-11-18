using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Financeiro.Application.Access;
using Financeiro.Application.Catalogs;
using Financeiro.Application.Importacao;
using Financeiro.Domain.Entities;
using Financeiro.Infrastructure.Access;
using Financeiro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Financeiro.Domain;

namespace Financeiro.Infrastructure.Importacao;

public sealed class ImportService(
    FinanceiroDbContext db,
    FinancialScope scope,
    IRequestActor actor,
    PermissionEvaluator permissions,
    ISpreadsheetReader reader,
    IAuditWriter auditWriter) : IImportService
{
    private const string ParserVersion = "2026.11.1";

    public async Task<ImportBatchEntry> GetAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        await DemandViewAsync(cancellationToken);
        var batch = await db.ImportBatches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken)
            ?? throw new CatalogNotFoundException();
        return new(batch.Id, batch.OriginalFileName, batch.SizeBytes, batch.ParserVersion, batch.State,
            batch.StatisticsJson, batch.Version, batch.CreatedAt);
    }

    public async Task<ImportAnalysisResult> AnalyzeAsync(
        Stream workbook,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken = default)
    {
        await DemandAsync(cancellationToken);
        var options = new SpreadsheetOptions();
        await using var validated = await SpreadsheetInputValidator.ValidateAsync(
            workbook,
            fileName,
            contentType,
            options,
            cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(validated));
        validated.Position = 0;
        var existing = await db.ImportBatches.SingleOrDefaultAsync(x => x.ContentHash == hash, cancellationToken);
        if (existing is not null)
            return await ResultAsync(existing.Id, existing.State, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var batch = new ImportBatch
        {
            TenantId = scope.TenantId!.Value,
            SpaceId = scope.SpaceId!.Value,
            OriginalFileName = Path.GetFileName(fileName).Trim(),
            ContentHash = hash,
            SizeBytes = validated.Length,
            ParserVersion = ParserVersion,
            State = ImportState.Received,
            CreatedBy = actor.UserId
        };
        db.ImportBatches.Add(batch);

        try
        {
            validated.Position = 0;
            var sheets = await reader.ReadAsync(validated, cancellationToken);
            var parsedRows = sheets.SelectMany(SpreadsheetRowAnalyzer.Analyze).ToArray();
            var issues = parsedRows.SelectMany(SpreadsheetIssueDetector.Detect).ToArray();
            var duplicateIssues = SpreadsheetIssueDetector.DetectDuplicates(parsedRows);
            issues = issues.Concat(duplicateIssues).Take(options.MaxIssues).ToArray();
            var issueByRow = parsedRows.ToDictionary(
                row => (row.SheetName, row.RowNumber),
                row => SpreadsheetIssueDetector.Detect(row));

            var sourceRecords = new List<ImportSourceRecord>();
            var annotations = new List<ImportAnnotation>();
            foreach (var sheet in sheets)
            {
                foreach (var row in sheet.Rows)
                {
                    var parsed = parsedRows.FirstOrDefault(x => x.SheetName == sheet.Name && x.RowNumber == row.RowNumber);
                    if (parsed is null) continue;
                    var rowIssues = issueByRow.GetValueOrDefault((sheet.Name, row.RowNumber)) ?? [];
                    var severity = rowIssues.Count == 0 ? ImportIssueSeverity.Info : rowIssues.Max(x => x.Severity);
                    var firstIssue = rowIssues.FirstOrDefault();
                    var source = new ImportSourceRecord
                    {
                        TenantId = batch.TenantId,
                        SpaceId = batch.SpaceId,
                        ImportBatchId = batch.Id,
                        SheetName = sheet.Name,
                        NormalizedSheetName = SpreadsheetTextNormalizer.Normalize(sheet.Name),
                        SourceType = parsed.SourceType,
                        RowNumber = row.RowNumber,
                        StartColumn = parsed.StartColumn,
                        CellReference = parsed.CellReference,
                        Classification = parsed.Classification,
                        RawContentJson = JsonSerializer.Serialize(new
                        {
                            values = parsed.Values,
                            cells = row.Cells
                        }),
                        DerivedValuesJson = JsonSerializer.Serialize(parsed.DerivedValues),
                        IssueCode = firstIssue?.Code,
                        IssueMessage = firstIssue?.Message,
                        Severity = severity,
                        Decision = ImportDecision.Pending
                    };
                    sourceRecords.Add(source);
                    foreach (var cell in row.Cells.Where(cell => cell.CommentText is not null))
                    {
                        annotations.Add(new ImportAnnotation
                        {
                            TenantId = batch.TenantId,
                            SpaceId = batch.SpaceId,
                            ImportBatchId = batch.Id,
                            SheetName = sheet.Name,
                            CellReference = cell.Address,
                            SourceAuthor = cell.CommentAuthor,
                            Text = cell.CommentText!
                        });
                    }
                }

            }

            batch.State = issues.Length == 0 ? ImportState.Analyzed : ImportState.AwaitingReview;
            batch.StatisticsJson = JsonSerializer.Serialize(new
            {
                totalRows = parsedRows.Length,
                eligibleRows = parsedRows.Count(x => x.Classification == ImportRecordClass.Record),
                errors = issues.Count(x => x.Severity == ImportIssueSeverity.Error),
                warnings = issues.Count(x => x.Severity == ImportIssueSeverity.Warning),
                info = issues.Count(x => x.Severity == ImportIssueSeverity.Info)
            });
            db.ImportSourceRecords.AddRange(sourceRecords);
            db.ImportAnnotations.AddRange(annotations);
            await db.SaveChangesAsync(cancellationToken);
            return new(
                batch.Id,
                batch.State,
                parsedRows.Length,
                parsedRows.Count(x => x.Classification == ImportRecordClass.Record),
                issues.Count(x => x.Severity == ImportIssueSeverity.Error),
                issues.Count(x => x.Severity == ImportIssueSeverity.Warning),
                issues.Count(x => x.Severity == ImportIssueSeverity.Info));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            batch.State = ImportState.Failed;
            batch.FailureReason = exception.Message[..Math.Min(exception.Message.Length, 1000)];
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PageResult<ImportRowEntry>> ListRowsAsync(
        Guid batchId,
        ImportRowFilter filter,
        CancellationToken cancellationToken = default)
    {
        await DemandViewAsync(cancellationToken);
        var offset = filter.Offset();
        var query = db.ImportSourceRecords.AsNoTracking().Where(x => x.ImportBatchId == batchId);
        if (!string.IsNullOrWhiteSpace(filter.SheetName))
            query = query.Where(x => x.SheetName == filter.SheetName);
        if (filter.Severity.HasValue)
            query = query.Where(x => x.Severity == filter.Severity.Value);
        if (filter.Decision.HasValue)
            query = query.Where(x => x.Decision == filter.Decision.Value);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.SheetName).ThenBy(x => x.RowNumber).ThenBy(x => x.Id)
            .Skip(offset).Take(filter.PageSize).ToArrayAsync(cancellationToken);
        return new(rows.Select(x => new ImportRowEntry(
            x.Id,
            x.SheetName,
            x.RowNumber,
            x.CellReference,
            x.SourceType,
            x.Classification,
            x.DerivedValuesJson,
            x.IssueCode,
            x.IssueMessage,
            x.Severity,
            x.Decision,
            x.Version)).ToArray(), total, filter.Page, filter.PageSize);
    }

    public async Task<ImportPendingSummary> PendingAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        await DemandViewAsync(cancellationToken);
        var rows = await db.ImportSourceRecords.AsNoTracking()
            .Where(x => x.ImportBatchId == batchId)
            .ToArrayAsync(cancellationToken);
        return new(
            rows.Count(x => x.Severity == ImportIssueSeverity.Error),
            rows.Count(x => x.Severity == ImportIssueSeverity.Warning),
            rows.Count(x => x.Decision == ImportDecision.Pending));
    }

    public async Task<byte[]> ExportCsvAsync(
        Guid batchId,
        ImportRowFilter filter,
        CancellationToken cancellationToken = default)
    {
        await DemandViewAsync(cancellationToken);
        var query = db.ImportSourceRecords.AsNoTracking().Where(x => x.ImportBatchId == batchId);
        if (!string.IsNullOrWhiteSpace(filter.SheetName))
            query = query.Where(x => x.SheetName == filter.SheetName);
        if (filter.Severity.HasValue)
            query = query.Where(x => x.Severity == filter.Severity.Value);
        if (filter.Decision.HasValue)
            query = query.Where(x => x.Decision == filter.Decision.Value);
        var rows = await query.OrderBy(x => x.SheetName).ThenBy(x => x.RowNumber).ThenBy(x => x.Id).ToArrayAsync(cancellationToken);
        var csv = new StringBuilder("aba;linha;celula;tipo;classificacao;valores_derivados;codigo_issue;mensagem_issue;severidade;decisao;versao\n");
        foreach (var row in rows)
        {
            csv.AppendJoin(';',
                Csv(row.SheetName),
                row.RowNumber,
                Csv(row.CellReference),
                Csv(row.SourceType.ToString()),
                Csv(row.Classification.ToString()),
                Csv(row.DerivedValuesJson),
                Csv(row.IssueCode),
                Csv(row.IssueMessage),
                Csv(row.Severity.ToString()),
                Csv(row.Decision.ToString()),
                row.Version);
            csv.Append('\n');
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<ImportRowEntry> DecideAsync(
        Guid batchId,
        Guid rowId,
        ImportDecisionInput input,
        CancellationToken cancellationToken = default)
    {
        await DemandAsync(cancellationToken);
        if (input.Decision is not (ImportDecision.Approved or ImportDecision.Rejected or ImportDecision.Ignored))
            throw new ImportValidationException("A decisão de importação é inválida.");
        var batch = await db.ImportBatches.SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken)
            ?? throw new CatalogNotFoundException();
        if (batch.State is ImportState.Confirmed or ImportState.Cancelled)
            throw new ImportRuleException("O lote não aceita alterações após seu encerramento.");
        var row = await db.ImportSourceRecords.SingleOrDefaultAsync(
            x => x.Id == rowId && x.ImportBatchId == batchId,
            cancellationToken) ?? throw new CatalogNotFoundException();
        if (row.Version != input.Version)
            throw new RequestConflictException("O registro foi alterado. Recarregue a prévia antes de decidir.");
        row.Decision = input.Decision;
        row.DecidedBy = actor.UserId;
        row.DecidedAt = DateTimeOffset.UtcNow;
        auditWriter.Record("importacao.decisao", "ImportSourceRecord", row.Id, new
        {
            batchId,
            decision = input.Decision.ToString(),
            row.SheetName,
            row.RowNumber
        });
        await db.SaveChangesAsync(cancellationToken);
        return new(
            row.Id,
            row.SheetName,
            row.RowNumber,
            row.CellReference,
            row.SourceType,
            row.Classification,
            row.DerivedValuesJson,
            row.IssueCode,
            row.IssueMessage,
            row.Severity,
            row.Decision,
            row.Version);
    }

    public async Task<ImportConfirmationResult> ConfirmAsync(
        Guid batchId,
        long version,
        CancellationToken cancellationToken = default)
    {
        await DemandAsync(cancellationToken);
        var strategy = db.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                var batch = await db.ImportBatches.SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken)
                    ?? throw new CatalogNotFoundException();
                if (batch.Version != version)
                    throw new RequestConflictException("O lote foi alterado. Recarregue antes de confirmar.");
                if (batch.State is ImportState.Confirmed or ImportState.Cancelled)
                    throw new ImportRuleException("O lote não aceita confirmação neste estado.");

                var rows = await db.ImportSourceRecords
                    .Where(x => x.ImportBatchId == batchId && x.Decision == ImportDecision.Approved)
                    .OrderBy(x => x.SheetName).ThenBy(x => x.RowNumber).ThenBy(x => x.Id)
                    .ToArrayAsync(cancellationToken);
                if (rows.Any(x => x.Severity == ImportIssueSeverity.Error))
                    throw new ImportRuleException("Corrija ou rejeite os registros com erro antes de confirmar.");

                var created = 0;
                var reused = 0;
                var rejected = 0;
                var failed = 0;
                foreach (var row in rows)
                {
                    try
                    {
                        var outcome = await MapApprovedRowAsync(batch, row, cancellationToken);
                        created += outcome.Created;
                        reused += outcome.Reused;
                        rejected += outcome.Rejected;
                    }
                    catch (ImportValidationException)
                    {
                        rejected++;
                    }
                    catch
                    {
                        failed++;
                        throw;
                    }
                }

                batch.State = ImportState.Confirmed;
                batch.ConfirmedBy = actor.UserId;
                batch.ConfirmedAt = DateTimeOffset.UtcNow;
                batch.StatisticsJson = JsonSerializer.Serialize(new { confirmed = true, created, reused, rejected, failed });
                auditWriter.Record("importacao.confirmada", "ImportBatch", batch.Id, new
                {
                    created,
                    reused,
                    rejected,
                    failed,
                    approvedRows = rows.Length
                });
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new ImportConfirmationResult(batch.Id, batch.State, created, reused, rejected, failed);
            });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            db.ChangeTracker.Clear();
            var failedBatch = await db.ImportBatches.SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken);
            if (failedBatch is not null && failedBatch.State is not ImportState.Confirmed and not ImportState.Cancelled)
            {
                failedBatch.State = ImportState.Failed;
                failedBatch.FailureReason = exception.Message[..Math.Min(exception.Message.Length, 1000)];
                await db.SaveChangesAsync(cancellationToken);
            }
            throw;
        }
    }

    public async Task CancelAsync(Guid batchId, long version, CancellationToken cancellationToken = default)
    {
        await DemandAsync(cancellationToken);
        var batch = await db.ImportBatches.SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken)
            ?? throw new CatalogNotFoundException();
        if (batch.Version != version)
            throw new RequestConflictException("O lote foi alterado. Recarregue antes de cancelar.");
        if (batch.State is ImportState.Confirmed or ImportState.Cancelled)
            throw new ImportRuleException("O lote não aceita cancelamento neste estado.");
        batch.State = ImportState.Cancelled;
        auditWriter.Record("importacao.cancelada", "ImportBatch", batch.Id, new { batchId });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<MapOutcome> MapApprovedRowAsync(
        ImportBatch batch,
        ImportSourceRecord row,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<StoredRowPayload>(row.RawContentJson) ?? new();
        var values = payload.Values ?? new Dictionary<string, string?>(StringComparer.Ordinal);
        var derived = JsonSerializer.Deserialize<Dictionary<string, string?>>(row.DerivedValuesJson)
            ?? new Dictionary<string, string?>(StringComparer.Ordinal);
        var description = First(values, "divida", "descricao", "descrição", "credor", "nome", "titulo", "estabelecimento")
            ?? $"{row.SheetName} linha {row.RowNumber}";
        var currency = NormalizeCurrency(First(values, "moeda", "currency"));
        var date = ParseDate(First(derived, "competencia") ?? First(values, "competencia", "mes", "data", "datavencimento"));
        var amount = ParseDecimal(First(derived, "valor_original") ?? First(values, "valortotal", "valorparcela", "valor", "total"));
        var installment = ParseInt(First(derived, "numero_parcela") ?? First(values, "numeroparcela", "parcela", "nparcela"));
        var person = await FindPersonAsync(First(derived, "responsavel") ?? First(values, "responsavel", "quem"), cancellationToken);
        var category = await FindCategoryAsync(First(values, "categoria", "category"), cancellationToken);
        var occurrenceKey = BuildOccurrenceKey(row, date, installment, description);
        var forecast = row.SourceType is ImportSourceType.Backup or ImportSourceType.Scenario or ImportSourceType.Anticipation;

        if (row.SourceType == ImportSourceType.Travel)
            return await MapTravelAsync(batch, row, description, currency, amount, date, category?.Id, person?.Id, occurrenceKey, cancellationToken);

        var obligation = await db.Obligations.SingleOrDefaultAsync(x =>
            x.Description == description && x.Currency == currency && x.Kind == (installment.HasValue ? "Installment" : "Single"), cancellationToken);
        var obligationCreated = obligation is null;
        obligation ??= New<Obligation>();
        obligation.CategoryId = category?.Id;
        obligation.ResponsibleId = person?.Id;
        obligation.Description = description.Trim();
        obligation.Creditor = First(values, "credor");
        obligation.Kind = installment.HasValue ? "Installment" : "Single";
        obligation.PurchaseDate = date;
        obligation.PurchaseDateContext = date is null ? First(values, "data", "competencia") : null;
        obligation.DueDay = date?.Day;
        obligation.InstallmentCount = ParseInt(First(values, "quantidadeparcelas", "parcelas"));
        obligation.Currency = currency;
        obligation.Active = true;
        if (obligationCreated) db.Obligations.Add(obligation);
        await db.SaveChangesAsync(cancellationToken);

        if (row.SourceType == ImportSourceType.Recurrence &&
            !await db.Recurrences.AnyAsync(x => x.ObligationId == obligation.Id, cancellationToken))
        {
            var recurrence = New<Recurrence>();
            recurrence.ObligationId = obligation.Id;
            recurrence.AnchorDate = date;
            recurrence.NominalDay = date?.Day ?? 1;
            recurrence.Active = true;
            db.Recurrences.Add(recurrence);
            await db.SaveChangesAsync(cancellationToken);
            if (amount is decimal recurrenceAmount)
                db.RecurrencePrices.Add(new RecurrencePrice
                {
                    TenantId = batch.TenantId,
                    SpaceId = batch.SpaceId,
                    CreatedBy = actor.UserId,
                    RecurrenceId = recurrence.Id,
                    StartDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    CycleAmount = recurrenceAmount,
                    Currency = currency,
                    IntervalMonths = Math.Max(ParseInt(First(values, "intervalo", "meses")) ?? 1, 1)
                });
        }

        var title = await db.PayableTitles.SingleOrDefaultAsync(x =>
            x.ObligationId == obligation.Id && x.OccurrenceKey == occurrenceKey, cancellationToken);
        if (title is null && date is DateOnly sourceDate)
        {
            title = await db.PayableTitles
                .Where(x => x.ObligationId == obligation.Id && x.Period == sourceDate)
                .Where(x => row.SourceType == ImportSourceType.Annual
                    ? x.InstallmentNumber == null
                    : installment.HasValue && x.InstallmentNumber == installment)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
        var titleCreated = title is null;
        title ??= New<PayableTitle>();
        title.ObligationId = obligation.Id;
        title.ResponsibleId = person?.Id ?? title.ResponsibleId;
        title.Period = date ?? title.Period;
        title.DueDate = date ?? title.DueDate;
        title.InstallmentNumber = installment ?? title.InstallmentNumber;
        title.CardId = await FindCardIdAsync(First(values, "cartao", "card"), cancellationToken) ?? title.CardId;
        title.OccurrenceKey = occurrenceKey;
        title.OriginalAmount = amount ?? title.OriginalAmount;
        if (forecast)
            title.State = TitleState.Forecast;
        else if (titleCreated || title.State == TitleState.Forecast)
            title.State = TitleState.Effective;
        if (titleCreated) db.PayableTitles.Add(title);
        await db.SaveChangesAsync(cancellationToken);

        if (!await db.ImportLinks.AnyAsync(x => x.ImportBatchId == batch.Id && x.SourceRecordId == row.Id, cancellationToken))
            db.ImportLinks.Add(new ImportLink
            {
                TenantId = batch.TenantId,
                SpaceId = batch.SpaceId,
                CreatedBy = actor.UserId,
                ImportBatchId = batch.Id,
                SourceRecordId = row.Id,
                EntityType = "PayableTitle",
                EntityId = title.Id,
                Role = forecast ? "forecast" : "effective",
                OccurrenceKey = occurrenceKey
            });

        var status = First(derived, "status_origem") ?? First(values, "status", "pagamento", "pago");
        if (!forecast && IsPaid(status) && !await db.HistoricalSettlements.AnyAsync(x =>
                x.TitleId == title.Id && x.PreviousReference == occurrenceKey, cancellationToken))
        {
            db.HistoricalSettlements.Add(new HistoricalSettlement
            {
                TenantId = batch.TenantId,
                SpaceId = batch.SpaceId,
                CreatedBy = actor.UserId,
                TitleId = title.Id,
                ApprovedBy = actor.UserId ?? throw new AccessDeniedException("Usuário autenticado obrigatório."),
                Amount = amount ?? 0m,
                Justification = "Marcador de pagamento importado; data e meio de pagamento permanecem desconhecidos.",
                PreviousReference = occurrenceKey,
                SourceStatus = status
            });
        }

        if (person is not null && amount is decimal planningAmount &&
            !await db.PlanningEntrys.AnyAsync(x => x.TitleId == title.Id && x.PersonId == person.Id, cancellationToken))
            db.PlanningEntrys.Add(new PlanningEntry
            {
                TenantId = batch.TenantId,
                SpaceId = batch.SpaceId,
                CreatedBy = actor.UserId,
                TitleId = title.Id,
                PersonId = person.Id,
                Amount = planningAmount
            });

        var account = await FindAccountAsync(First(values, "conta", "account", "banco"), cancellationToken);
        var accountBalance = ParseDecimal(First(values, "saldo", "saldoatual", "balance"));
        if (account is not null && accountBalance is decimal balance &&
            !await db.AccountPositions.AnyAsync(x => x.AccountId == account.Id &&
                x.PositionDate == (date ?? DateOnly.FromDateTime(DateTime.UtcNow)), cancellationToken))
            db.AccountPositions.Add(new AccountPosition
            {
                TenantId = batch.TenantId,
                SpaceId = batch.SpaceId,
                CreatedBy = actor.UserId,
                AccountId = account.Id,
                PositionDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow),
                Balance = balance
            });

        row.PrevailingSource = forecast
            ? "forecast"
            : row.SourceType == ImportSourceType.Annual ? "installments-or-annual" : row.SourceType.ToString();
        row.IssueCode ??= forecast ? "FORECAST_SOURCE" : null;
        await db.SaveChangesAsync(cancellationToken);
        return new(obligationCreated || titleCreated ? 1 : 0, obligationCreated || titleCreated ? 0 : 1, 0);
    }

    private async Task<MapOutcome> MapTravelAsync(
        ImportBatch batch,
        ImportSourceRecord row,
        string description,
        string currency,
        decimal? amount,
        DateOnly? date,
        Guid? categoryId,
        Guid? responsibleId,
        string occurrenceKey,
        CancellationToken cancellationToken)
    {
        var budget = await db.TravelBudgets.SingleOrDefaultAsync(x => x.Name == row.SheetName, cancellationToken);
        if (budget is null)
        {
            budget = New<TravelBudget>();
            budget.Name = row.SheetName;
            budget.StartDate = date;
            db.TravelBudgets.Add(budget);
        }
        await db.SaveChangesAsync(cancellationToken);
        var item = await db.TravelItems.SingleOrDefaultAsync(x =>
            x.TravelId == budget.Id && x.Description == description, cancellationToken);
        var created = item is null;
        item ??= New<TravelItem>();
        item.TravelId = budget.Id;
        item.CategoryId = categoryId;
        item.ResponsibleId = responsibleId;
        item.Description = description;
        item.State = row.SourceType is ImportSourceType.Scenario or ImportSourceType.Anticipation ? "Forecast" : "Budget";
        item.Currency = currency == "UNK" ? null : currency;
        item.BaseAmount = amount;
        if (created) db.TravelItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        if (!await db.ImportLinks.AnyAsync(x => x.ImportBatchId == batch.Id && x.SourceRecordId == row.Id, cancellationToken))
            db.ImportLinks.Add(new ImportLink
            {
                TenantId = batch.TenantId,
                SpaceId = batch.SpaceId,
                CreatedBy = actor.UserId,
                ImportBatchId = batch.Id,
                SourceRecordId = row.Id,
                EntityType = "TravelItem",
                EntityId = item.Id,
                Role = item.State == "Forecast" ? "forecast" : "effective",
                OccurrenceKey = occurrenceKey
            });
        await db.SaveChangesAsync(cancellationToken);
        return new(created ? 1 : 0, created ? 0 : 1, 0);
    }

    private async Task<Person?> FindPersonAsync(string? name, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(name) ? null : await db.Persons.SingleOrDefaultAsync(x => x.Name == name.Trim(), cancellationToken);

    private async Task<Category?> FindCategoryAsync(string? name, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(name) ? null : await db.Categorys.SingleOrDefaultAsync(x => x.Name == name.Trim(), cancellationToken);

    private async Task<Guid?> FindCardIdAsync(string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return (await db.CreditCards.SingleOrDefaultAsync(x => x.Name == name.Trim(), cancellationToken))?.Id;
    }

    private async Task<BankAccount?> FindAccountAsync(string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return await db.BankAccounts.SingleOrDefaultAsync(x =>
            x.Number == name.Trim() || x.Type == name.Trim(), cancellationToken);
    }

    private T New<T>() where T : ScopedEntity, new() => new()
    {
        TenantId = scope.TenantId!.Value,
        SpaceId = scope.SpaceId!.Value,
        CreatedBy = actor.UserId
    };

    private static string BuildOccurrenceKey(ImportSourceRecord row, DateOnly? date, int? installment, string description) =>
        $"{row.SourceType}:{row.SheetName}:{row.RowNumber}:{date:yyyy-MM-dd}:{installment}:{SpreadsheetTextNormalizer.Normalize(description)}";

    private static string? First(IReadOnlyDictionary<string, string?> values, params string[] keys) =>
        keys.Select(key => values.TryGetValue(key, out var value) ? value : null)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string NormalizeCurrency(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "UNK";
        var normalized = SpreadsheetTextNormalizer.Normalize(value);
        return normalized switch
        {
            "real" or "brl" or "r$" => "BRL",
            "dolar" or "usd" or "$" => "USD",
            "peso" or "clp" => "CLP",
            _ when normalized.Length == 3 => normalized.ToUpperInvariant(),
            _ => "UNK"
        };
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-BR"), out var pt)) return pt;
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant) ? invariant : null;
    }

    private static int? ParseInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        foreach (var format in new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "MM/yyyy", "M/yyyy" })
            if (DateOnly.TryParseExact(value.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return format.Contains("M/yyyy", StringComparison.Ordinal) ? new DateOnly(date.Year, date.Month, 1) : date;
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial is > 1 and < 100000)
            return DateOnly.FromDateTime(DateTime.FromOADate(serial));
        return null;
    }

    private static bool IsPaid(string? value)
    {
        var normalized = SpreadsheetTextNormalizer.Normalize(value);
        return normalized is "p" or "pago" or "paga" or "quitado" or "liquidado";
    }

    private sealed record StoredRowPayload(
        Dictionary<string, string?>? Values = null,
        IReadOnlyList<RawCell>? Cells = null);

    private sealed record MapOutcome(int Created, int Reused, int Rejected);

    private async Task DemandAsync(CancellationToken cancellationToken)
    {
        if (!scope.IsAuthorized || actor.UserId is null ||
            !await permissions.HasAsync("importacoes.executar", cancellationToken))
            throw new AccessDeniedException("Permissão de importação necessária no espaço ativo.");
    }

    private async Task DemandViewAsync(CancellationToken cancellationToken)
    {
        if (!scope.IsAuthorized || actor.UserId is null ||
            !await permissions.HasAsync("importacoes.visualizar", cancellationToken))
            throw new AccessDeniedException("Permissão de visualização de importações necessária no espaço ativo.");
    }

    private static string Csv(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty :
        $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private async Task<ImportAnalysisResult> ResultAsync(Guid batchId, ImportState state, CancellationToken cancellationToken)
    {
        var batch = await db.ImportBatches.AsNoTracking().SingleAsync(x => x.Id == batchId, cancellationToken);
        var records = await db.ImportSourceRecords.AsNoTracking().Where(x => x.ImportBatchId == batchId).ToArrayAsync(cancellationToken);
        return new(
            batch.Id,
            state,
            records.Length,
            records.Count(x => x.Classification == ImportRecordClass.Record),
            records.Count(x => x.Severity == ImportIssueSeverity.Error),
            records.Count(x => x.Severity == ImportIssueSeverity.Warning),
            records.Count(x => x.Severity == ImportIssueSeverity.Info));
    }
}
