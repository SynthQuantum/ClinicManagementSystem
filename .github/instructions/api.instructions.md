---
applyTo: "ClinicManagementSystem.API/**/*.cs"
---

# API Layer Instructions

- Keep controllers focused on transport concerns only.
- Delegate domain logic to service interfaces in `ClinicManagementSystem.Services.Interfaces`.
- Validate incoming models and return clear validation errors.
- Enforce authentication and authorization on protected routes.
- Use explicit, consistent status codes (`200`, `201`, `204`, `400`, `401`, `403`, `404`, `409`, `500`).
- Never expose stack traces or internal infrastructure details in responses.
- Ensure audit-relevant actions write structured audit events through existing patterns.
- Use cancellation tokens for long-running requests and downstream calls where available.
- Keep endpoint contracts stable unless a change request explicitly allows a breaking change.
