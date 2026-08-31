# Scaling Phase 1 Foundation

## Goal

Prepare the current system for distributed scale without changing all runtime behavior at once.

## Outcomes

1. Infrastructure managed through IaC.
2. Secrets externalized.
3. Production-safe database migration process.
4. Baseline telemetry, dashboards, and alerts.

## Step-by-Step

### Step 1: Create environment inventory

What to do:

1. List all current resources and configurations.
2. Capture API URL, Blazor URL, database server, cache, identity settings.
3. Record current app settings and environment variables.

Tools:

- Azure Portal for quick visual inventory.
- Azure CLI for exportable JSON.

Commands:

```powershell
az account show
az group list -o table
az resource list --resource-group <rg-name> -o table
```

### Step 2: Move infrastructure to IaC

What to do:

1. Choose Bicep or Terraform.
2. Define resources for API host, Blazor host, SQL, cache, monitoring, identity.
3. Parameterize per environment (dev, staging, prod).

Tools:

- Bicep CLI or Terraform CLI.
- Azure Developer CLI if you want environment orchestration.

Commands (Bicep example):

```powershell
az bicep install
az deployment group what-if --resource-group <rg-name> --template-file infra/main.bicep --parameters @infra/prod.parameters.json
az deployment group create --resource-group <rg-name> --template-file infra/main.bicep --parameters @infra/prod.parameters.json
```

### Step 3: Externalize secrets and config

What to do:

1. Move JWT signing key and database credentials out of appsettings files.
2. Store secrets in Azure Key Vault.
3. Configure apps to read settings from environment and secret references.

Tools:

- Azure Key Vault.
- Azure App Service configuration or container environment variables.

Commands:

```powershell
az keyvault create --name <kv-name> --resource-group <rg-name> --location <region>
az keyvault secret set --vault-name <kv-name> --name JwtKey --value "<secret-value>"
az keyvault secret set --vault-name <kv-name> --name SqlConnectionString --value "<secret-value>"
```

### Step 4: Production migration pipeline

What to do:

1. Keep startup auto-migrations disabled in production.
2. Add pre-deploy migration job in CI/CD.
3. Add rollback SQL scripts for critical schema changes.

Tools:

- dotnet ef CLI.
- GitHub Actions or Azure DevOps pipelines.

Commands:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations list --project ClinicManagementSystem.Data --startup-project ClinicManagementSystem.API
dotnet ef database update --project ClinicManagementSystem.Data --startup-project ClinicManagementSystem.API
```

### Step 5: Baseline observability

What to do:

1. Enable structured logging for API and Blazor.
2. Emit request latency, error rates, and dependency failures.
3. Add dashboards and alerts for key indicators.

Tools:

- Application Insights.
- Log Analytics and Azure Monitor alerts.

Starter metrics:

1. Availability.
2. P95 latency.
3. 5xx error rate.
4. DB dependency duration.
5. Queue lag (when queue is introduced).

### Step 6: Define SLOs and alert thresholds

What to do:

1. Set measurable SLOs.
2. Map each SLO to alert rules and action groups.

Example SLOs:

1. API availability >= 99.9% monthly.
2. P95 latency <= 500 ms for core endpoints.
3. Auth success ratio >= 99.5%.

## Validation Checklist

1. IaC can rebuild environment from scratch.
2. No production secrets remain in source-controlled config files.
3. Production startup does not auto-apply schema changes.
4. Alerts fire in test scenarios.
