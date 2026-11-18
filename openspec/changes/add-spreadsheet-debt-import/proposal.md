# Proposal

## Why

The current product accepts financial data manually, while the reference debt workbook contains historical obligations, installments, recurring expenses, account positions, cards, and travel information that users need to review before entering. A controlled `.xlsx` import now provides a practical migration path without inventing unknown facts, duplicating records, or bypassing tenant isolation and auditability.

## What Changes

- Add a space-scoped spreadsheet import capability for `.xlsx` files, including hidden worksheets, cached formula values, comments, source cell references, and bounded workbook validation.
- Analyze workbooks into an auditable staging area with normalized headers, row classifications, derived values, warnings/errors, duplicate candidates, source precedence, and preserved annotations.
- Add preview, filtering, row decisions, cancellation, CSV reporting, and atomic confirmation of approved records into existing financial entities.
- Make imports idempotent by content hash and occurrence keys, reusing existing links rather than silently creating duplicates.
- Preserve uncertainty: ambiguous dates, currencies, statuses, formulas, and historical payments remain reviewable or unknown; spreadsheet status alone does not create a bank payment.
- Add import permissions, audit events, API endpoints, request-size limits, and a React wizard with multipart upload, progress feedback, preview, decisions, and post-confirmation cache invalidation.
- Support equivalent migrations, constraints, indexes, and behavior for PostgreSQL and SQL Server.

## Capabilities

### New Capabilities

- `spreadsheet-import`: Upload, analyze, review, decide, and confirm spreadsheet-sourced financial records with staging, traceability, idempotency, and safe parsing.

### Modified Capabilities

- `financial-obligations`: Imported obligations, titles, planning entries, historical settlements, recurring expenses, account positions, and cards must obey the existing state, balance, and unknown-fact rules.
- `financial-dashboard`: Imported source discrepancies, duplicate candidates, excess credits, and source-total mismatches must remain visible as reconciliation indicators and must not inflate ordinary totals.
- `audit-trail`: Import analysis decisions, confirmations, cancellations, and resulting financial changes must use the existing safe, atomic audit contract.
- `tenant-access-control`: Import reads and mutations must require the active tenant/space context, import permissions, and CSRF protection for browser mutations.

## Impact

- Backend: new import domain/staging entities, EF configurations, dual-provider migrations, parser and application services, API controller/contracts, exception mappings, permissions, audit catalog entries, and upload limits.
- Frontend: import contracts and multipart client support, `/importacao` route/navigation item, wizard UI, styles, scoped query/cache behavior, unit tests, and Playwright coverage.
- Dependencies: add ClosedXML only to Infrastructure, with centralized package versioning; Domain and Application remain free of spreadsheet/OOXML package references.
- Operations and documentation: update CI/fixtures, README and architecture guidance, and validate atomic imports against both database providers.
