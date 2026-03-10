<#
.SYNOPSIS
    Deploys the BlazorHybrid Azure infrastructure using Bicep.

.DESCRIPTION
    PowerShell deployment helper that wraps 'az deployment group create' to deploy
    all Bicep modules for the BlazorHybrid offline-first backend.

.PARAMETER Environment
    Target environment: dev, staging, or prod.

.PARAMETER Location
    Azure region for resource deployment.

.PARAMETER AppName
    Base application name used in resource naming (3-16 chars).

.PARAMETER ResourceGroup
    Azure resource group name. Created if it does not exist.

.PARAMETER WhatIf
    Run in what-if mode to preview changes without deploying.

.EXAMPLE
    .\deploy.ps1 -Environment dev -Location eastus2 -AppName blazorhybrid -ResourceGroup rg-blazorhybrid-dev

.EXAMPLE
    .\deploy.ps1 -Environment prod -Location eastus2 -AppName blazorhybrid -ResourceGroup rg-blazorhybrid-prod -WhatIf
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment = 'dev',

    [Parameter(Mandatory = $false)]
    [string]$Location = 'eastus2',

    [Parameter(Mandatory = $false)]
    [ValidateLength(3, 16)]
    [string]$AppName = 'blazorhybrid',

    [Parameter(Mandatory = $false)]
    [string]$ResourceGroup = '',

    [Parameter(Mandatory = $false)]
    [switch]$UseParamFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Resolve paths ────────────────────────────────────────────────────────────

$infraDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$mainBicep = Join-Path $infraDir 'main.bicep'
$paramFile = Join-Path $infraDir 'main.bicepparam'

if (-not (Test-Path $mainBicep)) {
    Write-Error "Bicep file not found: $mainBicep"
    exit 1
}

# ── Defaults ─────────────────────────────────────────────────────────────────

if ([string]::IsNullOrWhiteSpace($ResourceGroup)) {
    $ResourceGroup = "rg-$AppName-$Environment"
}

$deploymentName = "blazorhybrid-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

# ── Pre-flight checks ───────────────────────────────────────────────────────

Write-Host ''
Write-Host '╔══════════════════════════════════════════════════════════════╗' -ForegroundColor Cyan
Write-Host '║          BlazorHybrid Infrastructure Deployment             ║' -ForegroundColor Cyan
Write-Host '╚══════════════════════════════════════════════════════════════╝' -ForegroundColor Cyan
Write-Host ''
Write-Host "  Environment   : $Environment" -ForegroundColor Yellow
Write-Host "  Location      : $Location" -ForegroundColor Yellow
Write-Host "  App Name      : $AppName" -ForegroundColor Yellow
Write-Host "  Resource Group: $ResourceGroup" -ForegroundColor Yellow
Write-Host "  Deployment    : $deploymentName" -ForegroundColor Yellow
Write-Host "  What-If       : $($WhatIf.IsPresent)" -ForegroundColor Yellow
Write-Host ''

# Verify Azure CLI is installed and logged in
try {
    $account = az account show 2>&1 | ConvertFrom-Json
    Write-Host "  Subscription  : $($account.name) ($($account.id))" -ForegroundColor Green
    Write-Host ''
}
catch {
    Write-Error 'Azure CLI is not installed or you are not logged in. Run "az login" first.'
    exit 1
}

# ── Ensure resource group exists ─────────────────────────────────────────────

$rgExists = az group exists --name $ResourceGroup 2>&1
if ($rgExists -eq 'false') {
    Write-Host "Creating resource group '$ResourceGroup' in '$Location'..." -ForegroundColor Cyan
    az group create --name $ResourceGroup --location $Location --tags "application=$AppName" "environment=$Environment" "managedBy=bicep" | Out-Null
    Write-Host "  Resource group created." -ForegroundColor Green
}
else {
    Write-Host "  Resource group '$ResourceGroup' already exists." -ForegroundColor Green
}

# ── Build deployment arguments ───────────────────────────────────────────────

$deployArgs = @(
    'deployment', 'group', 'create'
    '--resource-group', $ResourceGroup
    '--name', $deploymentName
    '--template-file', $mainBicep
    '--verbose'
)

if ($UseParamFile -and (Test-Path $paramFile)) {
    $deployArgs += @('--parameters', "@$paramFile")
    Write-Host "  Using parameter file: $paramFile" -ForegroundColor Cyan
}
else {
    $deployArgs += @(
        '--parameters'
        "location=$Location"
        "environmentName=$Environment"
        "appName=$AppName"
    )
}

# ── Deploy ───────────────────────────────────────────────────────────────────

if ($WhatIf.IsPresent) {
    Write-Host ''
    Write-Host 'Running what-if analysis...' -ForegroundColor Cyan
    Write-Host ''

    $whatIfArgs = @(
        'deployment', 'group', 'what-if'
        '--resource-group', $ResourceGroup
        '--template-file', $mainBicep
    )

    if ($UseParamFile -and (Test-Path $paramFile)) {
        $whatIfArgs += @('--parameters', "@$paramFile")
    }
    else {
        $whatIfArgs += @(
            '--parameters'
            "location=$Location"
            "environmentName=$Environment"
            "appName=$AppName"
        )
    }

    az @whatIfArgs
    exit 0
}

Write-Host ''
Write-Host 'Starting deployment...' -ForegroundColor Cyan
Write-Host ''

$startTime = Get-Date
az @deployArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Deployment '$deploymentName' failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

$duration = (Get-Date) - $startTime

Write-Host ''
Write-Host '╔══════════════════════════════════════════════════════════════╗' -ForegroundColor Green
Write-Host '║                  Deployment Succeeded!                      ║' -ForegroundColor Green
Write-Host '╚══════════════════════════════════════════════════════════════╝' -ForegroundColor Green
Write-Host ''
Write-Host "  Duration: $($duration.ToString('mm\:ss'))" -ForegroundColor Green
Write-Host ''

# ── Show outputs ─────────────────────────────────────────────────────────────

Write-Host 'Deployment outputs:' -ForegroundColor Cyan
az deployment group show `
    --resource-group $ResourceGroup `
    --name $deploymentName `
    --query 'properties.outputs' `
    --output table

Write-Host ''
