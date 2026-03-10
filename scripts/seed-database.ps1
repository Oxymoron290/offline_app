<#
.SYNOPSIS
    Seeds the BlazorPWA database with sample data.

.DESCRIPTION
    Creates sample inspection reports and related data for development and testing.
#>

$ErrorActionPreference = 'Stop'
$rootDir = Split-Path -Parent $PSScriptRoot

Write-Host "=== Seeding BlazorPWA Database ===" -ForegroundColor Cyan
Write-Host ""

# Run EF Core migration first
Write-Host "Applying database migrations..." -ForegroundColor Yellow
dotnet ef database update --project "$rootDir\src\BlazorPWA.API"
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Migration failed!" -ForegroundColor Red
    exit 1
}
Write-Host "  ✅ Migrations applied" -ForegroundColor Green

Write-Host ""

# Seed data via API
Write-Host "Seeding sample data..." -ForegroundColor Yellow

$apiBase = "https://localhost:7001"

$sampleReports = @(
    @{
        id = [guid]::NewGuid().ToString()
        title = "Warehouse Safety Inspection"
        description = "Annual safety inspection of the main warehouse facility"
        facilityName = "Main Distribution Warehouse"
        facilityAddress = "123 Industrial Blvd, Suite 100"
        inspectorName = "Jane Smith"
        inspectorId = "EMP-001"
        status = "Draft"
        notes = "Scheduled for routine annual review"
    },
    @{
        id = [guid]::NewGuid().ToString()
        title = "Office Fire Safety Audit"
        description = "Quarterly fire safety compliance check"
        facilityName = "Corporate Office Building"
        facilityAddress = "456 Business Park Dr"
        inspectorName = "John Doe"
        inspectorId = "EMP-002"
        status = "In Progress"
        notes = "Fire extinguisher inspections pending"
    },
    @{
        id = [guid]::NewGuid().ToString()
        title = "Restaurant Health Inspection"
        description = "Monthly food safety and hygiene inspection"
        facilityName = "Downtown Cafe"
        facilityAddress = "789 Main Street"
        inspectorName = "Jane Smith"
        inspectorId = "EMP-001"
        status = "Completed"
        notes = "All areas passed inspection"
    }
)

Write-Host "  Note: Make sure the API is running (.\scripts\run-local.ps1) before seeding." -ForegroundColor Yellow
Write-Host ""

foreach ($report in $sampleReports) {
    try {
        $json = $report | ConvertTo-Json -Depth 3
        $response = Invoke-RestMethod -Uri "$apiBase/api/reports" -Method Post -Body $json -ContentType "application/json" -SkipCertificateCheck
        Write-Host "  ✅ Created: $($report.title)" -ForegroundColor Green
    }
    catch {
        Write-Host "  ⚠️  Could not create: $($report.title) - $($_.Exception.Message)" -ForegroundColor Yellow
        Write-Host "     Make sure the API is running first." -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=== Seeding Complete ===" -ForegroundColor Green
