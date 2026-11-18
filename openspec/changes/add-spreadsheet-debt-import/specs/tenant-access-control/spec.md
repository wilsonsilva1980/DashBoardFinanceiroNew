# Spec Delta

## MODIFIED Requirements

### Requirement: Access is restricted to authorized memberships
The system SHALL authorize financial requests using the authenticated user, active tenant membership, active space membership, and applicable space permissions, including separate permissions for viewing and executing spreadsheet imports.

#### Scenario: User requests another tenant's resource
- **WHEN** a user requests a resource outside an authorized tenant or space
- **THEN** the system rejects the request without revealing the resource

#### Scenario: Member lacks import permission
- **WHEN** a member without the required import view or execute permission requests an import operation
- **THEN** the API returns HTTP 403 and does not disclose staged data or change financial state

### Requirement: Authentication protects state-changing operations
The system SHALL use revocable secure sessions and require anti-forgery validation for authenticated state-changing browser requests, including upload, row decisions, confirmation, and cancellation.

#### Scenario: Request lacks a valid anti-forgery token
- **WHEN** an authenticated browser sends a state-changing request without a valid anti-forgery token
- **THEN** the system rejects the request without changing data

#### Scenario: Browser confirms an import
- **WHEN** an authenticated browser sends an import confirmation with a valid anti-forgery token
- **THEN** the system applies the request only within the server-authorized active space and permission set
