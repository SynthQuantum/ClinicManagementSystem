# Security & HIPAA Compliance

## Overview

This document describes the security controls implemented in the Clinic Management System, maps each control to its relevant HIPAA Security Rule provision, and captures known gaps with recommended mitigations. It provides a transparent, auditable record of HIPAA Privacy and Security Rule readiness for any hosted environment.

---

## Threat Model Summary

| Actor                      | Trust Level       | Entry Points               |
| -------------------------- | ----------------- | -------------------------- |
| Authenticated Doctor       | Medium            | API (JWT), Blazor UI       |
| Authenticated Receptionist | Low-Medium        | API (JWT), Blazor UI       |
| Authenticated Admin        | High              | API (JWT), Blazor UI       |
| Unauthenticated user       | None              | Public login endpoint only |
| Internal service (ML.NET)  | High (in-process) | Direct service call        |

Primary assets protected: patient PHI (diagnoses, treatments, prescriptions, visit records), appointment records, staff PII, and the audit log itself.

---

## Implemented Controls

### 1. Authentication — §164.312(d)

**Control**: JWT bearer authentication using ASP.NET Core Identity.

- Tokens are signed with HMAC-SHA256; key provided via `Jwt:Key` in configuration (never committed to source control).
- Token expiry: 60 minutes (configurable via `Jwt:ExpireMinutes`).
- Identity lockout: **5 failed attempts → 15-minute lockout** (configured in `Program.cs`).
- Password policy: minimum 8 characters, requires digit, uppercase, non-alphanumeric character.
- Passwords are stored as bcrypt hashes via `UserManager<AppUser>` — never plaintext.
- `AppUsersController` uses `UserManager.CreateAsync(user, password)` so `PasswordHash`/`SecurityStamp` cannot be overposted by callers.

**Blazor UI adds**: Cookie-based Identity authentication (`POST /account/login`, `POST /account/logout`, `POST /account/register`).

**Secrets management**:

```
cd ClinicManagementSystem.API
dotnet user-secrets init
dotnet user-secrets set Jwt:Key "your-strong-random-secret"
```

Production must supply `Jwt:Key` through a secure environment mechanism (Azure Key Vault, environment variable). Do not commit it to `appsettings.json`.

**Relevant files**: `AuthController.cs`, `Program.cs`

---

### 2. Role-Based Access Control (RBAC) — §164.312(a)(1)

**Control**: Every controller enforces `[Authorize]` with role-scoped policies following the principle of least privilege.

| Controller                             | Allowed Roles               | Notes                       |
| -------------------------------------- | --------------------------- | --------------------------- |
| `AuthController`                       | Anonymous (login only)      | Logout requires auth        |
| `PatientsController`                   | Admin, Doctor, Receptionist | PHI access                  |
| `AppointmentsController`               | Admin, Doctor, Receptionist |                             |
| `VisitRecordsController` (reads)       | Admin, Doctor, Receptionist | PHI — reads are audited     |
| `VisitRecordsController` (write)       | Admin, Doctor               | PHI mutation                |
| `VisitRecordsController` (delete)      | Admin                       |                             |
| `StaffMembersController`               | Admin                       |                             |
| `AuditLogsController`                  | Admin                       | Tamper-resistant, read-only |
| `ClinicSettingsController`             | Admin                       |                             |
| `PredictionResultsController` (read)   | Admin, Doctor               |                             |
| `PredictionResultsController` (create) | Admin                       | Prevents result fabrication |
| `PredictionsController`                | Admin, Doctor               | ML inference                |
| `DashboardController`                  | Admin, Doctor, Receptionist | Aggregated view only        |
| `NotificationsController`              | Admin, Doctor, Receptionist |                             |
| `AppUsersController`                   | Admin                       | Identity management         |

**Relevant files**: All files in `ClinicManagementSystem.API/Controllers/`

---

### 3. Audit Controls — §164.312(b)

