# Testing Status

## Current State

Automated tests are now included in this repository.

### Test Projects

- tests/ClinicManagementSystem.Models.Tests
- tests/ClinicManagementSystem.Data.Tests
- tests/ClinicManagementSystem.Services.Tests
- tests/ClinicManagementSystem.API.Tests
- tests/ClinicManagementSystem.API.IntegrationTests
- tests/ClinicManagementSystem.Blazor.Tests

Each solution project now has a matching unit-test project:

- ClinicManagementSystem.Models -> tests/ClinicManagementSystem.Models.Tests
- ClinicManagementSystem.Data -> tests/ClinicManagementSystem.Data.Tests
- ClinicManagementSystem.Services -> tests/ClinicManagementSystem.Services.Tests
- ClinicManagementSystem.API -> tests/ClinicManagementSystem.API.Tests
- ClinicManagementSystem.Blazor -> tests/ClinicManagementSystem.Blazor.Tests

### Coverage Focus

- Models unit tests:
  - computed property behavior and entity defaults
- Data unit tests:
  - EF Core timestamping and soft-delete query filters
- Services unit tests:
  - Patient create/get/delete (soft delete)
  - Staff create/update/delete (soft delete)
  - Appointment create/validation/conflict/status/delete
  - Notification reminder creation, deduplication, send transitions, and failure reasons
  - Performance summary calculations, p95, error rate, and persistence flush behavior
  - Dashboard summary counts
  - ML service dataset generation, training metrics, and prediction output shape
- API unit tests:
  - controller action branching and HTTP result mapping
  - performance middleware request sample capture
- API integration tests:
  - GET /api/Patients
  - GET /api/Appointments
  - GET /api/Dashboard/summary
- Blazor unit tests:
  - navigation component rendering

### Run Tests

```bash
dotnet test .\ClinicManagementSystem.slnx
```

### Latest Result

- Total tests: 29
- Total tests: 32
- Passed: 32
- Failed: 0
- Command: `dotnet test .\ClinicManagementSystem.slnx`

## Manual Verification Performed

- Build verification on all projects
- API endpoint checks for core modules
- UI flow checks for:
  - patient CRUD
  - staff CRUD
  - appointment create/update/delete
  - scheduling conflict warnings
  - dashboard cards and trend/workload
  - prediction risk display and ML metrics page

## Authentication Manual Checklist

- Log in to Blazor with development admin account.
- Verify successful redirect to dashboard.
- Verify Logout clears auth session and redirects to login.
- Attempt to access `/staff` as non-admin and confirm unauthorized behavior.
- Attempt to call protected API endpoint without JWT and confirm `401 Unauthorized`.
- Call `/api/auth/login` and retry protected API with bearer token.
- Verify role restrictions:
  - Patients: Admin, Doctor, Receptionist
  - StaffMembers: Admin only
  - Appointments: Admin, Doctor, Receptionist
  - Dashboard: Admin, Doctor
  - Predictions: Admin, Doctor, Receptionist
- Verify auth events exist in `AuditLogs` (login success/fail/logout).

## Recommended Next Additions

- API negative-path integration tests (validation and conflict responses)
- Focused coverage reporting in CI per project
- Component tests for form validation and submission flows in Blazor pages

## Reminder Workflow Manual Scenarios

- Create an appointment more than 24 hours in advance and run manual reminder processing.
- Verify reminder notifications are created for available patient channels (email/phone).
- Run processing again and verify duplicate reminders are not created.
- Set a due notification and verify status transitions from `Pending` to `Sent`.
- Use an invalid recipient and verify status becomes `Failed` with populated `FailureReason`.
- Verify `/notifications` monitoring page reflects pending/sent/failed counts and recent history.

## Evidence Placeholder (to fill per sprint)

- Build status: pass/fail
- API latency sample for top 5 endpoints
- Regression checklist coverage percent
- Defect count by module
