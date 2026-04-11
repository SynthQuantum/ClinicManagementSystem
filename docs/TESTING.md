# Testing

## Test Projects

| Project                                             | What it covers                                |
| --------------------------------------------------- | --------------------------------------------- |
| `tests/ClinicManagementSystem.Models.Tests`         | Computed property behavior, entity defaults   |
| `tests/ClinicManagementSystem.Data.Tests`           | EF Core timestamps, soft-delete query filters |
| `tests/ClinicManagementSystem.Services.Tests`       | All service-layer logic (see below)           |
| `tests/ClinicManagementSystem.API.Tests`            | Controller action branching, middleware       |
| `tests/ClinicManagementSystem.API.IntegrationTests` | HTTP end-to-end via `WebApplicationFactory`   |
| `tests/ClinicManagementSystem.Blazor.Tests`         | Component rendering                           |

---

## Coverage Scope

### Service Unit Tests (`ClinicManagementSystem.Services.Tests`)

**Patient service** (`PatientServiceTests.cs`)

- Create and retrieve patients
- Soft-delete hides record from queries

**Staff service** (`StaffServiceTests.cs`)

- Create, update, soft-delete

**Appointment service** (`AppointmentServiceTests.cs`)

- Create and persist appointment
- Start-time-after-end-time validation
- Scheduling conflict for same staff

**Appointment conflict edge cases** (`AppointmentServiceConflictTests.cs`)

- Adjacent slots do **not** conflict
- Cancelled appointments do NOT block slots
- Same patient / different staff → patient conflict
- Cancelled new appointment skips conflict check
- `UpdateAsync` triggers conflict detection
- `UpdateStatusAsync` returns true / false
- `DeleteAsync` returns false when not found
- `GetByPatientAsync` / `GetByStaffAsync` / `GetByDateAsync` filter correctly

**Dashboard aggregation** (`DashboardAggregationTests.cs`)

- `GetAppointmentTrendAsync` returns `days+1` data points
- Zero-fills days with no appointments
- Counts appointments on the correct day
- Excludes appointments outside the lookback window
- `GetStaffWorkloadAsync` returns one row per staff member
- Counts total, completed, and no-show appointments per staff
- Orders by total appointments descending
- Includes staff name in output

**No-show prediction fallback** (`PredictionFallbackTests.cs`)

- Rule-based fallback returns valid risk level when no model is trained
- High-risk input profile → Medium or High risk level
- Low-risk input profile → lower probability
- `PredictNoShowForAppointmentAsync` throws `ArgumentException` for missing appointment
- Persists `PredictionResult` entity when `persist=true`
- Does NOT persist when `persist=false`

**Notification service** (`NotificationServiceTests.cs`)

- Creates reminders and prevents duplicates
- Marks notifications failed with failure reason
- Processes and sends due notifications

**Performance monitoring** (`PerformanceMonitoringServiceTests.cs`)

- Summary calculations: average, p95, error rate
- Flush persists samples to database

**ML service** (`PredictionServiceMlTests.cs`)

- Feature vector mapping is deterministic
- Dataset generation writes CSV with correct header
- Training returns metrics in valid range and saves model
- Model loads successfully after training
- `PredictNoShowAsync` returns valid output shape

---

### API Unit Tests (`ClinicManagementSystem.API.Tests`)

**PatientsController** (`PatientsControllerTests.cs`)

- `GET` with no query → calls `GetAllAsync`
- `GET` with query → calls `SearchAsync`
- `GET /{id}` → 200 (found) or 404 (not found)
- `POST` → 201 Created with `Location` header
- `PUT /{id}` → 200 (found) or 404 (not found)
- `DELETE /{id}` → 204 (found) or 404 (not found)

**StaffMembersController** (`StaffMembersControllerTests.cs`)

- `GET` → 200 with all staff (including empty collection)
- `GET /{id}` → 200 or 404
- `POST` → 201 Created
- `PUT /{id}` → 200 or 404
- `DELETE /{id}` → 204 or 404

**AppointmentsController** (`AppointmentsControllerTests.cs`)

- `GET` → all, by patient, by staff, by date (all filter paths)
- `GET /{id}` → 200 or 404
- `POST` → 201 Created / 400 Bad Request / 409 Conflict
- `PUT /{id}` → 404 / 409 Conflict
- `DELETE /{id}` → 204 or 404

