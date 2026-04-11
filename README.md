# Clinic Management System

## Project Overview

Clinic Management System is a .NET 10 capstone application for outpatient clinic operations. It combines a Blazor Server frontend, an ASP.NET Core Web API, an EF Core SQL Server data layer, and an ML.NET no-show prediction pipeline.

The repository now documents and demonstrates a complete capstone narrative: role-based workflows, auditable operations, scheduling safeguards, dashboard insights, and local AI-assisted no-show risk scoring.

## Business Problem

Small and medium clinics often struggle with fragmented workflows:

- Patient records are spread across informal tools
- Appointment scheduling is prone to double booking
- No-shows reduce utilization and increase wait times
- Operational leaders lack real-time insight into workload and outcomes

This system addresses those pain points with a unified workflow that supports scheduling, patient/staff operations, notifications, dashboard analytics, and no-show risk support.

## Target Users

- Admin: manages staff, users, settings, and operational oversight
- Doctor: views clinical workload, appointments, patients, and prediction insights
- Receptionist: handles front-desk workflows for patients, appointments, and reminders

## Architecture Summary

Core solution projects:

- ClinicManagementSystem.API: authenticated REST API layer
- ClinicManagementSystem.Blazor: server-rendered web UI with role-aware pages
- ClinicManagementSystem.Services: business logic and ML orchestration
- ClinicManagementSystem.Data: EF Core DbContext, identity integration, and seeding
- ClinicManagementSystem.Models: entities, enums, and DTO contracts

Runtime flow:

1. Blazor UI and API call service-layer abstractions
2. Services enforce business rules and persistence behavior
3. EF Core writes and reads SQL Server with soft-delete filters
4. ML.NET artifacts are stored locally in ml-artifacts/no-show

See docs/ARCHITECTURE.md for full details.

## Implemented Features

### Core Operations

- Patient management: create, update, search, detail, soft delete
- Staff management: create, update, list, detail, soft delete
- Appointment management: create, update, status patch, list, detail, delete
- Visit records: create and maintain encounter-level records
- Clinic settings: get and upsert clinic profile and schedule defaults
- Notifications: reminder creation, pending/history/summary views, send and process reminders

### Scheduling and Workflow Safeguards

- Conflict detection for overlapping appointments by:
  - Staff member
  - Patient
- Time-range validation on appointment requests
- Reminder deduplication rules in notification workflows

### Dashboard and Monitoring

- KPI summary: patients, appointments, completed, cancelled, no-show rate
- Appointment trend data for lookback periods
- Staff workload summaries
- API performance telemetry:
  - average latency
  - p95 latency
  - slow endpoints
  - recent failed requests

### Authentication, Authorization, and Auditability

- ASP.NET Core Identity integration with custom App tables
- JWT authentication for API clients
- Cookie authentication for Blazor interactive sessions
- Role-based authorization on API controllers and Blazor pages
- Audit log entries for auth and business actions

## Role Matrix

| Capability Area                         | Admin | Doctor | Receptionist |
| --------------------------------------- | ----- | ------ | ------------ |
| Dashboard home and summary insights     | Yes   | Yes    | No           |
| Patients pages and patient APIs         | Yes   | Yes    | Yes          |
| Appointments pages and appointment APIs | Yes   | Yes    | Yes          |
| Staff pages and staff APIs              | Yes   | No     | No           |
| Prediction lab and no-show inference    | Yes   | Yes    | Yes          |
| Notifications management pages and APIs | Yes   | Yes    | Yes          |
| Performance reset endpoint              | Yes   | No     | No           |

## API Endpoint Summary

| Area              | Endpoints                                                                                                                                                                                                         | Notes                                     |
| ----------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------- |
| Auth              | POST /api/auth/login, POST /api/auth/logout                                                                                                                                                                       | JWT login plus authenticated logout       |
| Patients          | GET /api/Patients, GET /api/Patients/{id}, POST /api/Patients, PUT /api/Patients/{id}, DELETE /api/Patients/{id}                                                                                                  | Query search supported on list            |
| StaffMembers      | GET /api/StaffMembers, GET /api/StaffMembers/{id}, POST /api/StaffMembers, PUT /api/StaffMembers/{id}, DELETE /api/StaffMembers/{id}                                                                              | Admin restricted                          |
| Appointments      | GET /api/Appointments, GET /api/Appointments/{id}, POST /api/Appointments, PUT /api/Appointments/{id}, PATCH /api/Appointments/{id}/status, DELETE /api/Appointments/{id}                                         | Supports date, patient, and staff filters |
| Dashboard         | GET /api/Dashboard/summary, GET /api/Dashboard/trend, GET /api/Dashboard/staff-workload                                                                                                                           | Admin and Doctor roles                    |
| Predictions       | POST /api/Predictions/no-show, POST /api/Predictions/no-show/appointment/{appointmentId}, POST /api/Predictions/no-show/dataset, POST /api/Predictions/no-show/train, GET /api/Predictions/no-show/metrics/latest | Local ML.NET workflow                     |
| Notifications     | GET /api/Notifications/pending, GET /api/Notifications/history, GET /api/Notifications/summary, POST /api/Notifications, POST /api/Notifications/{id}/send, POST /api/Notifications/process-reminders             | Reminder operations                       |
| VisitRecords      | GET /api/VisitRecords/patient/{patientId}, GET /api/VisitRecords/{id}, POST /api/VisitRecords, PUT /api/VisitRecords/{id}, DELETE /api/VisitRecords/{id}                                                          | Encounter history                         |
| AppUsers          | GET /api/AppUsers, GET /api/AppUsers/{id}, POST /api/AppUsers, PUT /api/AppUsers/{id}, DELETE /api/AppUsers/{id}                                                                                                  | Identity user management                  |
| AuditLogs         | GET /api/AuditLogs, GET /api/AuditLogs/{id}, POST /api/AuditLogs                                                                                                                                                  | Audit persistence                         |
| ClinicSettings    | GET /api/ClinicSettings, PUT /api/ClinicSettings                                                                                                                                                                  | Configuration profile                     |
| PredictionResults | GET /api/PredictionResults/appointment/{appointmentId}, POST /api/PredictionResults                                                                                                                               | Stored prediction records                 |
| Performance       | GET /api/Performance/summary, POST /api/Performance/reset                                                                                                                                                         | Runtime performance visibility            |

