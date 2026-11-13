# Design

## Context

The repository currently contains the consolidated architecture document and workbook, with no application implementation. See `proposal.md` for motivation and the change specs for behavioral contracts. The implementation must begin from manual entry, retain unknown facts as unknown, and support either PostgreSQL or SQL Server per deployment without dual writes.

## Goals / Non-Goals

**Goals:**

- Build a modular monolith that enforces tenant and financial-space isolation at API, query, and relational layers.
- Represent payable obligations and their settlement independently from spreadsheet-derived labels and formula layouts.
- Make financial operations auditable, idempotent where retried, and transactionally safe under concurrent updates.
- Deliver a React client and documented REST API backed by portable EF Core mappings and provider-specific migrations.

**Non-Goals:**

- Spreadsheet upload, parsing, staging, or automated reconciliation.
- SaaS subscription billing, bank synchronization, full income ledger, or transfer accounting.
- Per-customer database provider selection, cross-provider replication, or automatic database migration between providers.
- Treating historical spreadsheet totals, comments, or formula results as verified financial transactions.

## Decisions

### Modular Clean Architecture

Create `Projetos/Financeiro.slnx` with Domain, Application, Infrastructure, API, React client, and focused test projects. Domain owns financial invariants; Application owns use cases and ports; Infrastructure owns EF Core, Identity, providers, and repositories; API owns HTTP binding and composition.

This keeps business rules independent of UI and storage while retaining a single deployable service. Microservices were rejected because the initial scope needs transactional consistency across identity, tenancy, titles, and payments rather than independent deployment boundaries.

### Tenant and space scope is server-owned

Resolve the active tenant and financial space from the authenticated session and membership, never from a body field as authorization evidence. Every financial entity carries `tenant_id` and `espaco_id`; relational alternate keys and composite foreign keys enforce same-scope references. Global query filters help prevent omissions but do not replace write validation or explicit scope in raw/admin/job queries.

This avoids relying solely on client routing or ORM filters. A tenant-only data model was rejected because users require distinct family, personal, and travel spaces.

### Card brands belong to financial spaces

User decision: card brands are editable per financial space, not a shared global catalog. Brands carry tenant/space keys, active state, concurrency version, and a name unique within that scope. Cards reference brands through a composite same-scope FK. Catalog permissions cover creation, editing, deactivation, and restricted deletion of brands.

Upgrade the earlier global-brand model with separate scoped storage. Preserve the old global rows as legacy evidence, copy each referenced brand to every distinct card scope, and update card references transactionally. Unreferenced legacy brands remain in legacy storage because assigning them to a space would fabricate ownership. No endpoint exposes mutable global brands. This scope-changing migration is forward-only; collapsing independently edited brands on downgrade would lose data.

### Identity uses local cookies with anti-forgery protection

Use ASP.NET Core Identity with secure, revocable cookies, CSRF validation for mutations, and policy-based tenant/space authorization. Provision the tenant, default space, administrator membership, and baseline roles in one idempotent transaction.

Cookie authentication matches the first-party React SPA. Bearer/OIDC is deferred because it adds integration scope without being the selected authentication model.

### Financial model centers on titles and settlement allocations

Model obligations as their origin and titles as payable occurrences. Preserve original amount, adjustments, state, settlement evidence, and concurrency version. Confirmed payments allocate to titles; historical settlements can reduce balances without a date; reversals preserve history. A settlement agreement simulates first and applies a discount plus allocations atomically only on confirmation.

Balance is derived from original value, adjustments, confirmed allocations, and active historical settlements. This replaces binary spreadsheet status with explicit, auditable state and prevents a single payment from being recorded twice. Persisting a mutable “remaining value” as the sole source of truth was rejected because it cannot reliably explain or reverse changes.

### Recurrences and invoice composition avoid duplicate liability

Recurrences use an anchor date, periodicity, nominal due day, and unique occurrence key. The generation job/API creates only missing occurrences and uses the month's final day for invalid nominal days while preserving the original nominal day.

