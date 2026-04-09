# Architecture

## Runtime Components

- Blazor frontend: [ClinicManagementSystem.Blazor](../ClinicManagementSystem.Blazor)
- Web API backend: [ClinicManagementSystem.API](../ClinicManagementSystem.API)
- Business services: [ClinicManagementSystem.Services](../ClinicManagementSystem.Services)
- Data access and seed: [ClinicManagementSystem.Data](../ClinicManagementSystem.Data)
- Shared entities and DTOs: [ClinicManagementSystem.Models](../ClinicManagementSystem.Models)
- Authentication and identity: ASP.NET Core Identity with EF Core

## Layer Responsibilities

- UI/API layers handle user interaction and endpoint routing.
- Services layer contains business logic, scheduling checks, dashboard aggregation, and ML workflows.
- Data layer provides EF Core DbContext and persistence.
- Models layer defines entities and DTO contracts used across projects.

## Data Flow

```mermaid
sequenceDiagram
    participant User
    participant Blazor
    participant API
    participant Services
    participant DbContext
    participant SQL

    User->>Blazor: Schedule appointment
    Blazor->>Services: Validate + save request
    Services->>DbContext: Query conflicts + update
    DbContext->>SQL: Read/Write
    SQL-->>DbContext: Result
    DbContext-->>Services: Entities
    Services-->>Blazor: Success / conflict message

    User->>Blazor: Request no-show prediction
    Blazor->>Services: PredictNoShow...
    Services->>Services: Load ML model (local)
    Services-->>Blazor: Risk + probability + recommendation
```

## Cross-Cutting Notes

- Soft-delete query filters are configured in DbContext.
- Startup applies pending migrations.
- Seed data runs idempotently at startup.
- Identity roles and development admin are seeded idempotently at startup.
- ML inference is local with ML.NET and model files under [ml-artifacts/no-show](../ml-artifacts/no-show).
- Notification reminders are processed by a hosted background service in the API (`ReminderProcessingHostedService`) using `INotificationService`.
- Delivery providers are currently development-safe logging implementations (`LoggingEmailSender`, `LoggingSmsSender`) behind sender interfaces.
- Reminder deduplication is enforced by appointment + reminder timestamp + recipient checks before insert.
- API request performance is captured by `PerformanceMonitoringMiddleware` and summarized by `IPerformanceMonitoringService`.
- Performance samples can be persisted to the `PerformanceSamples` table through the `PerformanceSampleFlushHostedService` background process.

## Authentication Architecture

- `AppUser` is integrated with ASP.NET Core Identity (`IdentityUser<Guid>`).
- API uses JWT bearer authentication (`/api/auth/login`).
- Blazor uses Identity application cookies (`/account/login`, `/account/logout`).
- Role-based authorization is enforced in API controllers and Blazor route attributes.
- Authentication-related events are persisted in `AuditLogs`.