## Database Entity Summary

| Entity            | Purpose                                                                | Key Relationships                                                                     |
| ----------------- | ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| AppUser           | Identity-backed user account with role metadata and soft-delete fields | Identity roles and claims tables                                                      |
| Patient           | Demographics, insurance, contact, and emergency details                | One-to-many with Appointment and VisitRecord                                          |
| StaffMember       | Clinical/front-desk personnel with role and specialty                  | One-to-many with Appointment and VisitRecord                                          |
| Appointment       | Scheduled encounter with timing, status, and prediction fields         | Many-to-one to Patient and StaffMember; one-to-many Notification and PredictionResult |
| VisitRecord       | Encounter documentation: diagnosis, treatment, prescription            | Many-to-one to Patient, StaffMember, optional Appointment                             |
| Notification      | Appointment-linked reminders and delivery status                       | Many-to-one to Appointment                                                            |
| PredictionResult  | Stored model outputs and metadata for appointments                     | Many-to-one to Appointment                                                            |
| AuditLog          | Audit trail of auth and domain actions                                 | Optional actor and entity references                                                  |
| ClinicSettings    | Clinic-level operational configuration                                 | Standalone configuration entity                                                       |
| PerformanceSample | Request latency and status telemetry records                           | Standalone performance entity                                                         |

## AI No-Show Prediction Summary

The no-show subsystem is implemented with ML.NET and supports:

- Direct feature-based inference
- Appointment-based inference with optional persistence
- Synthetic plus historical dataset generation
- FastTree binary classifier training and metric reporting
- Local artifact persistence:
  - ml-artifacts/no-show/no_show_training_data.csv
  - ml-artifacts/no-show/no_show_model.zip
  - ml-artifacts/no-show/no_show_model_metrics.json

Model outputs include risk probability, categorical risk level, and recommendation text for follow-up actions.

See docs/ML.md for full model details.

## Security and Privacy Considerations

Implemented controls:

- Authentication and role-based authorization in API and UI
- JWT key fail-fast validation at API startup
- Audit trail for authentication and domain actions
- Request auditing and performance middleware
- DTO-based write paths for core API modules
- Global soft-delete query filters in DbContext

Operational requirements for any real deployment:

- Use HTTPS and secure transport everywhere
- Store secrets outside source control
- Enforce access governance and least privilege
- Define retention and monitoring policies for sensitive records

See docs/SECURITY.md for implementation and hardening guidance.

## Setup Instructions

### Prerequisites

- .NET SDK 10
- SQL Server instance

### 1. Configure Connection Strings

Update ClinicDb connection string in:

- ClinicManagementSystem.API/appsettings.Development.json
- ClinicManagementSystem.Blazor/appsettings.Development.json

### 2. Configure JWT Secret for API

From the API project folder:

    dotnet user-secrets init
    dotnet user-secrets set Jwt:Key "your-strong-random-secret"

The API does not start if Jwt:Key is missing.

### 3. Optional Development Admin Seeding

In API development configuration, set:

- IdentitySeed:SeedAdmin=true
- IdentitySeed:AdminEmail=your-admin-email
- IdentitySeed:AdminPassword=your-strong-password

### 4. Restore and Build

    dotnet restore
    dotnet build ClinicManagementSystem.slnx -c Debug

### 5. Run Applications

API:

    dotnet run --project ClinicManagementSystem.API

Blazor:

    dotnet run --project ClinicManagementSystem.Blazor

## Testing Instructions

Run full automated test suite:

    dotnet test ClinicManagementSystem.slnx --no-build --logger "console;verbosity=minimal"

Current baseline in repository:

- Total tests: 107
- Failed: 0

Detailed test strategy and project coverage are documented in docs/TESTING.md.

## Deployment Notes

- The repository includes environment-specific appsettings files for API and Blazor.
- Deployment currently targets self-managed environments (for example VM or App Service) using dotnet publish artifacts.
- Migrations and seed logic are applied during startup when configured environment and provider conditions are met.
- No production IaC templates are committed in this repository at this time.

See docs/DEPLOYMENT.md for step-by-step deployment guidance.

## Limitations

- This project is an educational capstone and learning artifact.
- ML training data is synthetic plus local historical development data, not validated clinical production datasets.
- The application is not production-certified healthcare software and has not been certified against formal medical compliance frameworks.

## Future Enhancements

- Expand integration tests for validation and conflict-negative API paths
- Introduce CI coverage reporting and quality gates
- Add production-ready delivery providers for email and SMS
- Add secure secret-provider integration and environment hardening automation
- Enhance ML feature engineering with richer historical appointment behavior
- Add deployment automation with infrastructure-as-code and release workflows