**Control**: Append-only audit log for all PHI access and security-relevant events.

Every controller action that reads or mutates PHI calls `IAuditLogService.CreateAsync` capturing:

| Field               | Purpose                                                                       |
| ------------------- | ----------------------------------------------------------------------------- |
| `EntityName`        | Entity accessed (e.g. `"Patient"`, `"VisitRecord"`)                           |
| `ActionType`        | What happened (`"Read"`, `"Create"`, `"Update"`, `"Delete"`, `"LoginFailed"`) |
| `EntityId`          | Primary key of the affected record                                            |
| `PerformedByUserId` | Identity of the authenticated user                                            |
| `UserRole`          | Role claim at the time of the action                                          |
| `IpAddress`         | Client IP (X-Forwarded-For → RemoteIpAddress; IPv6-safe, ≤45 chars)           |
| `HttpMethod`        | HTTP verb of the request                                                      |
| `RequestPath`       | URL path of the request                                                       |
| `Outcome`           | `"Success"` or `"Failure"`                                                    |
| `ChangesJson`       | Serialized before/after state for mutations                                   |
| `Description`       | Human-readable summary (≤2000 chars)                                          |

**Tamper resistance**:

- `AuditLog` has **no soft-delete query filter** — records cannot be hidden by setting `IsDeleted = true`.
- `AuditLogsController` is **read-only**. The `POST /api/AuditLogs` endpoint that previously allowed fabrication of audit entries has been removed.
- Composite indexes on `(EntityName, CreatedAt)` and `(PerformedByUserId, CreatedAt)` support efficient compliance queries.

**Read audit coverage** (HIPAA §164.312(b)):

- `VisitRecordsController.GetByPatient` logs a `ListedByPatient` event for every PHI listing.
- `VisitRecordsController.GetById` logs a `Read` event for each individual record access.

**Admin audit query endpoints**:

| Endpoint                                              | Description                                  |
| ----------------------------------------------------- | -------------------------------------------- |
| `GET /api/AuditLogs`                                  | Most recent N entries                        |
| `GET /api/AuditLogs/{id}`                             | Single entry                                 |
| `GET /api/AuditLogs/by-user/{userId}`                 | All actions by a user                        |
| `GET /api/AuditLogs/by-entity/{entityName}?entityId=` | All actions on a specific record             |
| `GET /api/AuditLogs/by-date-range?from=&to=`          | Time-bounded query                           |
| `GET /api/AuditLogs/security-events`                  | Failed logins, lockouts, unauthorized access |

**Relevant files**: `AuditLog.cs`, `IAuditLogService.cs`, `AuditLogService.cs`, `AuditLogsController.cs`

---

### 4. Transmission Security — §164.312(e)(1)

**Control**: HTTPS enforced at the transport layer.

- `app.UseHttpsRedirection()` redirects all HTTP to HTTPS.
- `app.UseHsts()` applied in all non-development environments.
- `UseForwardedHeaders(XForwardedFor | XForwardedProto)` ensures that reverse-proxy HTTPS termination is reflected correctly in the request scheme — and thus in `GetClientIpAddress()`.

**Relevant file**: `Program.cs`

---

### 5. Security Response Headers

**Control**: `SecurityHeadersMiddleware` adds defensive HTTP response headers to every non-Swagger response.

