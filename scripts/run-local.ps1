<#
.SYNOPSIS
    Runs all BlazorPWA services locally for development.

.DESCRIPTION
    Starts the API, Client, and Worker projects in separate processes.
    Press Ctrl+C to stop all services.
#>

$ErrorActionPreference = 'Stop'
$rootDir = Split-Path -Parent $PSScriptRoot

Write-Host "=== Starting BlazorPWA Local Development ===" -ForegroundColor Cyan
Write-Host ""

# Store process IDs for cleanup
$processes = @()

try {
    # Start API
    Write-Host "Starting API server..." -ForegroundColor Yellow
    $apiProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList "run", "--project", "$rootDir\src\BlazorPWA.API\BlazorPWA.API.csproj", "--launch-profile", "https" `
        -PassThru -NoNewWindow
    $processes += $apiProcess
    Write-Host "  ✅ API started (PID: $($apiProcess.Id))" -ForegroundColor Green

    Start-Sleep -Seconds 3

    # Start Worker
    Write-Host "Starting Worker..." -ForegroundColor Yellow
    $workerProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList "run", "--project", "$rootDir\src\BlazorPWA.Worker\BlazorPWA.Worker.csproj" `
        -PassThru -NoNewWindow
    $processes += $workerProcess
    Write-Host "  ✅ Worker started (PID: $($workerProcess.Id))" -ForegroundColor Green

    Start-Sleep -Seconds 2

    # Start Client
    Write-Host "Starting Client PWA..." -ForegroundColor Yellow
    $clientProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList "run", "--project", "$rootDir\src\BlazorPWA.Client\BlazorPWA.Client.csproj" `
        -PassThru -NoNewWindow
    $processes += $clientProcess
    Write-Host "  ✅ Client started (PID: $($clientProcess.Id))" -ForegroundColor Green

    Write-Host ""
    Write-Host "=== All services running ===" -ForegroundColor Green
    Write-Host "  API:    https://localhost:7001" -ForegroundColor Cyan
    Write-Host "  Client: https://localhost:5001" -ForegroundColor Cyan
    Write-Host "  Worker: Running in background" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Press Ctrl+C to stop all services..." -ForegroundColor Yellow

    # Wait for any process to exit
    while ($true) {
        foreach ($proc in $processes) {
            if ($proc.HasExited) {
                Write-Host "Process $($proc.Id) has exited with code $($proc.ExitCode)" -ForegroundColor Yellow
            }
        }
        Start-Sleep -Seconds 5
    }
}
finally {
    Write-Host ""
    Write-Host "Stopping all services..." -ForegroundColor Yellow
    foreach ($proc in $processes) {
        if (-not $proc.HasExited) {
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            Write-Host "  Stopped PID $($proc.Id)" -ForegroundColor Gray
        }
    }
    Write-Host "All services stopped." -ForegroundColor Green
}
