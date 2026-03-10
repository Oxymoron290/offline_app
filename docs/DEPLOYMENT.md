# Deployment Guide

This guide covers deploying the BlazorPWA field service application to Azure, from initial infrastructure provisioning through CI/CD pipeline setup.

---

## Table of Contents

- [Azure Subscription Setup](#azure-subscription-setup)
- [Infrastructure Deployment with Bicep](#infrastructure-deployment-with-bicep)
- [Application Deployment](#application-deployment)
- [GitHub Actions CI/CD Setup](#github-actions-cicd-setup)
- [Database Migration](#database-migration)
- [Environment Configuration](#environment-configuration)
- [Post-Deployment Verification](#post-deployment-verification)

---

## Azure Subscription Setup

### Required Permissions

The deploying user or service principal needs the following Azure RBAC roles:

| Role | Scope | Purpose |
|------|-------|---------|
| **Contributor** | Resource Group | Create and manage all Azure resources |
| **User Access Administrator** | Resource Group | Assign Managed Identity roles |
| **Key Vault Administrator** | Key Vault resource | Manage secrets and access policies |

### Resource Providers

Ensure the following resource providers are registered on your subscription:

```powershell
az provider register --namespace Microsoft.Web
az provider register --namespace Microsoft.Sql
az provider register --namespace Microsoft.ServiceBus
az provider register --namespace Microsoft.Storage
az provider register --namespace Microsoft.KeyVault
az provider register --namespace Microsoft.Insights
az provider register --namespace Microsoft.OperationalInsights
```

Verify registration status:

```powershell
az provider show --namespace Microsoft.Web --query "registrationState" --output tsv
```

### Cost Estimation

Approximate monthly costs by environment (USD, East US region):

| Resource | Dev | Staging | Production |
|----------|-----|---------|------------|
| App Service Plan (API) | ~$13 (B1) | ~$55 (S1) | ~$146 (P1v3) |
| App Service Plan (Client) | ~$13 (B1) | ~$55 (S1) | ~$146 (P1v3) |
| App Service Plan (Worker) | ~$13 (B1) | ~$55 (S1) | ~$146 (P1v3) |
| Azure SQL Database | ~$5 (Basic 5 DTU) | ~$15 (S0 10 DTU) | ~$75 (S2 50 DTU) |
| Azure Service Bus | ~$10 (Standard) | ~$10 (Standard) | ~$668 (Premium 1 MU) |
| Azure Blob Storage | ~$2 (LRS, 50 GB) | ~$5 (LRS, 100 GB) | ~$20 (GRS, 500 GB) |
| Key Vault | ~$0.03 | ~$0.03 | ~$0.03 |
| Application Insights | ~$2 (5 GB) | ~$5 (10 GB) | ~$12 (25 GB) |
| **Total** | **~$58/month** | **~$200/month** | **~$1,213/month** |

> **Note:** These are estimates. Actual costs depend on usage, data volume, and scaling configuration. Use the [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/) for precise estimates.

---

## Infrastructure Deployment with Bicep

### Prerequisites

```powershell
# Ensure Azure CLI is installed and updated
az --version

# Ensure Bicep CLI is available (bundled with Azure CLI 2.20+)
az bicep version

# If Bicep needs updating
az bicep upgrade
```

### Step-by-Step Deployment

#### 1. Login to Azure

```powershell
az login
```

#### 2. Set the target subscription

```powershell
az account set --subscription "your-subscription-id"

# Verify
az account show --query "{name:name, id:id}" --output table
```

#### 3. Create the resource group

```powershell
# Development
az group create --name rg-blazorpwa-dev --location eastus

# Staging
az group create --name rg-blazorpwa-staging --location eastus

# Production
az group create --name rg-blazorpwa-prod --location eastus
```

#### 4. Deploy infrastructure

```powershell
# Deploy to development environment
az deployment group create `
  --resource-group rg-blazorpwa-dev `
  --template-file infra\main.bicep `
  --parameters infra\parameters\dev.bicepparam `
  --parameters sqlAdminPassword='YourSecurePassword123!'
```

For staging or production, use the appropriate parameter file:

```powershell
# Staging
az deployment group create `
  --resource-group rg-blazorpwa-staging `
  --template-file infra\main.bicep `
  --parameters infra\parameters\staging.bicepparam `
  --parameters sqlAdminPassword='YourSecurePassword123!'

# Production
az deployment group create `
  --resource-group rg-blazorpwa-prod `
  --template-file infra\main.bicep `
  --parameters infra\parameters\prod.bicepparam `
  --parameters sqlAdminPassword='YourSecurePassword123!'
```

### Using the Helper Script

A convenience script wraps the deployment with validation and output:

```powershell
.\infra\deploy.ps1 -Environment dev -ResourceGroupName rg-blazorpwa-dev
.\infra\deploy.ps1 -Environment staging -ResourceGroupName rg-blazorpwa-staging
.\infra\deploy.ps1 -Environment prod -ResourceGroupName rg-blazorpwa-prod
```

The script performs:
1. Parameter validation
2. `az bicep build` to verify template syntax
3. `az deployment group what-if` to preview changes
4. `az deployment group create` to apply the deployment
5. Outputs the deployed resource names and endpoints

### What Gets Deployed

The Bicep template provisions the following resources:

| Resource | Naming Convention | Description |
|----------|-------------------|-------------|
| App Service Plan (API) | `plan-api-blazorpwa-{env}` | Hosts the API App Service |
| App Service (API) | `app-api-blazorpwa-{env}` | ASP.NET Core Minimal API |
| App Service Plan (Client) | `plan-client-blazorpwa-{env}` | Hosts the Client App Service |
| App Service (Client) | `app-client-blazorpwa-{env}` | Blazor WASM PWA static site |
| App Service Plan (Worker) | `plan-worker-blazorpwa-{env}` | Hosts the Worker App Service |
| App Service (Worker) | `app-worker-blazorpwa-{env}` | Background media processor |
| Azure SQL Server | `sql-blazorpwa-{env}` | Logical SQL Server |
| Azure SQL Database | `sqldb-blazorpwa-{env}` | Application database |
| Service Bus Namespace | `sb-blazorpwa-{env}` | Message broker namespace |
| Service Bus Queue | `media-processing` | Media processing job queue |
| Storage Account | `stblazorpwa{env}` | Blob storage for media files |
| Blob Containers | `photos`, `videos`, `documents`, `staging` | Media storage containers |
| Key Vault | `kv-blazorpwa-{env}` | Secrets and configuration |
| Application Insights | `appi-blazorpwa-{env}` | Telemetry and monitoring |
| Log Analytics Workspace | `log-blazorpwa-{env}` | Log aggregation for App Insights |

### Verifying Deployment

```powershell
# List all resources in the resource group
az resource list --resource-group rg-blazorpwa-dev --output table

# Verify App Service is running
az webapp show --resource-group rg-blazorpwa-dev --name app-api-blazorpwa-dev --query "state" --output tsv

# Verify SQL Server
az sql server show --resource-group rg-blazorpwa-dev --name sql-blazorpwa-dev --query "state" --output tsv

# Verify Service Bus
az servicebus namespace show --resource-group rg-blazorpwa-dev --name sb-blazorpwa-dev --query "status" --output tsv
```

---

## Application Deployment

### Manual Deployment

Use these steps for one-off deployments or when CI/CD is not yet configured.

#### 1. Build and publish all projects

```powershell
# API
dotnet publish src\BlazorPWA.API -c Release -o publish\api

# Client (Blazor WASM)
dotnet publish src\BlazorPWA.Client -c Release -o publish\client

# Worker
dotnet publish src\BlazorPWA.Worker -c Release -o publish\worker
```

#### 2. Create deployment packages

```powershell
# Create zip packages for Azure deployment
Compress-Archive -Path publish\api\* -DestinationPath publish\api.zip -Force
Compress-Archive -Path publish\client\* -DestinationPath publish\client.zip -Force
Compress-Archive -Path publish\worker\* -DestinationPath publish\worker.zip -Force
```

#### 3. Deploy to Azure App Service

```powershell
# Deploy API
az webapp deploy `
  --resource-group rg-blazorpwa-dev `
  --name app-api-blazorpwa-dev `
  --src-path publish\api.zip `
  --type zip

# Deploy Client
az webapp deploy `
  --resource-group rg-blazorpwa-dev `
  --name app-client-blazorpwa-dev `
  --src-path publish\client.zip `
  --type zip

# Deploy Worker
az webapp deploy `
  --resource-group rg-blazorpwa-dev `
  --name app-worker-blazorpwa-dev `
  --src-path publish\worker.zip `
  --type zip
```

#### 4. Restart services (if needed)

```powershell
az webapp restart --resource-group rg-blazorpwa-dev --name app-api-blazorpwa-dev
az webapp restart --resource-group rg-blazorpwa-dev --name app-worker-blazorpwa-dev
```

---

## GitHub Actions CI/CD Setup

### Repository Secrets

Configure the following secrets in your GitHub repository under **Settings → Secrets and variables → Actions**:

| Secret Name | Description | How to Obtain |
|-------------|-------------|---------------|
| `AZURE_CREDENTIALS` | Service principal JSON for Azure login | `az ad sp create-for-rbac` output (see below) |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID | `az account show --query id --output tsv` |
| `SQL_ADMIN_PASSWORD` | SQL Server admin password | Set during initial deployment |

### Creating the Service Principal

```powershell
# Create a service principal with Contributor role scoped to your resource group
az ad sp create-for-rbac `
  --name "sp-blazorpwa-cicd" `
  --role contributor `
  --scopes /subscriptions/{subscription-id}/resourceGroups/rg-blazorpwa-dev `
  --sdk-auth
```

The output JSON should be stored as the `AZURE_CREDENTIALS` secret:

```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

> **Security note:** For production, consider using [OIDC-based authentication](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure) (federated credentials) instead of client secrets.

### Workflow Descriptions

The repository includes three GitHub Actions workflows:

#### `deploy-infra.yml` — Infrastructure Deployment

**Trigger:** Manual dispatch (`workflow_dispatch`) or push to `infra/**` files on `main`.

**What it does:**
1. Checks out the repository.
2. Logs in to Azure using the service principal.
3. Validates the Bicep template (`az bicep build`).
4. Runs a what-if deployment preview.
5. Deploys the Bicep template to the target environment.
6. Outputs deployed resource names.

**Usage:**
```yaml
# Triggered automatically on push to infra/ on main
# Or manually via GitHub UI: Actions → Deploy Infrastructure → Run workflow
```

#### `deploy-app.yml` — Application Deployment

**Trigger:** Push to `main` branch (after merge), or manual dispatch.

**What it does:**
1. Checks out the repository.
2. Sets up .NET 8 SDK.
3. Restores packages and builds all projects.
4. Runs unit tests.
5. Publishes API, Client, and Worker projects.
6. Deploys each package to the corresponding Azure App Service.
7. Runs EF Core migrations against the target database.
8. Runs smoke tests against the deployed endpoints.

**Usage:**
```yaml
# Triggered automatically on push to main
# Or manually: Actions → Deploy Application → Run workflow
# Select environment: dev, staging, or prod
```

#### `pr-validation.yml` — Pull Request Validation

**Trigger:** Pull request opened or updated targeting `main`.

**What it does:**
1. Checks out the PR branch.
2. Restores packages.
3. Builds all projects (`dotnet build`).
4. Runs all unit tests (`dotnet test`).
5. Runs code analysis / linting.
6. Posts build and test results as a PR comment.

**Usage:**
```yaml
# Triggered automatically on PR to main
# Must pass before merge is allowed (configure as required check)
```

### Branch Protection Rules

Configure the following branch protection rules on `main`:

- Require pull request before merging
- Require status checks to pass: `pr-validation`
- Require branches to be up to date before merging
- Require at least 1 approving review

---

## Database Migration

### Running Migrations in CI/CD

The `deploy-app.yml` workflow includes a migration step:

```yaml
- name: Run EF Core Migrations
  run: |
    dotnet tool install --global dotnet-ef
    dotnet ef database update --project src/BlazorPWA.API --connection "${{ secrets.SQL_CONNECTION_STRING }}"
```

### Manual Migration

For manual migration against a local or remote database:

```powershell
# Local database
dotnet ef database update --project src\BlazorPWA.API

# Remote database (specify connection string)
dotnet ef database update --project src\BlazorPWA.API `
  --connection "Server=sql-blazorpwa-dev.database.windows.net;Database=sqldb-blazorpwa-dev;User Id=sqladmin;Password=YourPassword;Encrypt=True"
```

### Creating a New Migration

When the EF Core model changes:

```powershell
dotnet ef migrations add MigrationName --project src\BlazorPWA.API
```

### Rolling Back a Migration

```powershell
# Roll back to a specific migration
dotnet ef database update PreviousMigrationName --project src\BlazorPWA.API

# Remove the last migration (if not yet applied)
dotnet ef migrations remove --project src\BlazorPWA.API
```

### Migration Best Practices

- Always review generated migration SQL before applying to staging/production.
- Use `dotnet ef migrations script` to generate SQL scripts for DBA review.
- Never delete applied migrations from the Migrations folder.
- Test migrations against a copy of the production database before applying.

---

## Environment Configuration

### Key Vault Secrets

After infrastructure deployment, populate Key Vault with required secrets:

```powershell
$vaultName = "kv-blazorpwa-dev"

# SQL connection string (for fallback/EF migrations)
az keyvault secret set --vault-name $vaultName `
  --name "ConnectionStrings--DefaultConnection" `
  --value "Server=sql-blazorpwa-dev.database.windows.net;Database=sqldb-blazorpwa-dev;Authentication=Active Directory Managed Identity;Encrypt=True"

# Application Insights connection string
az keyvault secret set --vault-name $vaultName `
  --name "ApplicationInsights--ConnectionString" `
  --value "InstrumentationKey=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx;..."
```

> **Note:** Most Azure service connections use Managed Identity and don't require secrets. Key Vault is used for third-party keys, connection string overrides, and sensitive configuration.

### App Service Configuration

Configure App Service application settings:

```powershell
# API App Service settings
az webapp config appsettings set `
  --resource-group rg-blazorpwa-dev `
  --name app-api-blazorpwa-dev `
  --settings `
    ASPNETCORE_ENVIRONMENT=Development `
    KeyVault__VaultUri=https://kv-blazorpwa-dev.vault.azure.net/ `
    ServiceBus__Namespace=sb-blazorpwa-dev.servicebus.windows.net `
    BlobStorage__AccountName=stblazorpwadev `
    Cors__AllowedOrigins__0=https://app-client-blazorpwa-dev.azurewebsites.net

# Worker App Service settings
az webapp config appsettings set `
  --resource-group rg-blazorpwa-dev `
  --name app-worker-blazorpwa-dev `
  --settings `
    ASPNETCORE_ENVIRONMENT=Development `
    KeyVault__VaultUri=https://kv-blazorpwa-dev.vault.azure.net/ `
    ServiceBus__Namespace=sb-blazorpwa-dev.servicebus.windows.net `
    BlobStorage__AccountName=stblazorpwadev
```

### Environment-Specific Settings

| Setting | Dev | Staging | Production |
|---------|-----|---------|------------|
| `ASPNETCORE_ENVIRONMENT` | Development | Staging | Production |
| App Service Plan SKU | B1 | S1 | P1v3 |
| SQL Database tier | Basic (5 DTU) | Standard S0 | Standard S2 |
| Service Bus tier | Standard | Standard | Premium |
| Blob Storage redundancy | LRS | LRS | GRS |
| Logging level | Information | Warning | Warning |
| Detailed errors | Enabled | Enabled | Disabled |
| HTTPS only | Yes | Yes | Yes |
| Minimum TLS | 1.2 | 1.2 | 1.2 |

---

## Post-Deployment Verification

After deploying, verify that all services are healthy.

### 1. Health Check Endpoints

```powershell
# API health check
Invoke-RestMethod -Uri "https://app-api-blazorpwa-dev.azurewebsites.net/health" -Method Get

# Expected response:
# { "status": "Healthy", "checks": { "database": "Healthy", "serviceBus": "Healthy", "blobStorage": "Healthy" } }
```

### 2. Smoke Tests

```powershell
# Verify API is responding
Invoke-RestMethod -Uri "https://app-api-blazorpwa-dev.azurewebsites.net/api/reports" -Method Get

# Verify Client is serving
$response = Invoke-WebRequest -Uri "https://app-client-blazorpwa-dev.azurewebsites.net" -Method Get
$response.StatusCode  # Should be 200
```

### 3. Verify Service Bus Connectivity

```powershell
# Check queue exists and is active
az servicebus queue show `
  --resource-group rg-blazorpwa-dev `
  --namespace-name sb-blazorpwa-dev `
  --name media-processing `
  --query "{status:status, messageCount:countDetails.activeMessageCount}" `
  --output table
```

### 4. Verify Blob Storage Connectivity

```powershell
# List containers
az storage container list `
  --account-name stblazorpwadev `
  --auth-mode login `
  --query "[].name" `
  --output table
```

Expected containers: `photos`, `videos`, `documents`, `staging`.

### 5. Verify Database Connectivity

```powershell
# Test SQL connectivity from API
# The API health endpoint already tests this, but you can also:
az sql db show `
  --resource-group rg-blazorpwa-dev `
  --server sql-blazorpwa-dev `
  --name sqldb-blazorpwa-dev `
  --query "{status:status, maxSizeBytes:maxSizeBytes}" `
  --output table
```

### 6. End-to-End Verification

1. Open the Client URL in a browser: `https://app-client-blazorpwa-dev.azurewebsites.net`
2. Create a test inspection report.
3. Add a photo attachment.
4. Verify the report appears in the API: `GET /api/reports`.
5. Verify the photo was processed (check Blob Storage).
6. Check Application Insights for telemetry data.

### Deployment Checklist

- [ ] All resource providers registered
- [ ] Bicep deployment completed successfully
- [ ] Key Vault secrets populated
- [ ] App Service settings configured
- [ ] EF Core migrations applied
- [ ] API health check returns `Healthy`
- [ ] Client loads in browser
- [ ] Worker is running (check App Service logs)
- [ ] Service Bus queue exists and is Active
- [ ] Blob Storage containers exist
- [ ] End-to-end test (create report → sync → verify) passes
- [ ] Application Insights receiving telemetry
- [ ] GitHub Actions secrets configured
- [ ] Branch protection rules enabled

---

## Related Documentation

- [Getting Started](GETTING-STARTED.md) — Local development setup
- [Architecture Guide](ARCHITECTURE.md) — System design and data flows
- [Operations Guide](OPERATIONS.md) — Monitoring and troubleshooting
- [User Guide](USER-GUIDE.md) — End-user documentation
