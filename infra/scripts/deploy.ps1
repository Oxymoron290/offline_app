<#
.SYNOPSIS
    Deploys the LACO Blazor WASM PWA infrastructure to Azure.

.DESCRIPTION
    Creates the resource group (if needed) and deploys all Azure resources
    via the main.bicep template.

.PARAMETER ResourceGroupName
    Name of the Azure resource group.

.PARAMETER Location
    Azure region for the deployment (e.g., eastus2, westus2).

.PARAMETER Environment
    Target environment: dev, staging, or prod.

.PARAMETER ResourceNamePrefix
    Prefix for all resource names (default: laco).

.PARAMETER SqlAdminLogin
    SQL Server administrator login.

.PARAMETER SqlAdminPassword
    SQL Server administrator password (SecureString).

.EXAMPLE
    .\deploy.ps1 -ResourceGroupName "rg-laco-dev" -Location "eastus2" -Environment "dev"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $false)]
    [string]$Location = "eastus2",

    [Parameter(Mandatory = $false)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment = "dev",

    [Parameter(Mandatory = $false)]
    [string]$ResourceNamePrefix = "laco",

    [Parameter(Mandatory = $false)]
    [string]$SqlAdminLogin = "lacoadmin",

    [Parameter(Mandatory = $false)]
    [SecureString]$SqlAdminPassword
)

$ErrorActionPreference = "Stop"

# ─── Helper ─────────────────────────────────────────────────────────────────────

function Write-Step {
    param([string]$Message)
    Write-Host "`n>>> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "  [OK] $Message" -ForegroundColor Green
}

function Write-Failure {
    param([string]$Message)
    Write-Host "  [FAIL] $Message" -ForegroundColor Red
}

# ─── Pre-flight checks ─────────────────────────────────────────────────────────

Write-Step "Checking Azure CLI..."
try {
    $azVersion = az version --output json 2>$null | ConvertFrom-Json
    Write-Success "Azure CLI version $($azVersion.'azure-cli') detected."
}
catch {
    Write-Failure "Azure CLI is not installed or not in PATH. Install from https://aka.ms/installazurecli"
    exit 1
}

Write-Step "Verifying Azure login..."
try {
    $account = az account show --output json 2>$null | ConvertFrom-Json
    Write-Success "Logged in as $($account.user.name) (subscription: $($account.name))."
}
catch {
    Write-Failure "Not logged in to Azure. Run 'az login' first."
    exit 1
}

# ─── Prompt for SQL password if not provided ────────────────────────────────────

if (-not $SqlAdminPassword) {
    $SqlAdminPassword = Read-Host -Prompt "Enter SQL Admin Password" -AsSecureString
}

$sqlPasswordPlainText = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword)
)

if ([string]::IsNullOrWhiteSpace($sqlPasswordPlainText)) {
    Write-Failure "SQL admin password cannot be empty."
    exit 1
}

# ─── Create resource group ──────────────────────────────────────────────────────

Write-Step "Ensuring resource group '$ResourceGroupName' exists in '$Location'..."
$rgExists = az group exists --name $ResourceGroupName 2>$null
if ($rgExists -eq "false") {
    az group create --name $ResourceGroupName --location $Location --tags "environment=$Environment" "project=LACO" --output none
    Write-Success "Resource group '$ResourceGroupName' created."
}
else {
    Write-Success "Resource group '$ResourceGroupName' already exists."
}

# ─── Deploy Bicep template ──────────────────────────────────────────────────────

$templateFile = Join-Path $PSScriptRoot "..\main.bicep"
$templateFile = (Resolve-Path $templateFile).Path

Write-Step "Deploying infrastructure ($Environment) to '$ResourceGroupName'..."
Write-Host "  Template: $templateFile"

$deploymentName = "laco-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

try {
    $result = az deployment group create `
        --resource-group $ResourceGroupName `
        --name $deploymentName `
        --template-file $templateFile `
        --parameters `
            location=$Location `
            environmentName=$Environment `
            resourceNamePrefix=$ResourceNamePrefix `
            sqlAdminLogin=$SqlAdminLogin `
            sqlAdminPassword=$sqlPasswordPlainText `
        --output json 2>&1

    $deployment = $result | ConvertFrom-Json

    if ($deployment.properties.provisioningState -ne "Succeeded") {
        Write-Failure "Deployment finished with state: $($deployment.properties.provisioningState)"
        Write-Host ($result | Out-String)
        exit 1
    }

    Write-Success "Deployment '$deploymentName' succeeded."
}
catch {
    Write-Failure "Deployment failed: $_"
    exit 1
}

# ─── Output deployed resource info ──────────────────────────────────────────────

Write-Step "Deployment Outputs:"
$outputs = $deployment.properties.outputs

$outputTable = @(
    [PSCustomObject]@{ Resource = "Static Web App URL";     Value = $outputs.staticWebAppUrl.value }
    [PSCustomObject]@{ Resource = "Static Web App Hostname"; Value = $outputs.staticWebAppHostname.value }
    [PSCustomObject]@{ Resource = "SQL Server FQDN";        Value = $outputs.sqlServerFqdn.value }
    [PSCustomObject]@{ Resource = "SQL Database";           Value = $outputs.sqlDatabaseName.value }
    [PSCustomObject]@{ Resource = "Storage Account";        Value = $outputs.storageAccountName.value }
    [PSCustomObject]@{ Resource = "Blob Endpoint";          Value = $outputs.storageBlobEndpoint.value }
    [PSCustomObject]@{ Resource = "Key Vault";              Value = $outputs.keyVaultName.value }
    [PSCustomObject]@{ Resource = "Key Vault URI";          Value = $outputs.keyVaultUri.value }
)

$outputTable | Format-Table -AutoSize

# ─── Store deployment token securely ────────────────────────────────────────────

if ($outputs.staticWebAppDeploymentToken.value) {
    Write-Host "`n  SWA Deployment Token (for CI/CD): ***hidden***" -ForegroundColor Yellow
    Write-Host "  To retrieve it, run:" -ForegroundColor Yellow
    Write-Host "    az deployment group show -g $ResourceGroupName -n $deploymentName --query properties.outputs.staticWebAppDeploymentToken.value -o tsv" -ForegroundColor DarkYellow
}

Write-Host "`nDeployment complete!" -ForegroundColor Green

# Clear sensitive data from memory
$sqlPasswordPlainText = $null
[System.GC]::Collect()
