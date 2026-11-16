# payment-settlement Specification

## Purpose

Records auditable settlement of payable titles through payments, historical evidence, reversals, and negotiated agreements.

## Requirements

### Requirement: Confirmed payments allocate exactly once
The system SHALL confirm a payment only when its positive allocations match its amount, currency, tenant, and financial space, and SHALL make confirmation idempotent.

#### Scenario: Client retries payment confirmation
- **WHEN** a client repeats a confirmation request with the same idempotency identity
- **THEN** the system returns the original payment result without creating a second settlement

### Requirement: Payment reversal preserves history
The system SHALL reverse a confirmed payment as a single recorded operation that restores its allocation effects without deleting the original payment history.

#### Scenario: Member reverses a confirmed payment
- **WHEN** an authorized member reverses a confirmed payment
- **THEN** the affected titles are recalculated and the payment's original settlement remains auditable

### Requirement: Historical settlement supports incomplete evidence
The system SHALL permit an approved historical settlement to reduce a title balance without a confirmed payment date and SHALL exclude it from period cash-flow views when the date is unknown.

#### Scenario: Member records an undated historical settlement
- **WHEN** a member records an approved historical settlement without an effective date
- **THEN** the title balance is reduced and no dated cash-flow event is created

### Requirement: Settlement agreements avoid duplicate payable amounts
The system SHALL keep a settlement-agreement simulation nonfinancial until confirmed, and SHALL apply its discount and payment allocations atomically when confirmed.

#### Scenario: Member confirms an agreement covering installments
- **WHEN** an authorized member confirms a valid agreement for selected installments
- **THEN** the system settles the selected titles without adding the agreement and installments as separate payable totals
