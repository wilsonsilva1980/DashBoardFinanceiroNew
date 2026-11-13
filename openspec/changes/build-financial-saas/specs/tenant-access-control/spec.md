# Spec Delta

## Purpose

Provides tenant-isolated financial spaces with authenticated, authorized access for their members.

## ADDED Requirements

### Requirement: Onboarding provisions an isolated financial context
The system SHALL create an active tenant, a default financial space, and an active administrator membership in one idempotent onboarding operation.

#### Scenario: New user completes onboarding
- **WHEN** an authenticated user completes initial onboarding
- **THEN** the user can access the newly created default space as its administrator

### Requirement: Access is restricted to authorized memberships
The system SHALL authorize financial requests using the authenticated user, active tenant membership, active space membership, and applicable space permissions.

#### Scenario: User requests another tenant's resource
- **WHEN** a user requests a resource outside an authorized tenant or space
- **THEN** the system rejects the request without revealing the resource

### Requirement: Authentication protects state-changing operations
The system SHALL use revocable secure sessions and require anti-forgery validation for authenticated state-changing browser requests.

#### Scenario: Request lacks a valid anti-forgery token
- **WHEN** an authenticated browser sends a state-changing request without a valid anti-forgery token
- **THEN** the system rejects the request without changing data
