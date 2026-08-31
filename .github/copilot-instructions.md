# Clinic Management System Copilot Instructions

## Project Context

- This solution handles clinic operations, patient data, appointments, notifications, and audit trails.
- Treat all patient-facing and user-identifiable data as sensitive healthcare data.
- Prefer secure-by-default implementations over convenience.

## Architecture Expectations

- Keep business logic in `ClinicManagementSystem.Services`.
- Keep API controllers thin in `ClinicManagementSystem.API`.
- Use DTOs and entities from `ClinicManagementSystem.Models`.
- Do not move business rules into UI components.

## Coding Standards

- Target existing project patterns and naming conventions.
- Prefer async APIs with cancellation support where practical.
- Avoid broad refactors unless explicitly requested.
- Keep changes minimal, focused, and testable.

## Security Requirements

- Never log secrets, tokens, connection strings, PHI, or credential-like values.
- Validate and sanitize all external inputs.
- Enforce authorization checks for data-changing endpoints.
- Return safe error payloads and avoid leaking internal exception details.
- Prefer parameterized data access and avoid dynamic query concatenation.

## API Guidelines

- Use consistent response models and HTTP status codes.
- Prefer `ProblemDetails` style responses for API errors.
- Maintain backward compatibility for existing contracts unless the task requires breaking changes.

## Testing Expectations

- Add or update tests for changed behavior when feasible.
- Favor unit tests in service-layer changes and integration tests for API behavior.
- Keep tests deterministic and avoid time/network flakiness.

## Performance and Reliability

- Avoid N+1 query patterns and repeated expensive operations in loops.
- Use paging/filtering for list endpoints that can grow.
- Ensure background services support graceful shutdown and cancellation.

## Documentation

- For non-trivial behavior changes, update relevant docs in `docs/`.
- Include short comments only where logic is complex or non-obvious.
