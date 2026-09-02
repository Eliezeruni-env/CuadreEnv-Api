Write-Host "=== PREPARING DEV ENVIRONMENT FOR SAAS API ===" -ForegroundColor Cyan

# 1. Kill all processes named crud-onion
$p = Get-Process -Name "crud-onion" -ErrorAction SilentlyContinue
if ($p) {
    Write-Host "Killing existing crud-onion processes..." -ForegroundColor Yellow
    $p | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# 2. Kill any process listening on port 5160
$conn = Get-NetTCPConnection -LocalPort 5160 -ErrorAction SilentlyContinue
if ($conn) {
    foreach ($c in $conn) {
        $pidToKill = $c.OwningProcess
        if ($pidToKill -gt 0) {
            Write-Host "Killing process on port 5160 (PID: $pidToKill)..." -ForegroundColor Yellow
            Stop-Process -Id $pidToKill -Force -ErrorAction SilentlyContinue
        }
    }
    Start-Sleep -Seconds 2
}

# 3. Kill iisexpress if running
$iis = Get-Process -Name iisexpress -ErrorAction SilentlyContinue
if ($iis) {
    Write-Host "Stopping IIS Express..." -ForegroundColor Yellow
    Stop-Process -Name iisexpress -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
}

# 4. Clean project build artifacts
Write-Host "Cleaning build cache..." -ForegroundColor Gray
dotnet clean "c:\Users\pelie\source\repos\MyDev\cuadreEnv\api\crud-onion\crud-onion.csproj"

# 5. Start dotnet watch
Write-Host "Launching API with Hot Reload (dotnet watch)..." -ForegroundColor Green
dotnet watch run --project "c:\Users\pelie\source\repos\MyDev\cuadreEnv\api\crud-onion\crud-onion.csproj" --launch-profile http
