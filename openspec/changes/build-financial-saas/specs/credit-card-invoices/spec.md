# Spec Delta

## Purpose

Models credit-card invoices as the payable source while retaining purchases as useful analytical composition.

## ADDED Requirements

### Requirement: Invoice is the single payable source
The system SHALL treat a credit-card invoice title as the payable obligation and SHALL exclude its linked purchases from aggregate accounts-payable totals.

#### Scenario: Dashboard includes an invoice with purchases
- **WHEN** a dashboard calculates outstanding payables for an invoice and its linked purchases
- **THEN** it counts the invoice once and does not add purchase amounts separately

### Requirement: Members manage invoice composition
The system SHALL allow authorized members to record and view analytical purchases linked to a card invoice in the same financial space.

#### Scenario: Member adds a purchase to an invoice
- **WHEN** a member submits a valid purchase for an invoice in the active space
- **THEN** the system stores the purchase as invoice composition

### Requirement: Available card limit is not overstated
The system SHALL present available credit as unknown when closing, payment, or usage evidence is insufficient to calculate it reliably.

#### Scenario: Card has no confirmed usage history
- **WHEN** a member views a card with incomplete usage or closing information
- **THEN** the system does not present a precise available-limit value
