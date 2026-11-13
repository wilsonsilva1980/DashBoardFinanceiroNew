# Spec Delta

## Purpose

Generates recurring financial obligations predictably from member-managed schedules without duplicating occurrences.

## ADDED Requirements

### Requirement: Recurrence uses explicit schedule semantics
The system SHALL generate monthly, quarterly, and annual occurrences from a stored anchor date and nominal due day.

#### Scenario: Schedule has a nominal day absent from a month
- **WHEN** a recurrence scheduled for day 31 generates an occurrence in a shorter month
- **THEN** the occurrence is due on that month's last valid day while future occurrences retain day 31 as the nominal day

### Requirement: Recurrence generation is idempotent
The system SHALL create at most one occurrence for each recurrence and occurrence key in a financial space.

#### Scenario: Member generates the same horizon twice
- **WHEN** a member runs generation twice for an already generated horizon
- **THEN** the system does not create duplicate titles

### Requirement: Edited pricing does not rewrite history
The system SHALL apply a changed recurrence amount only to newly generated occurrences and SHALL preserve prior occurrence values.

#### Scenario: Member changes a subscription price
- **WHEN** a member changes a recurrence amount after titles exist
- **THEN** existing titles retain their original amounts
