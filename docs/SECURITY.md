# Security and Privacy

## Purpose

This document summarizes security controls implemented in the Clinic Management System capstone and defines the minimum operational practices expected for any hosted environment.

## Security Posture Summary

Implemented controls in this repository:

- ASP.NET Core Identity user and role management
- JWT bearer authentication for API endpoints
- Cookie authentication for Blazor interactive sessions
- Role-based authorization on controllers and pages
- Audit logging for authentication and domain operations
- Request audit middleware for endpoint activity visibility
- Performance monitoring middleware for runtime observability
- Global soft-delete query filters for data lifecycle control

## Identity and Access Control

### Identity Model

- AppUser extends IdentityUser with role metadata and active/deleted flags.
- Identity tables are mapped to App-prefixed table names:
  - AppUsers
  - AppRoles
  - AppUserRoles
  - AppUserClaims
  - AppUserLogins
  - AppRoleClaims
  - AppUserTokens

### Authentication Flows

API:

- POST /api/auth/login issues JWT token
- POST /api/auth/logout requires authenticated principal

Blazor:

- POST /account/login signs users in with Identity cookies
- POST /account/logout clears cookie session
- POST /account/register creates new users with Receptionist role by default

### Role Enforcement

Role restrictions are applied in controller and page attributes.

- Admin has full administrative access
- Doctor has clinical and dashboard access
- Receptionist has front-desk workflow access

See README.md role matrix for consolidated capability mapping.

## Secrets Management

### JWT Key Requirements

The API requires Jwt:Key at startup and fails fast if it is missing.

Development setup using user-secrets:

    cd ClinicManagementSystem.API
    dotnet user-secrets init
    dotnet user-secrets set Jwt:Key "your-strong-random-secret"

Production requirement:

- provide Jwt:Key through secure environment configuration
- do not commit Jwt:Key into source-controlled appsettings files

## Data Protection Controls

Implemented technical controls:

- Soft-delete filters on core entities to reduce accidental hard deletion
- EF relationship constraints with restrict deletes for critical links
- Validation attributes and DTO boundaries for key API write models
- Authentication and operation logs persisted in AuditLogs

Operational controls expected for deployment:

- enforce HTTPS everywhere
- protect backups and database storage with encryption at rest
- restrict database/network access to least privilege
- configure retention and access policy for audit records

## Audit and Observability

### Audit Logging

The system writes audit entries for:

- authentication outcomes (login success/failure, lockout, logout)
- patient, staff, appointment, and prediction actions

### Request-Level Monitoring

- RequestAuditLoggingMiddleware captures endpoint usage context.
- PerformanceMonitoringMiddleware captures latency and status data.
- PerformanceSampleFlushHostedService persists samples for longer-term review.

## Known Security Limitations

- This repository is an educational capstone, not a certified healthcare product.
- No formal compliance certification artifacts are included (for example HIPAA/SOC2 audit evidence packs).
- Notification providers are currently logging implementations, not production-grade external delivery integrations.

## Hardening Recommendations Before Real Deployment

1. Use managed secret providers and key rotation processes.
2. Add centralized security logging and alerting.
3. Add API rate limiting and abuse protections.
4. Add formal vulnerability scanning to CI/CD.
5. Conduct threat modeling and penetration testing.
6. Implement compliance controls required by target jurisdiction.
