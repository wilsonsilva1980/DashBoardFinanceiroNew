# financial-dashboard Specification

## Purpose

Provides space-scoped financial summaries that distinguish current obligations, planning, and reconciliation concerns.

## Requirements

### Requirement: Dashboard respects authorized financial context
The system SHALL return dashboard data only for the authenticated member's authorized active financial space.

#### Scenario: Member changes active space
- **WHEN** a member selects another authorized financial space
- **THEN** the dashboard refreshes with only that space's data

### Requirement: Dashboard distinguishes financial states
The system SHALL provide summaries by category, responsible party, due date, and monthly evolution while distinguishing effective, forecast, open, partial, paid, and overdue values.

#### Scenario: Member views a monthly summary
- **WHEN** a member requests a monthly dashboard summary
- **THEN** the system distinguishes planning values from effective outstanding values

### Requirement: Dashboard exposes reconciliation indicators
The system SHALL surface identified discrepancies, including excess credits and source-total mismatches, without silently incorporating them into normal payable totals.

#### Scenario: A reconciliable discrepancy exists
- **WHEN** dashboard data contains an identified discrepancy
- **THEN** the system presents an indicator for member review
