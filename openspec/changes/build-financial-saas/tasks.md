# Tasks

## 1. Solution and delivery foundation

- [x] 1.1 Create the `Projetos/` solution, Clean Architecture projects, React client, test projects, and reproducible .NET/Node tooling; verify locked dependency restore and baseline builds succeed.
- [x] 1.2 Resolve and lock compatible stable runtime, EF Core, Identity, database-provider, React, and container versions; verify restore treats downgrade and compatibility warnings as errors.
- [x] 1.3 Add Docker Compose for a selectable PostgreSQL or SQL Server development database plus environment configuration; verify each provider starts independently and the API selects only the configured provider.
- [x] 1.4 Configure CI to run formatting, builds, unit tests, API integration tests, frontend tests, and both database-provider migration/test jobs; verify all jobs execute from a clean checkout.

## 2. Persistence and tenancy foundation

- [x] 2.1 Implement the common financial and Identity persistence model with GUID identifiers, monetary precision, date/time conventions, concurrency versions, and tenant/space composite relationships; verify mappings create expected constraints for both providers.
- [x] 2.2 Create provider-specific PostgreSQL and SQL Server migration chains with portable schema behavior and provider-specific index/check differences; verify fresh migrations apply and roll forward in both containers.
- [x] 2.3 Implement tenant, financial-space, membership, baseline-role, and idempotent onboarding workflows; verify onboarding creates one usable administrator context and retrying it creates no duplicates.
- [x] 2.4 Add request-scoped tenant/space resolution, global query filtering, explicit write validation, and cross-scope integration tests; verify foreign tenant and foreign space reads/writes are rejected.

## 3. Authentication and authorization

- [x] 3.1 Configure local Identity registration, login, logout, secure revocable cookie sessions, and password/MFA policy extension points; verify unauthenticated API requests are rejected and logout invalidates access.
- [x] 3.2 Implement policy-based tenant/space permissions and prevent removal of the final tenant or space administrator; verify membership and last-administrator edge cases with integration tests.
- [x] 3.3 Add anti-forgery issuance and validation for browser mutations, OpenAPI security documentation, and development Swagger support; verify invalid or missing anti-forgery tokens cannot mutate data.

## 4. Financial catalogs and account positions

- [x] 4.1 Implement scoped CRUD/deactivation for people, categories, institutions, accounts, cards, and editable space-owned card brands while preserving literal account identifiers; migrate earlier global brands without losing names or references; verify API and unit tests retain leading zeroes and block cross-scope references.
- [x] 4.2 Enforce restricted deletion for referenced financial catalogs and return actionable validation errors; verify referenced accounts/cards cannot be physically deleted.
- [x] 4.3 Implement dated account-position snapshots and derived overdraft availability without treating snapshots as cash flow; verify position and dashboard-query tests keep savings, overdraft, income, and expenses separate.
- [x] 4.4 Build catalog and account-position React forms and lists with active-space cache scoping; verify switching space or logging out clears previously cached finance data.

## 5. Obligations, titles, and planning

- [x] 5.1 Implement obligations, installments, payable titles, responsible parties, planning entries, and lifecycle transitions; verify draft, forecast, effective, and cancelled title states through domain and API tests.
- [x] 5.2 Implement derived balance and settlement status calculations, including overdue semantics and unknown dates; verify forecast exclusion from standard totals and no fabricated dates in test cases.
- [x] 5.3 Add optimistic concurrency responses and audit-aware cancellation/deletion rules for financial records; verify concurrent edits return a conflict and only unreferenced drafts can be deleted.
- [x] 5.4 Build obligation, title, and planning workflows in the React client with validated monetary/date input; verify browser tests create and edit records without leaking another space's data.

## 6. Payments and settlement agreements

- [x] 6.1 Implement payment drafts, allocations, confirmation validation, deterministic title locking abstraction, and idempotency records; verify retry and concurrent-confirmation tests create a single settlement per request identity.
- [x] 6.2 Implement audit-preserving payment reversals and title balance recalculation; verify reversal restores payable balances without deleting the original payment.
- [x] 6.3 Implement approved historical settlements with optional effective date and audited revocation/replacement; verify undated settlements affect balances but not period cash-flow results.
- [x] 6.4 Implement settlement-agreement simulation and atomic confirmation with explicit discount allocation; verify installments and their confirmed agreement are not double counted.
- [x] 6.5 Build settlement UI flows and API contract tests for payment, reversal, history, and agreement operations; verify invalid allocation totals and cross-currency attempts are rejected.

## 7. Recurrences and card invoices

- [x] 7.1 Implement recurrence definitions, nominal-day handling, occurrence keys, and idempotent horizon generation; verify repeated generation creates no duplicates and day 31 rolls to month end.
- [x] 7.2 Preserve existing occurrence amounts after recurrence-price changes; verify tests distinguish historical and future generated title values.
- [x] 7.3 Implement invoice titles and linked analytical card purchases as a single payable source; verify aggregate payable queries count invoices once and exclude linked purchases.
- [x] 7.4 Build recurrence and invoice/purchase client screens with validation and API tests; verify incomplete card evidence displays available credit as unknown rather than a calculated value.

## 8. Travel currencies and reconciliation

- [x] 8.1 Implement scoped travel budgets, multi-currency travel items, ISO currency validation, and exchange-rate records; verify original currency/amount remain unchanged after conversion.
- [x] 8.2 Implement captured-rate conversion with explicit monetary rounding and rate-date traceability; verify conversion tests cover rounding and returned conversion evidence.
- [x] 8.3 Implement reconciliation discrepancy representation for excess credits and source-total mismatches; verify negative travel balances remain visible for review.
- [x] 8.4 Build travel-budget UI flows and API tests; verify unauthorized users cannot view or mutate another space's travel data.

## 9. Dashboard, API quality, and observability

- [x] 9.1 Implement space-scoped dashboard queries for summaries, monthly evolution, categories, responsible parties, due dates, and positions; verify effective, forecast, open, partial, paid, and overdue values remain distinct.
- [x] 9.2 Add reconciliation indicators to dashboard responses and UI without adding discrepancies to normal payable totals; verify query and browser tests surface discrepancies separately.
- [ ] 9.3 Complete versioned REST endpoints, pagination, validation, `ProblemDetails`, OpenAPI examples, and contract tests; verify generated OpenAPI JSON and Swagger UI render expected authenticated operations.
- [x] 9.4 Add structured logging, correlation identifiers, audit records, and secret/account-data redaction; verify sensitive values are absent from representative logs and confirmed operations have audit entries.

## 10. Release verification

- [ ] 10.1 Execute the full backend/frontend suite and PostgreSQL/SQL Server integration matrix, including migration, authorization, concurrency, and idempotency coverage; verify every required job passes.
- [ ] 10.2 Run an end-to-end manual-entry smoke workflow from onboarding through catalog, title, payment, recurrence, invoice, account position, travel, and dashboard; verify the workflow produces no duplicate payable amount or cross-space data exposure.
- [x] 10.3 Document local setup, provider selection, migration procedure, backup/rollback approach, security configuration, and known first-release exclusions; verify documented commands run from a clean environment.

## Workflow follow-up

- Archive the change after implementation and review requirements are satisfied.
- Verify the archived result.
