# Deployment Guide

## Purpose

This document explains how to deploy the current Clinic Management System implementation in a capstone-ready but practical way. It covers prerequisites, configuration, publish commands, and post-deploy checks.

## Current Deployment Model

The repository currently supports application deployment using dotnet publish outputs for:

- ClinicManagementSystem.API
- ClinicManagementSystem.Blazor

Infrastructure-as-code templates and automated release pipelines are not yet included in this repository.

## Prerequisites

- .NET SDK 10 on build agent or host
- SQL Server instance accessible to both app hosts
- HTTPS termination strategy (reverse proxy, App Service, or equivalent)
- Secure runtime configuration for secrets

## Configuration Checklist

### Database Connection Strings

Set ConnectionStrings:ClinicDb for both hosts:

- ClinicManagementSystem.API/appsettings.Production.json or environment variables
- ClinicManagementSystem.Blazor/appsettings.Production.json or environment variables

### JWT Settings (API)

Required:

- Jwt:Key
- Jwt:Issuer
- Jwt:Audience
- Jwt:ExpiryMinutes

Important:

- Jwt:Key must be provided securely at runtime
- API startup fails if Jwt:Key is missing

### Optional Identity Seed Settings

For controlled non-production bootstrap only:

- IdentitySeed:SeedAdmin
- IdentitySeed:AdminEmail
- IdentitySeed:AdminPassword

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

At startup, each host performs environment-aware database initialization:

- applies pending EF migrations when available for relational providers
- falls back to EnsureCreated when no migrations are present
- runs development data seeders
- runs identity seeding according to environment/configuration rules

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

1. API starts without Jwt:Key configuration errors.
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

## Limitations

- This deployment guide targets capstone and educational hosting scenarios.
- No production-certified healthcare compliance pack is included.
- Notification provider integrations remain development-safe logging adapters.