| Header                    | Value                                        | Purpose                         |
| ------------------------- | -------------------------------------------- | ------------------------------- |
| `X-Content-Type-Options`  | `nosniff`                                    | Prevent MIME-type sniffing      |
| `X-Frame-Options`         | `DENY`                                       | Prevent clickjacking            |
| `X-XSS-Protection`        | `1; mode=block`                              | Legacy browser XSS filter       |
| `Referrer-Policy`         | `strict-origin-when-cross-origin`            | Limit referrer leakage          |
| `Permissions-Policy`      | `geolocation=(), microphone=(), camera=()`   | Deny unnecessary browser APIs   |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` | API-appropriate restrictive CSP |

**Relevant file**: `ClinicManagementSystem.API/Middleware/SecurityHeadersMiddleware.cs`

---

### 6. Rate Limiting on Authentication — §164.312(a)(2)(iii)

**Control**: Fixed-window rate limiter on the login endpoint.

- Policy `auth-fixed-window`: 10 requests per minute per IP address.
- Returns HTTP 503 when the limit is exceeded (no queuing).
- Decoration: `[EnableRateLimiting("auth-fixed-window")]` on `POST /api/Auth/login`.
- Complements Identity lockout (5 failed attempts → 15-minute lockout).

**Relevant files**: `Program.cs`, `AuthController.cs`

---

### 7. CORS Policy

**Control**: Environment-aware named CORS policy `ClinicCorsPolicy`.

- **Development / Testing**: Any origin permitted (local developer convenience).
- **Production**: Only origins listed in the `AllowedCorsOrigins` configuration array are permitted; credentials are allowed.
- Applied before authentication in the middleware pipeline.

**Relevant file**: `Program.cs`

---

### 8. HTTP-Level Audit Logging

**Control**: `RequestAuditLoggingMiddleware` produces structured log entries for every request.

Captured fields: `Method`, `Path`, `UserId`, `Role`, `IpAddress`, `StatusCode`, `Duration`.

Severity:

- `LogWarning` — HTTP 401 (unauthenticated attempt) and 403 (role/policy violation).
- `LogError` — HTTP 5xx.
- `LogInformation` — all other responses.

This provides a second, independent audit channel (log sink) separate from the database audit log.

**Relevant file**: `ClinicManagementSystem.API/Middleware/RequestAuditLoggingMiddleware.cs`

---

### 9. Input Validation & Overposting Prevention

**Control**: Dedicated DTOs with data annotations for all mutation endpoints.

| DTO                        | Used By                     | Protection                                                                      |
| -------------------------- | --------------------------- | ------------------------------------------------------------------------------- |
| `CreateUserRequest`        | `AppUsersController.Create` | Prevents overposting of `PasswordHash`, `SecurityStamp`; role is validated enum |
| `UpdateUserRequest`        | `AppUsersController.Update` | No password field; prevents credential overwrite                                |
| `VisitRecordUpsertRequest` | `VisitRecordsController`    | Prevents overposting of audit/timestamp fields                                  |

Model-state validation errors return HTTP 400 with `ValidationProblemDetails`.

---

### 10. Encryption at Rest

**Status**: Delegated to infrastructure.

SQL Server Transparent Data Encryption (TDE) must be enabled at the database server level (Azure SQL enables TDE by default). This is not enforced at the application layer.

---

## Gap Analysis

| #   | Gap                                       | Risk                                                        | Recommendation                                                                                                                          |
| --- | ----------------------------------------- | ----------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | **JWT token revocation**                  | Stolen token valid until expiry (60 min)                    | Implement a token blocklist (Redis) checked per request, or use short-lived tokens (5–15 min) with revocable server-side refresh tokens |
| 2   | **Encryption at rest**                    | PHI unencrypted if storage medium compromised               | Enable SQL Server TDE or Azure SQL encryption in production                                                                             |
| 3   | **Audit log retention (6 years)**         | HIPAA requires 6-year retention                             | Archive records older than 6 years to cold storage; do not delete                                                                       |
| 4   | **Business Associate Agreements**         | Vendors handling PHI require signed BAAs                    | Obtain BAAs from cloud provider, backup vendor, and any third-party integrations                                                        |
| 5   | **Backup & disaster recovery**            | No backup schedule at application layer                     | Configure automated SQL backups; test point-in-time restore; document RTO/RPO                                                           |
| 6   | **PHI de-identification for ML training** | Training data may contain PHI                               | De-identify per HIPAA Safe Harbor §164.514(b) before use in model training                                                              |
| 7   | **MFA / 2FA**                             | Password-only authentication                                | Add TOTP-based MFA for Admin and Doctor accounts via `SignInManager`                                                                    |
| 8   | **Breach notification workflow**          | No breach detection or notification process                 | Define breach response plan; alert on elevated 401/403/5xx rates                                                                        |
| 9   | **Formal risk register**                  | Risk analysis documented here but not in a tracked register | Create and review a formal risk register per §164.308(a)(1)                                                                             |
| 10  | **Penetration testing**                   | No security testing beyond unit tests                       | Conduct a pre-deployment penetration test and remediate findings                                                                        |

---

## HIPAA Security Rule Mapping

| HIPAA Section                            | Description                                                            | Status                                         |
| ---------------------------------------- | ---------------------------------------------------------------------- | ---------------------------------------------- |
| §164.308(a)(1) — Risk Analysis           | Gaps identified and documented in this file                            | Partial — formal risk register not yet created |
| §164.308(a)(3) — Workforce Security      | RBAC limits access to minimum necessary PHI                            | Implemented                                    |
| §164.308(a)(5) — Security Training       | Out of scope for this system                                           | Gap                                            |
| §164.310(d)(1) — Device & Media Controls | Infrastructure-level control                                           | Gap                                            |
| §164.312(a)(1) — Access Control          | Role-based JWT controls on all endpoints                               | Implemented                                    |
| §164.312(a)(2)(i) — Unique User ID       | Each AppUser has a unique Guid; audit entries record PerformedByUserId | Implemented                                    |
| §164.312(a)(2)(iii) — Automatic Logoff   | JWT expiry (60 min); rate limiting on auth endpoint                    | Partial                                        |
| §164.312(b) — Audit Controls             | Append-only audit log; all PHI reads and writes logged                 | Implemented                                    |
| §164.312(c)(1) — Integrity               | Audit log tamper resistance (no soft-delete, no create endpoint)       | Implemented                                    |
| §164.312(d) — Authentication             | Identity lockout, password policy, JWT                                 | Implemented                                    |
| §164.312(e)(1) — Transmission Security   | HTTPS + HSTS + ForwardedHeaders                                        | Implemented                                    |

---

## Security Architecture Notes

### Middleware Pipeline Order (`Program.cs`)

```
ForwardedHeaders
SecurityHeadersMiddleware
HSTS (non-development only)
HttpsRedirection
CORS
RateLimiter
RequestAuditLoggingMiddleware
Authentication
Authorization
Controllers
```

Order is significant: `ForwardedHeaders` must precede any IP-dependent middleware. Rate limiting and security headers are applied before authentication to protect the auth endpoint itself.

### IP Address Resolution (`HttpContextExtensions.GetClientIpAddress`)

1. `X-Forwarded-For` header — leftmost (client) entry, used when behind a trusted reverse proxy.
2. `HttpContext.Connection.RemoteIpAddress` — direct connection IP.
3. `"unknown"` — fallback when neither is available.

IPv6 addresses are stored as-is (the `IpAddress` column supports up to 45 characters).

### Audit Log Database Schema

```
AuditLog
├── Id (Guid, PK)
├── EntityName   (nvarchar 100)  ┐ Composite index
├── CreatedAt    (datetime2)     ┘
├── PerformedByUserId (Guid)     ┐ Composite index
├── CreatedAt    (datetime2)     ┘
├── ActionType   (nvarchar 100)
├── EntityId     (Guid, nullable)
├── UserRole     (nvarchar 100)
├── IpAddress    (nvarchar 45)
├── Outcome      (nvarchar 20)     -- "Success" | "Failure"
├── HttpMethod   (nvarchar 10)
├── RequestPath  (nvarchar 500)
├── ChangesJson  (nvarchar max)
└── Description  (nvarchar 2000)
```

No `HasQueryFilter` on `AuditLog` — records are never hidden or soft-deleted.
