# Proposal

## Why

Financial obligations, payments, account positions, recurring expenses, and travel budgets are currently represented in a spreadsheet whose formulas and duplicated views cannot safely provide tenant-isolated, auditable financial operations. A manual-entry SaaS is needed now to preserve the business coverage while replacing implicit spreadsheet behavior with explicit rules.

## What Changes

- Add tenant and financial-space onboarding, membership, role-based authorization, secure local authentication, and session management.
- Add manual management of people, categories, institutions, accounts, cards, space-owned card brands, obligations, installments, titles, planning entries, and historical settlements.
- Add transactional payment allocation, reversal, settlement agreements, and balance/situation calculation with idempotency and auditability.
- Add idempotent recurring-expense generation and credit-card invoice composition that prevents purchases and invoices from being double counted.
- Add dated account-position snapshots, multi-currency travel budgets, exchange rates, and reconciliation-aware totals.
- Add authenticated dashboard views and REST/OpenAPI endpoints for financial queries and operations.
- Deliver a modular ASP.NET Core/React solution with provider-specific EF Core migrations for either PostgreSQL or SQL Server, selected per deployment.

## Capabilities

### New Capabilities

- `tenant-access-control`: Tenant/space provisioning, authenticated membership, authorization, and context isolation.
- `financial-catalogs`: Manual management of people, categories, institutions, accounts, cards, and editable card brands within a financial space.
- `financial-obligations`: Obligations, installments, payable titles, planning entries, state, and balance rules.
- `payment-settlement`: Confirmed payments, allocations, reversals, historical settlements, and settlement agreements.
- `recurring-expenses`: Recurrence definitions and idempotent generation of payable occurrences.
- `credit-card-invoices`: Card invoice titles and analytical purchases with single-source payable totals.
- `account-positions`: Dated account snapshots and explicitly scoped available-limit calculations.
- `travel-currency-budgeting`: Multi-currency travel items, exchange-rate capture, and conversion behavior.
- `financial-dashboard`: Space-scoped financial summaries, due dates, breakdowns, and reconciliation indicators.

### Modified Capabilities

None.

## Impact

- Creates the `Projetos/` solution structure for ASP.NET Core Web API, domain/application/infrastructure layers, React/TypeScript client, test projects, and Docker Compose.
- Adds REST endpoints under `/api/v1`, cookie/CSRF authentication, OpenAPI documentation, and authorization enforcement.
- Adds EF Core mappings and separate migration chains for PostgreSQL and SQL Server; each deployment selects one provider and does not dual-write.
- Introduces .NET, Node, package, container, and test dependencies subject to compatibility verification during implementation.
