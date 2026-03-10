# Getting Started

This guide walks you through setting up your local development environment for the Blazor Hybrid Case Worker App.

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | Build and run all projects |
| [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) | 2.60+ | Azure resource management |
| [Azure Functions Core Tools](https://learn.microsoft.com/azure/azure-functions/functions-run-local) | 4.x | Run Azure Functions locally |
| [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/) | Latest | IDE (VS required for MAUI debugging) |
| [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) | Latest | Local Blob Storage emulator |
| [Azure Cosmos DB Emulator](https://learn.microsoft.com/azure/cosmos-db/local-emulator) | Latest | Local Cosmos DB |

## Install .NET MAUI Workload

```bash
dotnet workload install maui
```

## Clone and Restore

```bash
git clone https://github.com/YOUR_ORG/BlazorHybrid.git
cd BlazorHybrid
dotnet restore BlazorHybrid.sln
```

## Configure Local Settings

### Azure Functions API

Create or update `src/BlazorHybrid.Api/local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "CosmosDbConnectionString": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
    "StorageConnectionString": "UseDevelopmentStorage=true"
  }
}
```

### MAUI App

Update the auth configuration in `src/BlazorHybrid.App/MauiProgram.cs`:

```csharp
var authConfig = new AuthConfiguration
{
    ClientId = "YOUR_CLIENT_ID",       // From Entra ID app registration
    TenantId = "YOUR_TENANT_ID",       // Your Azure AD tenant
    Scopes = ["api://YOUR_API_CLIENT_ID/.default"],
    RedirectUri = "msauth://com.companyname.blazorhybrid.app"
};
```

## Run Locally

### 1. Start Azurite (Blob Storage Emulator)

```bash
azurite --silent --location ./azurite-data
```

### 2. Start Cosmos DB Emulator

Launch the Azure Cosmos DB Emulator from the Start menu (Windows) or run the Docker container:

```bash
docker run -p 8081:8081 -p 10250-10255:10250-10255 mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
```

### 3. Start the Azure Functions API

```bash
cd src/BlazorHybrid.Api
func start
```

The API will start at `http://localhost:7071`.

### 4. Run the MAUI App

#### Windows
```bash
dotnet build src/BlazorHybrid.App -t:Run -f net10.0-windows10.0.19041.0
```

#### Android (emulator or device)
```bash
dotnet build src/BlazorHybrid.App -t:Run -f net10.0-android
```

#### Using Visual Studio
1. Open `BlazorHybrid.sln`
2. Set `BlazorHybrid.App` as the startup project
3. Select your target platform (Windows Machine, Android Emulator, etc.)
4. Press F5

## Run Tests

```bash
dotnet test BlazorHybrid.sln --verbosity normal
```

## Project Structure

```
BlazorHybrid/
├── src/
│   ├── BlazorHybrid.App/      # MAUI Blazor Hybrid client
│   ├── BlazorHybrid.Shared/   # Shared DTOs and enums
│   └── BlazorHybrid.Api/      # Azure Functions API
├── tests/                      # Unit and integration tests
├── infra/                      # Bicep infrastructure templates
├── docs/                       # Documentation
└── .github/workflows/          # CI/CD pipelines
```

## Next Steps

- Read the [Architecture Guide](architecture.md) to understand the system design
- Follow the [Deployment Guide](deployment.md) to deploy to Azure
- Check the [User Guide](user-guide.md) for end-user documentation
