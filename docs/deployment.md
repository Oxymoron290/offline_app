# Deployment Guide

This guide covers deploying the Blazor Hybrid Case Worker App to Azure.

## Prerequisites

- Azure subscription with Owner or Contributor access
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) installed and authenticated
- [Bicep CLI](https://learn.microsoft.com/azure/azure-resource-manager/bicep/install) (included with Azure CLI 2.20+)
- .NET 10 SDK installed

## Step 1: Azure Login

```bash
az login
az account set --subscription "YOUR_SUBSCRIPTION_ID"
```

## Step 2: Deploy Infrastructure with Bicep

### Using the Deploy Script

```powershell
cd infra
.\scripts\deploy.ps1 -ResourceGroupName "rg-blazorhybrid-dev" -Location "eastus2" -EnvironmentName "dev"
```

### Manual Deployment

```bash
# Create resource group
az group create --name rg-blazorhybrid-dev --location eastus2

# Deploy Bicep templates
az deployment group create \
  --resource-group rg-blazorhybrid-dev \
  --template-file infra/main.bicep \
  --parameters infra/main.bicepparam \
  --parameters environmentName=dev

# View outputs
az deployment group show \
  --resource-group rg-blazorhybrid-dev \
  --name main \
  --query properties.outputs
```

### Deployed Resources

| Resource | Purpose |
|----------|---------|
| Azure Cosmos DB (Serverless) | Entity and media metadata storage |
| Azure Storage Account | Blob storage for photos, videos, documents |
| Azure Function App (Consumption) | REST API backend |
| Azure Key Vault | Secret management |
| Application Insights | Monitoring and diagnostics |
| Log Analytics Workspace | Log aggregation |

## Step 3: Configure Entra ID App Registration

Entra ID app registrations cannot be fully automated with Bicep. Follow these steps:

### API App Registration

1. Go to [Azure Portal > Microsoft Entra ID > App registrations](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps)
2. Click **New registration**
3. Name: `BlazorHybrid-API-{env}`
4. Supported account types: **Single tenant**
5. Click **Register**
6. Under **Expose an API**:
   - Set Application ID URI: `api://{client-id}`
   - Add a scope: `access_as_user` (Admins and users)
7. Note the **Application (client) ID** and **Directory (tenant) ID**

### Client App Registration

1. Create another registration: `BlazorHybrid-Client-{env}`
2. Under **Authentication**:
   - Add platform: **Mobile and desktop applications**
   - Redirect URI: `msauth://com.companyname.blazorhybrid.app`
   - Enable: Allow public client flows = **Yes**
3. Under **API permissions**:
   - Add permission > My APIs > `BlazorHybrid-API-{env}` > `access_as_user`
   - Grant admin consent

### Update Function App Settings

```bash
az functionapp config appsettings set \
  --resource-group rg-blazorhybrid-dev \
  --name func-blazorhybrid-dev \
  --settings \
    "AzureAd__TenantId=YOUR_TENANT_ID" \
    "AzureAd__ClientId=YOUR_API_CLIENT_ID" \
    "AzureAd__Audience=api://YOUR_API_CLIENT_ID"
```

## Step 4: Deploy the Azure Functions API

### Manual Deploy

```bash
cd src/BlazorHybrid.Api
dotnet publish --configuration Release --output ./publish

# Deploy to Azure Functions
func azure functionapp publish func-blazorhybrid-dev
```

### Via GitHub Actions

The `deploy-api.yml` workflow handles this automatically:
1. Set up GitHub repository secrets:
   - `AZURE_CLIENT_ID` — Service principal client ID
   - `AZURE_TENANT_ID` — Azure AD tenant ID
   - `AZURE_SUBSCRIPTION_ID` — Azure subscription ID
   - `AZURE_FUNCTION_APP_NAME` — Function App name
2. Trigger the workflow from GitHub Actions tab

## Step 5: Build and Distribute the MAUI App

### Update App Configuration

Edit `src/BlazorHybrid.App/MauiProgram.cs` and update:

```csharp
var authConfig = new AuthConfiguration
{
    ClientId = "YOUR_CLIENT_APP_ID",
    TenantId = "YOUR_TENANT_ID",
    Scopes = ["api://YOUR_API_CLIENT_ID/.default"],
    RedirectUri = "msauth://com.companyname.blazorhybrid.app"
};

// Update API base address
client.BaseAddress = new Uri("https://func-blazorhybrid-dev.azurewebsites.net");
```

### Build for Windows

```bash
dotnet publish src/BlazorHybrid.App -f net10.0-windows10.0.19041.0 -c Release
```

### Build for Android

```bash
dotnet publish src/BlazorHybrid.App -f net10.0-android -c Release
```

The APK/AAB will be in `src/BlazorHybrid.App/bin/Release/net10.0-android/publish/`.

### Build for iOS

```bash
dotnet publish src/BlazorHybrid.App -f net10.0-ios -c Release
```

> **Note**: iOS builds require a macOS machine with Xcode installed.

## Step 6: Verify Deployment

```bash
# Test the API health
curl https://func-blazorhybrid-dev.azurewebsites.net/api/entities?caseWorkerId=test

# Check Function App logs
az monitor app-insights query \
  --app appi-blazorhybrid-dev \
  --analytics-query "traces | take 10"
```

## Environment Promotion

Deploy to staging/production by changing the environment name:

```powershell
.\infra\scripts\deploy.ps1 -ResourceGroupName "rg-blazorhybrid-staging" -Location "eastus2" -EnvironmentName "staging"
.\infra\scripts\deploy.ps1 -ResourceGroupName "rg-blazorhybrid-prod" -Location "eastus2" -EnvironmentName "prod"
```

Or use the GitHub Actions `deploy-infra.yml` workflow with the environment selector.
