<#
.SYNOPSIS
    Sets up the local development environment for BlazorPWA.

.DESCRIPTION
    Checks prerequisites, restores packages, sets up user secrets,
    and prepares the local database.
#>

$ErrorActionPreference = 'Stop'

Write-Host "=== BlazorPWA Local Development Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# .NET SDK
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ .NET SDK not found. Install from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    exit 1
}
Write-Host "  ✅ .NET SDK: $dotnetVersion" -ForegroundColor Green

# Node.js (optional)
$nodeVersion = node --version 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ Node.js: $nodeVersion" -ForegroundColor Green
} else {
    Write-Host "  ⚠️  Node.js not found (optional, needed for DexieJS tooling)" -ForegroundColor Yellow
}

# Azure CLI (optional)
$azVersion = az version --output tsv 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ Azure CLI installed" -ForegroundColor Green
} else {
    Write-Host "  ⚠️  Azure CLI not found (needed for deployment)" -ForegroundColor Yellow
}

Write-Host ""

# Restore NuGet packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
$rootDir = Split-Path -Parent $PSScriptRoot
dotnet restore "$rootDir\BlazorPWA.sln"
Write-Host "  ✅ Packages restored" -ForegroundColor Green

Write-Host ""

# Setup user secrets for API project
Write-Host "Setting up user secrets for API project..." -ForegroundColor Yellow
$apiProject = "$rootDir\src\BlazorPWA.API\BlazorPWA.API.csproj"

dotnet user-secrets init --project $apiProject 2>$null
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=BlazorPWA;Trusted_Connection=True;MultipleActiveResultSets=true" --project $apiProject
dotnet user-secrets set "ServiceBus:ConnectionString" "" --project $apiProject
dotnet user-secrets set "BlobStorage:ConnectionString" "UseDevelopmentStorage=true" --project $apiProject

Write-Host "  ✅ User secrets configured" -ForegroundColor Green
Write-Host "  ℹ️  Update ServiceBus:ConnectionString with your Azure Service Bus connection string" -ForegroundColor Cyan

Write-Host ""

# Install EF Core tools
Write-Host "Installing EF Core tools..." -ForegroundColor Yellow
dotnet tool install --global dotnet-ef 2>$null
if ($LASTEXITCODE -ne 0) {
    dotnet tool update --global dotnet-ef 2>$null
}
Write-Host "  ✅ EF Core tools installed" -ForegroundColor Green

Write-Host ""

# Build the solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build "$rootDir\BlazorPWA.sln" --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  ✅ Build successful" -ForegroundColor Green

Write-Host ""
Write-Host "=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Update Azure Service Bus connection string in user secrets"
Write-Host "  2. Run database migration: dotnet ef database update --project src\BlazorPWA.API"
Write-Host "  3. Start the application: .\scripts\run-local.ps1"
Write-Host ""
