# Spec Delta

## MODIFIED Requirements

### Requirement: Members manage obligations and payable titles
The system SHALL allow authorized members to create and update obligations, installments, payable titles, responsible parties, due dates, and planning entries in their financial space, including records approved from a reviewed spreadsheet import.

#### Scenario: Member creates an installment title
- **WHEN** a member enters a valid installment title with an original amount and due date
- **THEN** the system records it as a payable title in the active space

#### Scenario: Member confirms an imported obligation
- **WHEN** a member approves a valid staged obligation or title during import confirmation
- **THEN** the system creates or reuses the corresponding obligation data with its source link and imported state

### Requirement: Unknown financial facts are not fabricated
The system SHALL allow unknown due dates, purchase dates, payment details, currencies, and historical details to remain unknown rather than substituting invented financial values or dates.

#### Scenario: Member records an incomplete historical obligation
- **WHEN** a member records a historical obligation without a confirmed purchase date
- **THEN** the system stores no purchase date and does not infer one

#### Scenario: Member confirms an imported row with an ambiguous field
- **WHEN** an approved import row lacks enough evidence for a purchase date, payment detail, or currency
- **THEN** the system preserves the unknown field and stores the source context for later review instead of fabricating a value
