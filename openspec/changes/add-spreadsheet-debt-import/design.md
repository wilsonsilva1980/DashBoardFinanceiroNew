# Design

## Context

The existing application is a modular ASP.NET Core/EF Core backend with separate PostgreSQL and SQL Server migration assemblies, a React/Vite frontend, server-derived tenant/space context, cookie authentication, global CSRF protection, and catalog-based permissions/auditing. Existing financial entities already represent obligations, titles, planning, historical settlements, recurring expenses, accounts, cards, travel items, and reconciliation indicators; the import flow must stage source data before invoking those domain services.

The reference workbook has annual matrix sheets, installment blocks with a variable anticipation layout, hidden backup sheets, recurring/cadastro sheets, mixed-currency travel data, comments, cached formulas, and overlapping sources. The parser therefore needs source-aware classification and evidence preservation rather than a positional, formula-evaluating import.

## Goals / Non-Goals

**Goals:**

- Provide a bounded, review-first `.xlsx` import lifecycle: receive, analyze, preview, decide, confirm, report, or cancel.
- Keep all staging and resulting records tenant/space scoped, auditable, idempotent, and equivalent across both providers.
- Reuse existing financial services and state semantics while preserving unknown values and separating forecast/reconciliation data.
- Keep spreadsheet parsing isolated from Domain and Application package dependencies.
- Deliver a usable frontend wizard with accessible review, clear errors, scoped cache invalidation, and resumable analysis polling.

**Non-Goals:**

- Import macros, `.xls` files, bank transactions, or unsupported workbook formats.
- Recalculate formulas, infer payment accounts/dates, or silently resolve ambiguous source data.
- Replace manual financial workflows, create a new financial domain model, or make clients choose a database provider.
- Persist the original workbook after analysis; only hash, staging evidence, annotations, and reports are retained.
- Make future, backup, anticipation, or unresolved travel data effective financial totals by default.

## Decisions

### Staging model before domain confirmation

Add import batch, source record, source link, and source annotation entities. Store raw/derived evidence and decisions in staging, then map only approved records to existing financial services during confirmation. This gives reviewers cell-level traceability and permits rollback without introducing spreadsheet concepts into Domain.

An import batch is unique by `(tenant, space, content hash)`. Source links and occurrence keys are checked before entity creation. The original binary is discarded after analysis, so the hash is the durable identity and the staging data is the review record.

### Parser boundary and safe workbook handling

Use ClosedXML only in Infrastructure behind an Application-level reader interface. Load in read-only mode, enumerate hidden sheets, use cached formula values without evaluation, preserve comments, enforce file/ZIP/workbook limits, and pass cancellation through analysis. ClosedXML is preferred over EPPlus because the documented target is MIT-compatible without a commercial runtime license; OOXML-only parsing was rejected as unnecessarily low-level for the workbook size and features.

Normalize headers with accent folding, whitespace/punctuation normalization, and known typo aliases. Detect variable blocks by recognized headers such as `Valor Quitação`, not fixed offsets. Classify totals, subtotals, sections, repeated headers, and model rows before creating eligible staging records.

### Source precedence and uncertainty policy

When annual and installment sources overlap, installment sheets prevail for installment number and value; annual sheets provide responsible party and planning context. Backup, 2027, and anticipation sources remain staging/reconciliation or forecast data and do not enter effective KPIs automatically. `P`/`Pago` requires first-import manual review and is represented as historical-settlement evidence, never as a bank payment without independent payment facts. Travel CLP/USD conversion requires a user-provided unit/rate during review.

Ambiguous dates remain unknown with raw context and an issue. Zero, empty, and `-` retain distinct meanings. Formula cache values are evidence and reconciliation inputs, not recalculated truth.

### API and transaction boundaries

Expose batch creation, status, paginated records, pending issues, row decisions, confirmation, report download, and cancellation under `/api/v1/importacoes`. Use separate view/execute permissions, existing context authorization, ProblemDetails conventions, bounded issue payloads, and optimistic version checks.

Confirmation uses the existing provider-aware execution strategy and transaction patterns, invokes existing domain/application services, writes source links and audit events in the same transaction, and marks the batch immutable after success. Duplicate hash requests return the existing batch rather than creating a second import.

### Persistence and provider portability

Configure four staging entities with existing naming, concurrency, tenant/space composite foreign keys, restrictive deletes, indexes for batch/decision/severity, bounded strings, and provider-equivalent JSON storage. PostgreSQL uses `jsonb`, while SQL Server uses `nvarchar(max)` with the repository's JSON validation convention. Generate independent migrations and snapshots; use provider-specific filtered/partial unique indexes where nullable occurrence keys require them.

Register `importacoes.visualizar` and `importacoes.executar` in the permission catalog and baseline roles, propagate permissions to existing spaces through data migration, and add import action/entity labels to the audit catalog.

### Frontend interaction

Add a scoped `/importacao` route and navigation entry. Use a hidden `.xlsx` input plus keyboard-accessible dropzone, local size/extension validation, multipart upload without a manually supplied `Content-Type`, polling for analysis status, paginated preview filters, row decisions, confirmation dialog, CSV report link, and invalidation of dashboard/obligation/title/fatura queries after confirmation. Keep monetary contract fields as strings and use existing formatting and form/error components.

## Risks / Trade-offs

- **[Formula cache is stale or misleading]** → Preserve formula and cache evidence, classify suspicious formulas, require review, and never evaluate workbook formulas.
- **[Overlapping worksheets double-count data]** → Hashes, occurrence keys, source links, explicit precedence, and reconciliation indicators prevent silent duplication.
- **[Large or malicious ZIP workbook]** → Enforce request, entry-count, decompressed-size, sheet, row, column, cell, and issue-count limits before full analysis.
- **[Partial confirmation across providers]** → Use execution strategy plus transaction, test rollback, and maintain independent migrations/snapshots for both providers.
- **[Import leaks tenant or source payload]** → Derive scope from active context, avoid client tenant IDs as authority, redact audit details, discard the binary, and test cross-space access.
- **[Frontend upload appears stalled]** → Use indeterminate upload feedback and status polling rather than claiming byte-level progress from `fetch`.
- **[New dependency violates architecture tests]** → Keep ClosedXML in Infrastructure only and add a dependency test/architecture assertion before integration.

## Migration Plan

1. Add staging entities/configurations, permissions, audit catalog entries, and provider-specific migrations.
2. Apply migrations to disposable Development PostgreSQL and SQL Server databases; run model-pending and isolation checks.
3. Deploy backend parser/services/API with limits enabled before exposing the frontend route.
4. Deploy frontend contracts, upload client, wizard, and scoped cache invalidation.
5. Run representative workbook fixtures and a disposable end-to-end import, verifying review decisions, rollback, idempotency, reconciliation indicators, and both providers.
6. Roll back application code to a compatible build if needed; do not remove applied schema. Correct migration issues with forward migrations. Since the original binary is not retained, failed analysis can be retried from the user's file and confirmed batches remain immutable.

## Open Questions

None. Source precedence, forecast handling, historical payment review, binary retention, and travel-currency review were confirmed for this change.
