---
mode: agent
model: GPT-5.3-Codex
description: Create or update a secure API endpoint in ClinicManagementSystem
---

# Secure API Endpoint Prompt

You are working in the ClinicManagementSystem repository.

## Goal

Create or update an ASP.NET Core API endpoint with secure-by-default behavior.

## Inputs

- Feature request: ${input:featureRequest}
- Target controller (optional): ${input:targetController}
- Required roles/policies (optional): ${input:authRequirements}

## Hard Requirements

- Keep controllers thin in `ClinicManagementSystem.API`.
- Put business rules in `ClinicManagementSystem.Services` through interfaces.
- Validate input models and return clear validation errors.
- Enforce authentication/authorization for protected operations.
- Never log secrets, tokens, PHI, or raw credentials.
- Use safe API errors (no stack traces or infrastructure internals).
- Keep API contracts backward compatible unless the request explicitly allows breaks.
- Add or update tests in `tests/` for behavior changes.

## Implementation Steps

1. Inspect existing controller and service patterns before coding.
2. Add or update request/response DTOs in the appropriate models project location.
3. Implement service-layer logic and validation.
4. Update controller endpoint to call service abstractions.
5. Add authorization attributes or policy checks.
6. Add or update unit/integration tests.
7. Run tests related to changed projects and summarize results.

## Output Format

Return:

1. Files changed with short rationale per file.
2. Security checks applied.
3. Test coverage added/updated.
4. Any assumptions or follow-up actions.
