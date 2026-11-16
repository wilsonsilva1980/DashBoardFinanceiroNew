# travel-currency-budgeting Specification

## Purpose

Lets members budget travel expenses in multiple currencies with captured, reviewable conversion evidence.

## Requirements

### Requirement: Members manage multi-currency travel budgets
The system SHALL allow authorized members to create travel budgets and record travel items with an original amount and ISO currency.

#### Scenario: Member records a foreign-currency item
- **WHEN** a member adds a travel item in a supported foreign currency
- **THEN** the system preserves the item's original amount and currency

### Requirement: Conversion preserves its source evidence
The system SHALL calculate a converted amount using a stored source currency, destination currency, rate, and rate date, rounding the monetary result explicitly.

#### Scenario: Member converts a travel item
- **WHEN** a member applies a valid exchange rate to a travel item
- **THEN** the system displays the converted value with the applied rate and rate date

### Requirement: Unresolved travel discrepancies remain visible
The system SHALL identify a negative or otherwise inconsistent travel balance as a reconciliation discrepancy rather than silently discarding it.

#### Scenario: Travel budget has an excess credit
- **WHEN** a calculated travel balance is negative
- **THEN** the system shows the discrepancy for member review
