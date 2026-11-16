# financial-obligations Specification

## Purpose

Represents manually entered debts and commitments as payable titles with explicit state, schedule, and balance behavior.

## Requirements

### Requirement: Members manage obligations and payable titles
The system SHALL allow authorized members to create and update obligations, installments, payable titles, responsible parties, due dates, and planning entries in their financial space.

#### Scenario: Member creates an installment title
- **WHEN** a member enters a valid installment title with an original amount and due date
- **THEN** the system records it as a payable title in the active space

### Requirement: Title state and settlement state are distinct
The system SHALL represent draft, forecast, effective, and cancelled title states separately from open, partial, paid, and overdue settlement status.

#### Scenario: Forecast title is viewed in outstanding totals
- **WHEN** a member views standard outstanding totals containing a forecast title
- **THEN** the forecast title is excluded unless the member selects a planning view

### Requirement: Balances are derived without silent truncation
The system SHALL derive a title balance from original value, adjustments, confirmed allocations, and active historical settlements, and SHALL surface rather than hide any excess legacy credit.

#### Scenario: Confirmed allocations exceed an original amount
- **WHEN** a title has a legacy excess payment or credit
- **THEN** the system identifies the excess for reconciliation instead of silently reducing it to zero

### Requirement: Unknown financial facts are not fabricated
The system SHALL allow unknown due dates, purchase dates, and historical details to remain unknown rather than substituting invented financial values or dates.

#### Scenario: Member records an incomplete historical obligation
- **WHEN** a member records a historical obligation without a confirmed purchase date
- **THEN** the system stores no purchase date and does not infer one
