# Spec Delta

## MODIFIED Requirements

### Requirement: Business operations create an auditable event
The system SHALL persist an audit event for each configured business operation that changes or confirms financial or access state, including spreadsheet import decisions, confirmations, cancellations, and resulting financial changes, with the actor, active tenant/space, action code, affected entity, entity identifier, UTC instant, correlation identifier, and safe change details.

#### Scenario: Confirmed payment is audited atomically
- **WHEN** an authorized user confirms a payment successfully
- **THEN** the payment state and its `pagamento.confirmado` audit event are committed together in the active tenant and space

#### Scenario: Confirmed import is audited atomically
- **WHEN** an authorized user confirms an import successfully
- **THEN** the resulting financial state, source links, and import confirmation audit event are committed together in the active tenant and space

#### Scenario: Failed business transaction does not leave a success audit event
- **WHEN** a configured business operation fails and its transaction is rolled back
- **THEN** the corresponding successful audit event is not persisted

### Requirement: Audit events contain safe details
The system SHALL validate audit action and entity codes, bound the serialized detail size, and redact credentials, tokens, cookies, complete bank identifiers, uploaded file payloads, and other configured sensitive values before persistence or display.

#### Scenario: Sensitive value is submitted as change detail
- **WHEN** an audit event includes a password, token, cookie, or complete account identifier in its detail object
- **THEN** the persisted and displayed detail replaces or omits the sensitive value

#### Scenario: Import payload is submitted as change detail
- **WHEN** an import event could include workbook contents or raw file payload
- **THEN** the event stores only safe metadata and bounded counts or references, never the workbook payload

#### Scenario: Detail exceeds the supported size
- **WHEN** an event detail exceeds the configured maximum serialized size or is not valid JSON
- **THEN** the event is rejected or safely reduced according to the audit writer contract and the business operation does not persist an unsafe success event
