# financial-catalogs Specification

## Purpose

Lets members maintain the space-scoped financial reference data needed to record obligations and account positions.

## Requirements

### Requirement: Members manage financial reference records
The system SHALL allow authorized members to create, view, update, and deactivate people, categories, institutions, accounts, cards, and card brands in an authorized financial space.

#### Scenario: Member creates an account
- **WHEN** an authorized member submits valid account data for the active space
- **THEN** the system stores the account in that space and returns it to the member

### Requirement: Referenced records preserve financial history
The system SHALL prevent physical deletion of a reference record that is used by a financial record and SHALL permit deactivation instead.

#### Scenario: Member deletes a referenced card
- **WHEN** a member attempts to delete a card referenced by an obligation or invoice
- **THEN** the system preserves the historical record and reports that it cannot be deleted

### Requirement: Sensitive account identifiers remain literal
The system SHALL preserve bank code, agency, account number, and check digit as supplied text, including leading zeroes and nonnumeric characters.

#### Scenario: Member saves an account with leading zeroes
- **WHEN** a member saves an account identifier containing leading zeroes
- **THEN** subsequent reads return the identifier without numeric normalization

### Requirement: Card brands are editable within their financial space
The system SHALL allow authorized members to create, view, update, and deactivate card brands in the active financial space and SHALL restrict card-brand references to that same tenant and space.

#### Scenario: Member edits a brand in one space
- **WHEN** an authorized member edits or deactivates a card brand
- **THEN** only that financial space's brand changes and brands with the same name in other spaces remain unchanged

#### Scenario: Member links a foreign-space brand
- **WHEN** a member submits a card referring to a brand from another tenant or space
- **THEN** the system rejects the reference without revealing the foreign brand

#### Scenario: Referenced brand is deleted
- **WHEN** a member attempts to delete a brand used by a card
- **THEN** the system preserves the brand and offers deactivation instead

#### Scenario: Earlier global brands are migrated
- **WHEN** a deployment upgrades from the initial global-brand schema
- **THEN** each card retains its brand label through a brand owned by its own space and unreferenced global rows remain preserved as legacy evidence
