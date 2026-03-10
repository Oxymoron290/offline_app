# Deployment Guide

Step-by-step guide to deploy the BlazorWASM_PWA application to Azure.

---

## Prerequisites

- **Azure subscription** with permissions to create resources (Contributor or Owner role)
- **Azure CLI** installed and authenticated:
  ```bash
  az login
  az account set --subscription "<your-subscription-id>"
  ```
- **GitHub repository** with the application code pushed
- **.NET 10 SDK** installed locally (for running migrations)
- **PowerShell 7+** (for running deployment scripts)

---

## Step 1: Register Entra ID Application

The application uses Microsoft Entra ID (Azure AD) for authentication. You need to create an app registration.

### 1.1 Create the App Registration

1. Navigate to the [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID** → **App registrations** → **New registration**

2. Configure the registration:
   - **Name:** `BlazorWASM-PWA` (or your preferred name)
   - **Supported account types:** Choose based on your requirements:
     - *Single tenant* — for internal organizational use
     - *Multitenant* — if case workers span multiple organizations
   - **Redirect URI:** Select **Single-page application (SPA)** and add:
     - `https://<your-swa-hostname>.azurestaticapps.net/authentication/login-callback`
     - `http://localhost:4280/authentication/login-callback` (for local development)

3. Click **Register**

### 1.2 Note the Application IDs

After registration, record these values (you will need them later):

| Value | Location |
|-------|----------|
| **Application (client) ID** | App registration → Overview |
| **Directory (tenant) ID** | App registration → Overview |

### 1.3 Configure API Permissions

1. Go to **API permissions** → **Add a permission**
2. Select **Microsoft Graph** → **Delegated permissions**
3. Add:
   - `User.Read` (sign in and read user profile)
   - `offline_access` (maintain access to data you have given it access to)
4. Click **Grant admin consent** (if you have admin privileges)

### 1.4 Expose an API (Optional — if API requires token validation)

1. Go to **Expose an API** → **Set** the Application ID URI (e.g., `api://<client-id>`)
2. **Add a scope:**
   - Scope name: `access_as_user`
   - Admin consent display name: `Access BlazorWASM PWA API`
   - Admin consent description: `Allow the application to access the API on behalf of the signed-in user`
   - State: **Enabled**

3. Under **Authorized client applications**, add your client app ID with the `access_as_user` scope.

### 1.5 Configure Client Application Settings

Update the Blazor client `wwwroot/appsettings.json` (or create it):

```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/<tenant-id>",
    "ClientId": "<client-id>",
    "ValidateAuthority": true
  }
}
```

---

## Step 2: Deploy Infrastructure with Bicep

The `infra/` directory contains Bicep templates that provision all required Azure resources.

### Resources Created

| Resource | Purpose |
|----------|---------|
| Azure Static Web App | Hosts Blazor WASM client and Azure Functions API |
| Azure SQL Database | Persistent data storage |
| Azure SQL Server | Logical SQL Server |
| Azure Storage Account | Blob storage for photos, videos, documents |
| Azure Key Vault | Securely store secrets (connection strings, keys) |
| Application Insights | Monitoring and telemetry |
| Log Analytics Workspace | Centralized logging |

### 2.1 Deploy Using the Deployment Script

```powershell
# From the repository root
.\infra\scripts\deploy.ps1 `
  -ResourceGroupName "rg-blazorpwa-dev" `
  -Location "eastus2" `
  -Environment "dev" `
  -SqlAdminPassword (Read-Host -AsSecureString "SQL Admin Password")
```

### 2.2 Deploy Using Azure CLI Directly

```bash
# Create a resource group
az group create --name rg-blazorpwa-dev --location eastus2

# Deploy the Bicep template
az deployment group create \
  --resource-group rg-blazorpwa-dev \
  --template-file infra/modules/main.bicep \
  --parameters environment=dev \
  --parameters sqlAdminPassword='<your-secure-password>' \
  --parameters entraIdClientId='<client-id>' \
  --parameters entraIdTenantId='<tenant-id>'
```

### 2.3 Verify Infrastructure Deployment

```bash
# List deployed resources
az resource list --resource-group rg-blazorpwa-dev --output table
```

Confirm all expected resources are present and in a healthy state.

---

## Step 3: Configure Static Web App

### 3.1 Link to GitHub Repository

1. Navigate to the **Azure Portal** → your Static Web App resource
2. Go to **Settings** → **Deployment** → connect to your GitHub repository
3. Configure:
   - **Organization:** Your GitHub org
   - **Repository:** `BlazorWASM_PWA`
   - **Branch:** `main`

Alternatively, this is handled automatically if you deployed via Bicep with the repository URL parameter.

### 3.2 Set Application Settings

Configure environment variables for the Static Web App (these are passed to Azure Functions):

```bash
az staticwebapp appsettings set \
  --name <swa-name> \
  --resource-group rg-blazorpwa-dev \
  --setting-names \
    "SqlConnectionString=Server=tcp:<sql-server>.database.windows.net,1433;Database=BlazorPWA;Authentication=Active Directory Default;" \
    "BlobStorageConnectionString=DefaultEndpointsProtocol=https;AccountName=<storage-account>;AccountKey=<key>;EndpointSuffix=core.windows.net" \
    "AzureAd__TenantId=<tenant-id>" \
    "AzureAd__ClientId=<client-id>" \
    "AzureAd__Audience=api://<client-id>" \
    "KeyVaultUri=https://<keyvault-name>.vault.azure.net/"
```

> **Best practice:** Store sensitive values (SQL connection string, storage keys) in Key Vault and reference them in app settings using Key Vault references: `@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/<secret-name>/)`.

### 3.3 Configure Authentication in `staticwebapp.config.json`

Ensure the `staticwebapp.config.json` in the client `wwwroot/` directory is configured:

```json
{
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["authenticated"]
    }
  ],
  "responseOverrides": {
    "401": {
      "statusCode": 302,
      "redirect": "/.auth/login/aad"
    }
  },
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/css/*", "/js/*", "/api/*", "/_framework/*", "/icon-*.png", "/favicon.png"]
  }
}
```

---

## Step 4: Deploy Application Code

### 4.1 Automatic Deployment via GitHub Actions

The repository includes a CI/CD workflow at `.github/workflows/deploy.yml` that:

- **Triggers on push to `main`** — deploys to production
- **Triggers on pull requests to `main`** — deploys to a staging environment
- **Cancels in-progress runs** — prevents redundant deployments

The workflow:

1. Checks out code
2. Sets up .NET 10 SDK
3. Restores dependencies
4. Publishes the Blazor WASM client (`dotnet publish`)
5. Builds the Azure Functions API (`dotnet build`)
6. Deploys to Azure Static Web Apps using the deployment token

**Required GitHub Secret:**

Set `AZURE_STATIC_WEB_APPS_API_TOKEN` in your repository:

1. Go to Azure Portal → Static Web App → **Manage deployment token** → Copy
2. Go to GitHub → Repository → **Settings** → **Secrets and variables** → **Actions**
3. Create a new secret: `AZURE_STATIC_WEB_APPS_API_TOKEN` with the copied token

Once configured, every push to `main` triggers an automatic deployment.

### 4.2 Manual Deployment via SWA CLI

For one-off deployments without GitHub Actions:

```bash
# Build the client
dotnet publish src/BlazorWASM_PWA.Client -c Release -o output

# Build the API
dotnet build src/BlazorWASM_PWA.Api -c Release -o api_output

# Deploy to Azure Static Web Apps
swa deploy output/wwwroot \
  --api-location src/BlazorWASM_PWA.Api \
  --deployment-token <your-deployment-token> \
  --env production
```

---

## Step 5: Run EF Core Migrations Against Azure SQL

After deploying the infrastructure and before the first use, apply database migrations to Azure SQL.

### 5.1 Allow Your IP Through the Firewall

```bash
# Get your public IP
$myIp = (Invoke-RestMethod -Uri "https://api.ipify.org")

# Add a temporary firewall rule
az sql server firewall-rule create \
  --resource-group rg-blazorpwa-dev \
  --server <sql-server-name> \
  --name AllowMyIP \
  --start-ip-address $myIp \
  --end-ip-address $myIp
```

### 5.2 Apply Migrations

```bash
cd src/BlazorWASM_PWA.Api

# Set the connection string for the Azure SQL Database
$env:SqlConnectionString="Server=tcp:<sql-server>.database.windows.net,1433;Database=BlazorPWA;User ID=<admin-user>;Password=<admin-password>;Encrypt=true;TrustServerCertificate=false;"

dotnet ef database update
```

### 5.3 Clean Up Firewall Rule

```bash
az sql server firewall-rule delete \
  --resource-group rg-blazorpwa-dev \
  --server <sql-server-name> \
  --name AllowMyIP
```

---

## Step 6: Verify Deployment

### 6.1 Health Check

Navigate to the Static Web App URL:

```
https://<your-swa-hostname>.azurestaticapps.net
```

Verify:
- [ ] The application loads in the browser
- [ ] The service worker installs and activates (check DevTools → Application → Service Workers)
- [ ] The PWA manifest loads correctly (check DevTools → Application → Manifest)

### 6.2 Test Authentication

1. Click the **Login** button
2. You should be redirected to the Microsoft Entra ID login page
3. Sign in with a valid account
4. You should be redirected back to the application, authenticated

### 6.3 Test API Connectivity

```bash
# Health check endpoint (if implemented)
curl https://<your-swa-hostname>.azurestaticapps.net/api/health

# Authenticated API call (requires a valid Bearer token)
curl -H "Authorization: Bearer <token>" \
  https://<your-swa-hostname>.azurestaticapps.net/api/sync?since=2024-01-01T00:00:00Z
```

### 6.4 Test Blob Storage

Upload a test file through the application and verify it appears in the Azure Storage Account's blob container.

---

## Custom Domain Setup

### 1. Add a Custom Domain

```bash
az staticwebapp hostname set \
  --name <swa-name> \
  --resource-group rg-blazorpwa-dev \
  --hostname app.yourdomain.com
```

### 2. Configure DNS

Add a CNAME record with your DNS provider:

| Type | Name | Value |
|------|------|-------|
| CNAME | `app` | `<your-swa-hostname>.azurestaticapps.net` |

Azure Static Web Apps automatically provisions and manages TLS certificates for custom domains.

### 3. Update Entra ID Redirect URIs

Add the new redirect URI to your app registration:
- `https://app.yourdomain.com/authentication/login-callback`

---

## Environment-Specific Configurations

Manage multiple environments (dev, staging, production) using separate resource groups and configurations.

### Naming Convention

| Environment | Resource Group | SQL Server | Storage Account |
|-------------|---------------|------------|-----------------|
| Development | `rg-blazorpwa-dev` | `sql-blazorpwa-dev` | `stblazorpwadev` |
| Staging | `rg-blazorpwa-stg` | `sql-blazorpwa-stg` | `stblazorpwastg` |
| Production | `rg-blazorpwa-prod` | `sql-blazorpwa-prod` | `stblazorpwaprod` |

### Deploy to a Specific Environment

```powershell
# Development
.\infra\scripts\deploy.ps1 -ResourceGroupName "rg-blazorpwa-dev" -Environment "dev" -Location "eastus2"

# Staging
.\infra\scripts\deploy.ps1 -ResourceGroupName "rg-blazorpwa-stg" -Environment "stg" -Location "eastus2"

# Production
.\infra\scripts\deploy.ps1 -ResourceGroupName "rg-blazorpwa-prod" -Environment "prod" -Location "eastus2"
```

### GitHub Actions Environment Branches

| Branch | Deployment Target |
|--------|-------------------|
| `main` | Production |
| Pull requests to `main` | Staging (preview environments) |

The `deploy.yml` workflow handles this automatically — pull requests create staging environments that are torn down when the PR is closed.

### Client-Side Environment Configuration

Use environment-specific `appsettings.{Environment}.json` files in `wwwroot/`:

**`wwwroot/appsettings.Development.json`:**
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/<dev-tenant-id>",
    "ClientId": "<dev-client-id>"
  },
  "ApiBaseUrl": "http://localhost:4280"
}
```

**`wwwroot/appsettings.Production.json`:**
```json
{
  "AzureAd": {
    "Authority": "https://login.microsoftonline.com/<prod-tenant-id>",
    "ClientId": "<prod-client-id>"
  },
  "ApiBaseUrl": "https://app.yourdomain.com"
}
```

---

## Deployment Checklist

Use this checklist for production deployments:

- [ ] Entra ID app registration created with correct redirect URIs
- [ ] Infrastructure deployed via Bicep (all resources healthy)
- [ ] GitHub Actions secret `AZURE_STATIC_WEB_APPS_API_TOKEN` configured
- [ ] Application settings configured on Static Web App
- [ ] Sensitive secrets stored in Key Vault
- [ ] EF Core migrations applied to Azure SQL
- [ ] Custom domain configured (if applicable)
- [ ] TLS certificate provisioned (automatic with SWA)
- [ ] Application loads and service worker activates
- [ ] Authentication flow works end-to-end
- [ ] API endpoints respond correctly
- [ ] Blob upload/download works
- [ ] Offline functionality verified (load app, go offline, create records)
- [ ] Sync functionality verified (go back online, records sync to server)
- [ ] Application Insights receiving telemetry

---

## Related Documentation

- [Getting Started](GETTING-STARTED.md) — Local development setup
- [Architecture](ARCHITECTURE.md) — System design and offline-first patterns
- [Operations Guide](OPERATIONS.md) — Monitoring and troubleshooting
- [User Guide](USER-GUIDE.md) — End-user documentation
