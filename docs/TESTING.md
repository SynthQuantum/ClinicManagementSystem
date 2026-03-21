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
  - Dashboard summary counts
  - ML service dataset generation, training metrics, and prediction output shape
- API unit tests:
  - controller action branching and HTTP result mapping
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

- Total tests: 23
- Passed: 23
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

## Recommended Next Additions

- API negative-path integration tests (validation and conflict responses)
- Focused coverage reporting in CI per project
- Component tests for form validation and submission flows in Blazor pages

## Evidence Placeholder (to fill per sprint)

- Build status: pass/fail
- API latency sample for top 5 endpoints
- Regression checklist coverage percent
- Defect count by module
