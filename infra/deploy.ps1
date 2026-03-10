<#
.SYNOPSIS
    Deploys the Blazor PWA infrastructure to Azure using Bicep.

.PARAMETER Environment
    Target environment: dev, staging, or prod.

.PARAMETER ResourceGroupName
    Name of the Azure resource group.

.PARAMETER Location
    Azure region for deployment.

.PARAMETER SqlAdminPassword
    SQL Server admin password (will prompt if not provided).

.EXAMPLE
    .\deploy.ps1 -Environment dev -ResourceGroupName rg-blazorpwa-dev -Location eastus
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,

    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $false)]
    [string]$Location = 'eastus',

    [Parameter(Mandatory = $false)]
    [securestring]$SqlAdminPassword
)

$ErrorActionPreference = 'Stop'

Write-Host "=== Blazor PWA Infrastructure Deployment ===" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow

# Prompt for SQL password if not provided
if (-not $SqlAdminPassword) {
    $SqlAdminPassword = Read-Host -Prompt "Enter SQL Admin Password" -AsSecureString
}

# Ensure logged in to Azure
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in to Azure. Running 'az login'..." -ForegroundColor Yellow
    az login
}

Write-Host "`nCurrent subscription: $($account.name) ($($account.id))" -ForegroundColor Green

# Create resource group if it doesn't exist
Write-Host "`nEnsuring resource group exists..." -ForegroundColor Cyan
az group create --name $ResourceGroupName --location $Location --output none

# Deploy Bicep template
Write-Host "`nDeploying Bicep template..." -ForegroundColor Cyan
$plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword)
)

$deploymentName = "blazorpwa-$Environment-$(Get-Date -Format 'yyyyMMddHHmmss')"

az deployment group create `
    --name $deploymentName `
    --resource-group $ResourceGroupName `
    --template-file "$PSScriptRoot\main.bicep" `
    --parameters "$PSScriptRoot\parameters\$Environment.bicepparam" `
    --parameters sqlAdminPassword=$plainPassword `
    --output table

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nDeployment FAILED!" -ForegroundColor Red
    exit 1
}

Write-Host "`nDeployment completed successfully!" -ForegroundColor Green

# Show outputs
Write-Host "`n=== Deployment Outputs ===" -ForegroundColor Cyan
az deployment group show `
    --name $deploymentName `
    --resource-group $ResourceGroupName `
    --query properties.outputs `
    --output table
