# Spec Delta

## MODIFIED Requirements

### Requirement: Dashboard exposes reconciliation indicators
The system SHALL surface identified discrepancies from manual and imported data, including excess credits, duplicate candidates, source-total mismatches, and unresolved source precedence, without silently incorporating them into normal payable totals.

#### Scenario: A reconciliable discrepancy exists
- **WHEN** dashboard data contains an identified discrepancy
- **THEN** the system presents an indicator for member review

#### Scenario: An import contains conflicting source totals
- **WHEN** a confirmed import preserves a duplicate candidate, source mismatch, excess credit, or forecast-source discrepancy
- **THEN** the dashboard exposes the discrepancy separately and excludes it from ordinary effective payable totals until resolved

## ADDED Requirements

### Requirement: Imported forecasts remain separate from effective dashboard values
The system SHALL keep backup, future, anticipation, and other explicitly forecast import sources distinguishable from effective financial values in dashboard summaries.

#### Scenario: A future worksheet is imported
- **WHEN** a member confirms a row classified as forecast or scenario
- **THEN** the row is available in planning or reconciliation views but is excluded from effective totals by default
