# Testing Strategy

## Purpose

This document defines the automated and manual testing approach for the Clinic Management System capstone and records the current baseline test status.

## Test Project Inventory

| Test Project                                      | Focus Area                                          |
| ------------------------------------------------- | --------------------------------------------------- |
| tests/ClinicManagementSystem.Models.Tests         | Entity behavior and computed properties             |
| tests/ClinicManagementSystem.Data.Tests           | DbContext timestamps and soft-delete query filters  |
| tests/ClinicManagementSystem.Services.Tests       | Core business services and ML service behavior      |
| tests/ClinicManagementSystem.API.Tests            | Controller-level unit tests and middleware behavior |
| tests/ClinicManagementSystem.API.IntegrationTests | End-to-end API behavior via WebApplicationFactory   |
| tests/ClinicManagementSystem.Blazor.Tests         | Blazor component rendering checks                   |

## Coverage Summary

### Service Layer Coverage

Implemented service tests currently validate:

- patient lifecycle operations
- staff lifecycle operations
- appointment creation and conflict logic
- appointment conflict edge cases
- dashboard aggregation and trend/workload calculations
- notification reminder creation, deduplication, and processing outcomes
- performance monitoring summary and persistence behavior
- prediction service feature mapping, dataset generation, training, load, fallback, and persistence paths

### API Unit Coverage

Implemented controller and middleware tests currently validate:

- patients, staff, appointments, dashboard, and predictions controller branching
- success, bad-request, conflict, and not-found paths
- role-sensitive flow assumptions at controller invocation level
- performance middleware sampling behavior

### API Integration Coverage

Current integration suite validates authenticated and unauthenticated endpoint behavior for:

- patients
- staff members
- appointments
- dashboard
- predictions

Integration setup uses WebApplicationFactory with in-memory database configuration and seeded auth data.

## Role-Driven Test Intent Matrix

| Role         | Primary flows verified in automated tests                         |
| ------------ | ----------------------------------------------------------------- |
| Admin        | Full CRUD modules, dashboard visibility, administrative endpoints |
| Doctor       | Clinical dashboard and appointment-facing flows                   |
| Receptionist | Front-desk patient/appointment/prediction operational flows       |

## Key Scenario Buckets

| Scenario Type                       | Coverage Status              |
| ----------------------------------- | ---------------------------- |
| Valid request flow                  | Covered                      |
| Invalid input and validation path   | Covered in unit tests        |
| Not-found path                      | Covered                      |
| Conflict path (appointments)        | Covered                      |
| Authentication-required path        | Covered in integration tests |
| Aggregation correctness (dashboard) | Covered                      |
| ML fallback behavior                | Covered                      |

## Reusable Test Builders

Shared fixture builders are available in tests/ClinicManagementSystem.Services.Tests/Builders:

- PatientBuilder
- StaffMemberBuilder
- AppointmentBuilder

These builders are used to keep service tests readable and reduce duplication for common entity setup.

## How to Run Tests

From repository root:

Run all tests:

    dotnet test ClinicManagementSystem.slnx

Run with minimal console output:

    dotnet test ClinicManagementSystem.slnx --no-build --logger "console;verbosity=minimal"

Run only services tests:

    dotnet test tests/ClinicManagementSystem.Services.Tests

Run only API integration tests:

    dotnet test tests/ClinicManagementSystem.API.IntegrationTests

Run a specific class filter example:

    dotnet test tests/ClinicManagementSystem.Services.Tests --filter "FullyQualifiedName~AppointmentServiceConflictTests"

Collect coverage output (if collector is available):

    dotnet test ClinicManagementSystem.slnx --collect:"XPlat Code Coverage"

## Continuous Integration

GitHub Actions workflow:

- .github/workflows/test.yml

Current CI behavior:

1. restore dependencies
2. build solution
3. run full test suite
4. publish TRX artifacts
5. publish summarized results

## Latest Validated Baseline

| Metric      | Value |
| ----------- | ----- |
| Total tests | 107   |
| Passed      | 107   |
| Failed      | 0     |
| Skipped     | 0     |

Validation command used:

    dotnet test ClinicManagementSystem.slnx --no-build --logger "console;verbosity=minimal"

## Manual Verification Scope

Manual checks performed for capstone demonstration readiness include:

- login/logout and unauthorized routing behavior
- patient CRUD UI flow
- staff CRUD UI flow
- appointment scheduling and conflict feedback
- dashboard summary and trend rendering
- prediction lab output rendering and metrics page visibility
- notifications page behavior

## Current Gaps and Next Testing Enhancements

- add more integration tests for invalid payload model-state paths
- add richer component tests for form validation and role-specific UI behavior
- add CI-level coverage thresholds by project area
- add non-functional test scripts for load and resilience profiling
