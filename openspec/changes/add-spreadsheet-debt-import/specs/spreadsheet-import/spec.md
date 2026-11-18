# Spec Delta

## Purpose

Permite importar planilhas de dívidas para um espaço financeiro com análise revisável, rastreabilidade de origem, confirmação atômica e proteção contra duplicação ou inferência de fatos desconhecidos.

## ADDED Requirements

### Requirement: Members can create a scoped spreadsheet import
The system SHALL accept only bounded `.xlsx` uploads and create an import batch in the authenticated member's active financial space.

#### Scenario: Member uploads a valid workbook
- **WHEN** an authorized member submits an `.xlsx` file within configured limits
- **THEN** the system creates a batch with a content hash, source metadata, and a processing state scoped to the active space

#### Scenario: Upload is unsupported or oversized
- **WHEN** a member submits a non-`.xlsx` file, malformed workbook, or file exceeding configured limits
- **THEN** the system rejects the request with a structured validation or size error and does not create financial records

### Requirement: Import analysis preserves source evidence
The system SHALL analyze visible and hidden worksheets without recalculating formulas and SHALL preserve worksheet, cell, raw-value, cached-value, formula, format, and comment references in staging.

#### Scenario: Workbook contains hidden worksheets and cached formulas
- **WHEN** the system analyzes the workbook
- **THEN** hidden worksheets and cached formula values are included in the staged source records without evaluating or replacing the workbook's formulas

#### Scenario: Workbook contains comments
- **WHEN** a source cell has a comment
- **THEN** the comment is preserved as an annotation linked to its worksheet and cell and is not converted into a financial transaction automatically

### Requirement: Import analysis classifies records and issues
The system SHALL classify source rows, derive review fields, and expose bounded issues for ambiguous, conflicting, unsupported, or suspicious source data.

#### Scenario: Workbook contains headers, totals, and data rows
- **WHEN** the analyzer processes a worksheet
- **THEN** it distinguishes records from headers, sections, subtotals, totals, and model rows and only records eligible source rows for decision

#### Scenario: Source data is ambiguous or inconsistent
- **WHEN** a date, status, currency, formula result, duplicate, or source total cannot be trusted
- **THEN** the system stages the row with a typed severity, source reference, and human-readable issue instead of inventing a value or silently discarding the discrepancy

### Requirement: Members can review and decide staged records
The system SHALL provide a paginated preview of staged records and allow authorized members to approve, reject, or ignore records with optimistic concurrency.

#### Scenario: Member filters the preview
- **WHEN** a member requests staged records with worksheet, severity, or decision filters
- **THEN** the system returns bounded pages with summary counts, source location, derived values, issues, and current decision

#### Scenario: Member decides a stale record
- **WHEN** a member submits a decision with an outdated record version
- **THEN** the system returns a conflict and leaves the existing decision unchanged

### Requirement: Confirmation creates approved financial data atomically
The system SHALL apply only approved, valid staged records in one transaction and SHALL report created, reused, rejected, and failed outcomes.

#### Scenario: Member confirms an import with approved records
- **WHEN** all mandatory review conditions are satisfied and the member confirms the batch
- **THEN** the system creates or reuses the corresponding financial entities atomically, records traceability links, and marks the batch confirmed

#### Scenario: A confirmation operation fails
- **WHEN** entity creation, linkage, or validation fails during confirmation
- **THEN** the transaction rolls back all financial changes and the batch exposes a failure result without a partial success state

### Requirement: Imports are idempotent and source precedence is explicit
The system SHALL use content hashes, occurrence keys, and source links to prevent duplicate imports and SHALL keep competing or forecast sources out of ordinary totals until explicitly decided.

#### Scenario: Member uploads identical content twice
- **WHEN** the same workbook content is uploaded again to the same space
- **THEN** the system returns or references the existing batch and does not create duplicate financial entities

#### Scenario: Annual and installment sources overlap
- **WHEN** multiple worksheets describe the same occurrence or a backup, forecast, or anticipation source competes with an effective source
- **THEN** the system marks the candidate and proposed precedence for review and does not silently count both sources as effective

### Requirement: Spreadsheet status does not fabricate a payment
The system SHALL preserve unknown dates, payment means, purchase dates, and currencies and SHALL represent spreadsheet payment markers as reviewable historical-settlement evidence unless independently confirmed.

#### Scenario: Source row contains `P` or `Pago` without payment evidence
- **WHEN** a member reviews a row marked paid without a confirmed date, account, and payment details
- **THEN** the system does not create a bank payment and keeps the settlement as reviewable historical evidence

#### Scenario: Source row contains an ambiguous date or currency
- **WHEN** a numeric value lacks sufficient date or currency context
- **THEN** the system retains the raw source value, leaves the uncertain domain field unknown, and requires review before confirmation

### Requirement: Import access and audit behavior are protected
The system SHALL restrict import operations to the active authorized space, require separate view and execute permissions, protect browser mutations with CSRF validation, and audit decisions, confirmations, cancellations, and resulting state changes.

#### Scenario: Member accesses another space's import
- **WHEN** a request references a batch outside the server-authorized active space
- **THEN** the API rejects the request without revealing the batch or its staged data

#### Scenario: Member confirms an import
- **WHEN** a confirmation succeeds
- **THEN** the system commits the financial changes and corresponding safe audit event together with actor, space, affected entity, and correlation data

### Requirement: Import behavior is equivalent across database providers
The system SHALL provide equivalent import states, constraints, uniqueness, isolation, and confirmation behavior on PostgreSQL and SQL Server.

#### Scenario: Import migrations run on PostgreSQL
- **WHEN** the PostgreSQL migration set is applied and an import is confirmed
- **THEN** staging, idempotency, and atomic confirmation work without provider-specific contract differences

#### Scenario: Import migrations run on SQL Server
- **WHEN** the SQL Server migration set is applied and an import is confirmed
- **THEN** staging, idempotency, and atomic confirmation work without provider-specific contract differences