Credit-card invoice titles are the payable source. Purchases linked to an invoice remain analytical details and are excluded from accounts-payable aggregation. This avoids reproducing source-sheet double counting.

### Provider portability is explicit

Use a single `FinanceiroDbContext` combining Identity and financial data so onboarding/access provisioning can be transactional. Keep shared model mappings in Infrastructure and generate separate SQL Server and PostgreSQL migration chains. Select the provider from deployment configuration, one provider per deployment. Use application-generated GUIDs, explicit `decimal(19,2)`, `DateOnly`, UTC instants, explicit concurrency tokens, and provider-specific migration SQL only behind provider branches.

EF Core 10/provider compatibility with the selected .NET target must be restored and tested as a set before versions are locked. A shared migration chain was rejected because filtered indexes, checks, locking SQL, JSON, and date functions differ by provider.

### Concurrency and idempotency are use-case concerns

For payment confirmation, lock selected titles in deterministic ID order using a provider-specific `ITituloLockService`, validate allocation totals after the lock, and commit all mutations in one transaction. Idempotency keys scope to tenant, space, actor/operation, and request payload hash. Ordinary edits use optimistic concurrency versions and return conflict responses.

This handles retries and competing financial writes without hiding duplicate effects. Retrying arbitrary failed transactions without an idempotency identity was rejected because external clients cannot distinguish a retry from a new payment.

### API and client boundaries

Expose versioned `/api/v1` REST controllers with `ProblemDetails`, validation, pagination, and OpenAPI metadata. Document cookie/CSRF interaction and expose development Swagger UI. The React/TypeScript client uses typed request boundaries, form validation, query caching scoped to the active tenant/space, and clears cached financial data on logout or context switch.

This provides a testable backend contract while preventing client cache leakage between spaces. Direct database access from the client and a generic, unscoped API surface were rejected.

### Observability and auditability

Record audit metadata for confirmed operations, reversals, historical-settlement revocation, and support-sensitive actions. Emit structured logs with correlation identifiers and tenant-safe context; never log sensitive account identifiers or session secrets. Keep deletions restricted to unreferenced drafts and use cancellation/reversal for durable finance records.

## Risks / Trade-offs

- [Current runtime/provider version compatibility is unverified] → Lock SDK and package versions only after restore warnings-as-errors, builds, and a two-provider test matrix pass.
- [Composite tenant/space keys increase mapping complexity] → Centralize mappings and add integration tests for foreign-scope reads and writes.
- [Cross-provider locking SQL can diverge] → Hide it behind a narrow interface and test concurrent confirmation and retry behavior on both providers.
- [A broad first release can delay usable milestones] → Implement in vertical slices: tenancy/catalogs, titles, settlement, recurrence/cards, positions/travel, then dashboards.
- [Manual historical records may remain incomplete] → Keep date and evidence explicit, separate them from cash-flow claims, and surface reconciliation work.
- [Cookie-based SPA operations can be exposed to CSRF] → Require anti-forgery tokens on mutations and verify them in API integration tests.

## Migration Plan

1. Establish repository tooling, solution skeleton, configuration, local containers, and CI checks without production data migration.
2. Create and test baseline schema/migrations independently for PostgreSQL and SQL Server.
3. Deploy a single selected provider to a nonproduction environment, provision tenants through onboarding, and validate scoped access and financial workflows.
4. Promote only after backup, migration, API contract, and browser workflow checks pass for the selected provider.
5. Roll back application releases through a compatible prior build. Do not remove applied financial schema or rewrite historical records; use forward migrations and corrective, audited operations.

## Open Questions

- Which exact stable .NET, EF Core, Identity, Npgsql, SQL Server, React, Node, and container image versions pass the compatibility gate at implementation time?
- What tenant and space role set, MFA policy, and password policy will be adopted beyond the minimum administrator bootstrap?
