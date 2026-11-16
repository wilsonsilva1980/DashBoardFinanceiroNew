# account-positions Specification

## Purpose

Captures bank-account positions as dated observations without misrepresenting them as income or cash-flow movements.

## Requirements

### Requirement: Members record dated account snapshots
The system SHALL allow authorized members to record account balance, savings balance, overdraft usage, and an effective position date separately.

#### Scenario: Member records an account position
- **WHEN** a member submits a valid dated account-position snapshot
- **THEN** the system stores the values as a historical snapshot for that account

### Requirement: Account positions are not income
The system SHALL exclude account-position snapshots from income and expense calculations.

#### Scenario: Dashboard calculates expense totals
- **WHEN** the dashboard includes an account position snapshot in the selected period
- **THEN** the snapshot does not change income or expense totals

### Requirement: Overdraft availability is explicit
The system SHALL calculate available overdraft limit from the configured limit and recorded overdraft usage without inferring one balance field from another.

#### Scenario: Account has savings and overdraft usage
- **WHEN** a member views an account with both savings and overdraft data
- **THEN** the system keeps the balances distinct and calculates available overdraft from overdraft usage only