**DashboardController** (`DashboardControllerTests.cs`)

- `GET /summary` → 200 with correct `NoShowRate`
- `GET /trend` → 200 with trend points; days parameter forwarded
- `GET /staff-workload` → 200 with workload list (including empty)

**PredictionsController** (`PredictionsControllerTests.cs`)

- `POST /no-show` → 200 with prediction output
- `POST /no-show/appointment/{id}` → 200 (found) or 400 (not found)
- `GET /no-show/metrics/latest` → 200 (exists) or 404 (none stored)

**Middleware** (`PerformanceMonitoringMiddlewareTests.cs`)

- Request sample captured with correct method, path, status code

**General controller branching** (`ControllersUnitTests.cs`)

- `PatientsController.GetAll` search branching
- `AppointmentsController.Create` conflict → 409
- `DashboardController.GetSummary` → 200

---

### API Integration Tests (`ClinicManagementSystem.API.IntegrationTests`)

Uses `WebApplicationFactory<Program>` with an in-memory database seeded before each class run.
All requests authenticate as an Admin user seeded during factory setup.

| Endpoint                            | Scenario                                 |
| ----------------------------------- | ---------------------------------------- |
| `GET /api/Patients`                 | Returns seeded patients                  |
| `POST /api/Patients`                | Creates patient, returns 201 + Location  |
| `GET /api/Patients/{id}`            | Returns 404 for unknown GUID             |
| `GET /api/Patients` (no auth)       | Returns 401 Unauthorized                 |
| `GET /api/StaffMembers`             | Returns 200 with staff list              |
| `POST /api/StaffMembers`            | Creates staff member, returns 201        |
| `GET /api/Appointments`             | Returns seeded appointments              |
| `GET /api/Appointments/{id}`        | Returns 404 for unknown GUID             |
| `GET /api/Dashboard/summary`        | Returns summary with counts > 0          |
| `GET /api/Dashboard/trend`          | Returns 8 points for `days=7`            |
| `GET /api/Dashboard/staff-workload` | Returns workload list                    |
| `POST /api/Predictions/no-show`     | Returns valid risk level and probability |

---

## Test Builders

Reusable fluent builders for constructing test fixtures are in
`tests/ClinicManagementSystem.Services.Tests/Builders/`:

```csharp
var patient = PatientBuilder.Default()
    .WithName("Alice", "Smith")
    .WithDateOfBirth(new DateTime(1990, 6, 15))
    .WithEmail("alice@test.local")
    .Build();

var staff = StaffMemberBuilder.Default()
    .WithRole(UserRole.Doctor)
    .WithSpecialty("Cardiology")
    .Build();

var appointment = AppointmentBuilder
    .For(patient.Id, staff.Id)
    .OnDate(DateTime.UtcNow.Date.AddDays(1))
    .WithSlot(new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0))
    .WithStatus(AppointmentStatus.Scheduled)
    .Build();
```

---

## Run Tests Locally

```bash
# All tests
dotnet test .\ClinicManagementSystem.slnx

# A single project
dotnet test .\tests\ClinicManagementSystem.Services.Tests\

# A specific test class
dotnet test .\tests\ClinicManagementSystem.Services.Tests\ --filter "FullyQualifiedName~AppointmentServiceConflictTests"

# With detailed output
dotnet test .\ClinicManagementSystem.slnx --logger "console;verbosity=detailed"

# With code coverage (requires coverlet)
dotnet test .\ClinicManagementSystem.slnx --collect:"XPlat Code Coverage"
```

---

## CI

Tests run automatically on every push and pull request via
`.github/workflows/test.yml`. The workflow:

1. Restores NuGet packages
2. Builds in Debug configuration
3. Runs the full test suite
4. Uploads `.trx` result files as a build artifact
5. Publishes a test summary via `dorny/test-reporter`

---

## Latest Result

| Metric      | Value |
| ----------- | ----- |
| Total tests | 107   |
| Failed      | 0     |
| Skipped     | 0     |

```bash
# Command used for final validation
dotnet test .\ClinicManagementSystem.slnx --no-build --logger "console;verbosity=minimal"
```

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
