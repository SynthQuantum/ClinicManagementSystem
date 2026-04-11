# Deployment Guide

## Purpose

This document explains how to run and deploy the current Clinic Management System implementation with explicit environment configuration, migration controls, and seeding behavior.

## Current Deployment Model

The repository currently supports application deployment using dotnet publish outputs for:

- ClinicManagementSystem.API
- ClinicManagementSystem.Blazor

Infrastructure-as-code templates and automated release pipelines are not yet included in this repository.

## Configuration Model (Standardized Sections)

Both API and Blazor appsettings now use the same core sections:

- ConnectionStrings
  - ClinicDb
- Authentication
  - Jwt (API)
  - IdentitySeed
- MlArtifacts
  - NoShowArtifactsPath
  - ModelFileName
  - DatasetFileName
  - MetricsFileName
- NotificationReminders
- PerformanceMonitoring
- StartupBehavior

Startup validates required values for:

- ConnectionStrings:ClinicDb
- MlArtifacts:NoShowArtifactsPath
- NotificationReminders numeric thresholds
- PerformanceMonitoring numeric thresholds
- Authentication:Jwt:Key (API)

## Local Development Run Steps

1.  Ensure .NET SDK 10 and LocalDB are available.
2.  Use Development environment.
3.  Confirm Development appsettings have a valid ConnectionStrings:ClinicDb value.
4.  From repository root:

        dotnet restore
        dotnet build ClinicManagementSystem.slnx -c Debug

5.  Run API:

        dotnet run --project ClinicManagementSystem.API --environment Development

6.  Run Blazor:

        dotnet run --project ClinicManagementSystem.Blazor --environment Development

Development defaults are configured for simple local startup:

- LocalDB connection string
- StartupBehavior migration and seeding enabled
- Development JWT key is present in API Development appsettings (development-only)

## Prerequisites

- .NET SDK 10 on build agent or host
- SQL Server instance accessible to both app hosts
- HTTPS termination strategy (reverse proxy, App Service, or equivalent)
- Secure runtime configuration for secrets

## Environment Guidance

### Development

Recommended behavior:

- ApplyMigrations: true
- EnsureCreatedWhenNoMigrations: true
- SeedDevelopmentData: true
- SeedIdentityData: true
- FailFastOnInitializationError: true

### Production

Recommended behavior:

- ApplyMigrations: false (run migrations in a controlled deployment step)
- EnsureCreatedWhenNoMigrations: false
- SeedDevelopmentData: false
- SeedIdentityData: false
- FailFastOnInitializationError: true

## Configuration Checklist

### Database Connection Strings

Set ConnectionStrings:ClinicDb for both hosts:

- ClinicManagementSystem.API/appsettings.Production.json or environment variables
- ClinicManagementSystem.Blazor/appsettings.Production.json or environment variables

### JWT Settings (API)

Required:

- Authentication:Jwt:Key
- Authentication:Jwt:Issuer
- Authentication:Jwt:Audience
- Authentication:Jwt:ExpiryMinutes

Important:

- Jwt:Key must be provided securely at runtime
- API startup fails if Jwt:Key is missing

### Optional Identity Seed Settings

For controlled non-production bootstrap only:

- Authentication:IdentitySeed:SeedAdmin
- Authentication:IdentitySeed:AdminEmail
- Authentication:IdentitySeed:AdminPassword

## Migration Steps

Use explicit migration commands instead of relying only on startup automation.

Add a migration:

    dotnet ef migrations add <MigrationName> --project ClinicManagementSystem.Data --startup-project ClinicManagementSystem.API

Apply migrations for API startup project:

    dotnet ef database update --project ClinicManagementSystem.Data --startup-project ClinicManagementSystem.API

Apply migrations for Blazor startup project:

    dotnet ef database update --project ClinicManagementSystem.Data --startup-project ClinicManagementSystem.Blazor

## Build and Publish

From repository root:

    dotnet restore
    dotnet build ClinicManagementSystem.slnx -c Release

Publish API:

    dotnet publish ClinicManagementSystem.API/ClinicManagementSystem.API.csproj -c Release -o out/api

Publish Blazor:

    dotnet publish ClinicManagementSystem.Blazor/ClinicManagementSystem.Blazor.csproj -c Release -o out/blazor

Deploy the generated output folders to your target runtime hosts.

## Startup Behavior in Hosted Environments

At startup, each host performs configurable initialization controlled by StartupBehavior:

- ApplyMigrations controls automatic migration execution
- EnsureCreatedWhenNoMigrations controls EnsureCreated fallback
- SeedDevelopmentData controls DevelopmentDataSeeder execution
- SeedIdentityData controls IdentitySeeder execution
- FailFastOnInitializationError controls whether startup stops on initialization failure

Identity seeding behavior also respects environment safety checks and Authentication:IdentitySeed settings.

## Recommended Hosting Topology

### Option A: Single VM with Reverse Proxy

- Host API and Blazor as separate services
- Route API traffic by path prefix or subdomain
- Terminate TLS at reverse proxy

### Option B: Managed App Services

- Deploy API and Blazor to separate app services
- Use managed SQL database
- Configure app settings and secrets in platform configuration

## Deployment Validation Checklist

After deployment, verify:

1. API starts without Authentication:Jwt:Key configuration errors.
2. Blazor login page is reachable.
3. POST /api/auth/login returns JWT for valid credentials.
4. Role-restricted endpoints enforce expected authorization.
5. Dashboard and appointments pages load and query data.
6. Prediction endpoints can generate/train and return metrics.
7. Reminder processing and performance sampling continue without exceptions.

## Operational Recommendations

- Enable structured logs and centralized collection.
- Monitor failed login, lockout, and authorization-denied events.
- Back up SQL data and validate restore procedures.
- Restrict outbound/inbound network access by least privilege.
- Add CI/CD deployment automation as a future enhancement.

## Production Cautions

- Do not keep development JWT values in production configuration.
- Do not enable SeedDevelopmentData in production.
- Do not enable SeedIdentityData unless explicitly intended for controlled bootstrap.
- Keep ApplyMigrations false for production unless your release process explicitly approves auto-migration.
- Keep connection strings and JWT key in secure environment configuration, not source-controlled files.

## Limitations

- This deployment guide targets capstone and educational hosting scenarios.
- No production-certified healthcare compliance pack is included.
- Notification provider integrations remain development-safe logging adapters.
