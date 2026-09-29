Param(
	[int]$Port = 52510,
	[string]$JwtKey = "cuadreenv_super_secret_jwt_key_2026",
	[string]$Environment = "Development",
	[string]$FrontendUrl = "http://localhost:3000"
)

# Resolve repo root assuming script lives in <repo>/scripts
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Resolve-Path (Join-Path $scriptDir '..')
Push-Location $repoRoot

Write-Host "Starting API in $Environment on http://localhost:$Port"
Write-Host "Jwt:Key set to: $JwtKey"

# Export environment variables for the child process
$env:ASPNETCORE_URLS = "http://*:$Port"
$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:Jwt__Key = $JwtKey
$env:Frontend__Url = $FrontendUrl

# Run the project
try {
	& dotnet run --project .\crud-onion\crud-onion.csproj
}
finally {
	Pop-Location
}
