# Scaling Tools Reference

## Purpose

This file lists recommended tools for each scaling activity and how to choose between them.

## Infrastructure Provisioning

### Azure CLI (az)

Use when:

1. You need quick imperative operations.
2. You are validating or troubleshooting resource state.

Strengths:

1. Fast scripting.
2. Easy CI integration.

### Bicep

Use when:

1. You want native Azure IaC with readable templates.
2. Your team prefers Azure-first workflows.

Strengths:

1. Strong Azure resource support.
2. Built-in what-if deployment analysis.

### Terraform

Use when:

1. You want cloud-neutral IaC patterns.
2. Your organization already uses Terraform.

Strengths:

1. Rich ecosystem.
2. Modular patterns and policy tooling.

## Deployment Automation

### Azure Developer CLI (azd)

Use when:

1. You want environment templates and app+infra deployment orchestration.

Useful commands:

```powershell
azd init
azd env new <env-name>
azd provision
azd deploy
```

### GitHub Actions or Azure DevOps

Use when:

1. You need CI checks, deployment gates, and approvals.
2. You need migration jobs and staged rollouts.

## Runtime Scaling

### App Service Autoscale

Use when:

1. Running API/Blazor as App Services.
2. You need independent scale rules per service.

### KEDA (for containerized workers)

Use when:

1. Queue depth should drive worker scale.
2. Running on Kubernetes or container platforms that support KEDA.

## Messaging

### Azure Service Bus

Use when:

1. You need reliable enterprise messaging.
2. You need dead-letter queues, sessions, advanced retry behavior.

### Azure Storage Queue

Use when:

1. Workload is simpler and lower-throughput.
2. You need lower complexity and cost.

## Caching

### Azure Cache for Redis

Use when:

1. You need shared low-latency cache across instances.
2. You need cache-aside and session-related shared state.

## Monitoring and Diagnostics

### Application Insights + Log Analytics

Use when:

1. You need request tracing and dependency telemetry.
2. You need alerting and KQL analytics.

Common checks:

1. P95 latency trends.
2. Failed request spikes.
3. Dependency call failures.
4. Queue processing lag.

## Security and Configuration

### Azure Key Vault

Use when:

1. Managing JWT keys, database credentials, and certificates.
2. Enforcing secret rotation policies.

## Load Testing

### Azure Load Testing

Use when:

1. You need managed large-scale load infrastructure.
2. You want integration with Azure metrics.

### k6

Use when:

1. You prefer code-first load tests in CI.
2. You want lightweight local and pipeline execution.

## Suggested Default Stack for This Repository

1. App Service for API and Blazor (independent plans if traffic differs).
2. Azure SQL (HA tier) with controlled migrations.
3. Azure Cache for Redis for distributed cache.
4. Azure Service Bus for reminder and performance sample workflows.
5. Application Insights and Log Analytics for observability.
6. GitHub Actions for CI/CD with staged rollout.
