# Spec Delta

## Purpose

A capacidade de trilha de auditoria permite registrar operações relevantes de negócio e consultar, com segurança, quem alterou cada registro dentro do espaço financeiro ativo. Ela oferece rastreabilidade operacional sem transformar a auditoria funcional em um depósito de logs técnicos ou dados sensíveis.

## ADDED Requirements

### Requirement: Business operations create an auditable event

The system SHALL persist an audit event for each configured business operation that changes or confirms financial or access state, including the actor, active tenant/space, action code, affected entity, entity identifier, UTC instant, correlation identifier, and safe change details.

#### Scenario: Confirmed payment is audited atomically

- **WHEN** an authorized user confirms a payment successfully
- **THEN** the payment state and its `pagamento.confirmado` audit event are committed together in the active tenant and space

#### Scenario: Failed business transaction does not leave a success audit event

- **WHEN** a configured business operation fails and its transaction is rolled back
- **THEN** the corresponding successful audit event is not persisted

### Requirement: Audit data is isolated and protected

The system SHALL derive audit scope from the server-authorized active context and SHALL prevent users from reading or writing audit records outside their active tenant and financial space.

#### Scenario: User queries the active space

- **WHEN** an authenticated member with `auditoria.visualizar` queries audit records
- **THEN** the response contains only records belonging to the active tenant and space

#### Scenario: User without audit permission queries the endpoint

- **WHEN** an authenticated user without `auditoria.visualizar` requests the audit listing
- **THEN** the API returns HTTP 403 and does not disclose audit data

#### Scenario: Client supplies another tenant or space

- **WHEN** a request attempts to use a tenant or space identifier that is not the server-authorized active context
- **THEN** the API ignores it as an authorization source or rejects the request and never returns records from that context

### Requirement: Audit events contain safe details

The system SHALL validate audit action and entity codes, bound the serialized detail size, and redact credentials, tokens, cookies, secrets, complete bank identifiers, and other configured sensitive values before persistence or display.

#### Scenario: Sensitive value is submitted as change detail

- **WHEN** an audit event includes a password, token, cookie, or complete account identifier in its detail object
- **THEN** the persisted and displayed detail replaces or omits the sensitive value

#### Scenario: Detail exceeds the supported size

- **WHEN** an event detail exceeds the configured maximum serialized size or is not valid JSON
- **THEN** the event is rejected or safely reduced according to the audit writer contract and the business operation does not persist an unsafe success event

### Requirement: Authorized users can query audit records

The system SHALL provide a read-only, paginated audit query supporting date range, action, entity, entity identifier, actor, and correlation filters, with deterministic newest-first ordering and bounded page size.

#### Scenario: User filters audit records

- **WHEN** an authorized user requests audit records with valid date and optional action, entity, entity identifier, user, or correlation filters
- **THEN** the API returns a page containing only matching records, total count, current page, page size, actor display information when available, and safe change details

#### Scenario: Query omits filters

- **WHEN** an authorized user requests the audit listing without optional filters
- **THEN** the API applies the documented default date window, returns records ordered by UTC instant descending with a stable identifier tie-breaker, and enforces the maximum page size

#### Scenario: Query parameters are invalid or excessive

- **WHEN** a request contains an invalid date, malformed identifier, unsupported action, inverted range, or page size above the limit
- **THEN** the API returns a validation error without executing an unbounded audit query

### Requirement: Users can inspect audit records in the workspace UI

The system SHALL provide a read-only audit screen for users with `auditoria.visualizar`, including filters, pagination, loading/error/empty states, and a detail view with escaped safe JSON.

#### Scenario: Authorized user opens the audit screen

- **WHEN** a user with `auditoria.visualizar` opens the audit route in an active space
- **THEN** the navigation exposes the route and the screen loads the active space audit page with supported filters and newest-first results

#### Scenario: User opens an audit detail

- **WHEN** an authorized user selects a row
- **THEN** the UI shows timestamp, action label and code, entity, entity identifier, actor, correlation identifier, and formatted safe change details without edit or delete controls

#### Scenario: User changes workspace or session ends

- **WHEN** the user changes the active space or logs out
- **THEN** audit query cache and visible records from the previous context are cleared before the next context is shown

### Requirement: Audit query and persistence are compatible with both database providers

The system SHALL preserve equivalent audit behavior, constraints, ordering, and supported filters for PostgreSQL and SQL Server migrations.

#### Scenario: Migrations are applied to PostgreSQL

- **WHEN** the PostgreSQL migration set is applied to a clean or current supported database
- **THEN** the audit indexes and schema changes are created without duplicate objects and the audit query contract works

#### Scenario: Migrations are applied to SQL Server

- **WHEN** the SQL Server migration set is applied to a clean or current supported database
- **THEN** the audit indexes and schema changes are created without duplicate objects and the audit query contract works
