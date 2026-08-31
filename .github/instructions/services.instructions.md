---
applyTo: "ClinicManagementSystem.Services/**/*.cs"
---

# Service Layer Instructions

- Implement business rules in services, not controllers or UI.
- Keep methods cohesive and side effects explicit.
- Validate business invariants close to where decisions are made.
- Use interfaces for externally consumed services and preserve existing abstractions.
- Prefer async methods for I/O bound operations.
- Avoid hidden mutable shared state.
- Keep notification, reminder, and background processing idempotent where feasible.
- Do not log sensitive patient or credential data.
