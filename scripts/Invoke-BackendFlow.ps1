[CmdletBinding()]
param(
	[string]$BaseUrl = "http://127.0.0.1:5199",
	[ValidateSet("Development", "Test")]
	[string]$Environment = "Development",
 [ValidateSet("Debug", "Release")]
 [string]$Configuration = "Debug",
	[string]$Email,
	[string]$Password,
	[string]$AccessToken,
	[string]$CompanyId = "1",
	[switch]$SkipMigrations,
	[switch]$SkipTests,
	[switch]$RunScenario,
	[switch]$ReadSweep,
	[switch]$FullSweep,
 [switch]$AllowDestructive,
 [string]$ArtifactDirectory = "artifacts\backend-flow"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$BaseUrl = $BaseUrl.TrimEnd('/')
$repo = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$solution = Join-Path $repo "crud-onion.slnx"
$apiProject = Join-Path $repo "crud-onion\crud-onion.csproj"
$testProject = Join-Path $repo "tests\Onion.Tests\Onion.Tests.csproj"
$results = [System.Collections.Generic.List[object]]::new()
$apiProcess = $null
$oldEnvironment = $env:ASPNETCORE_ENVIRONMENT
$logDirectory = Join-Path $repo $ArtifactDirectory

function Add-Result([string]$Name, [bool]$Passed, [string]$Detail) {
	[void]$results.Add([pscustomobject]@{ Name = $Name; Passed = $Passed; Detail = $Detail })
}

function Invoke-CommandStep([string]$Name, [scriptblock]$Action) {
	$started = Get-Date
	try {
		& $Action
		if ($LASTEXITCODE -ne 0) { throw "Exit code $LASTEXITCODE" }
		Add-Result $Name $true ((Get-Date) - $started).ToString()
	}
	catch {
		Add-Result $Name $false $_.Exception.Message
		throw
	}
}

function Invoke-Http([string]$Method, [string]$Path, [string]$Token = $null, [object]$Body = $null) {
	$headers = @{}
	if ($Token) { $headers["Authorization"] = "Bearer $Token" }
	$params = @{ Uri = "$BaseUrl$Path"; Method = $Method; Headers = $headers; UseBasicParsing = $true }
	if ($null -ne $Body) {
		$params.Body = $Body | ConvertTo-Json -Depth 20 -Compress
		$params.ContentType = "application/json"
	}
	try {
		$response = Invoke-WebRequest @params
		[pscustomobject]@{ Status = [int]$response.StatusCode; Body = [string]$response.Content }
	}
	catch {
		$response = $_.Exception.Response
		if ($null -eq $response) { throw }
		$body = $response.GetResponseStream()
		$reader = [System.IO.StreamReader]::new($body)
		try { $content = $reader.ReadToEnd() } finally { $reader.Dispose(); $body.Dispose() }
		[pscustomobject]@{ Status = [int]$response.StatusCode; Body = $content }
	}
}

function Assert-Http([string]$Name, [string]$Method, [string]$Path, [int[]]$Expected, [string]$Token = $null, [object]$Body = $null) {
	$response = Invoke-Http $Method $Path $Token $Body
	if ($Expected -notcontains $response.Status) {
		throw "$Name returned HTTP $($response.Status): $($response.Body)"
	}
	Add-Result $Name $true "HTTP $($response.Status)"
	return $response
}

try {
	foreach ($requiredPath in @($solution, $apiProject, $testProject)) {
		if (-not (Test-Path $requiredPath)) { throw "Required path was not found: $requiredPath" }
	}
	if (-not $BaseUrl.StartsWith("http://127.0.0.1") -and -not $BaseUrl.StartsWith("http://localhost")) {
		throw "Por seguridad, el flujo completo solo permite BaseUrl local. Use -BaseUrl http://127.0.0.1:puerto si es un entorno controlado."
	}

	Invoke-CommandStep "Restore" { & dotnet restore $solution }
	Invoke-CommandStep "Build" { & dotnet build $solution --configuration $Configuration --no-restore }

	if (-not $SkipMigrations) {
		Invoke-CommandStep "Migrations" {
			& dotnet ef database update --project (Join-Path $repo "DataAccess\Onion.DataAccess.csproj") --startup-project $apiProject --configuration $Configuration --no-build
		}
	}
	else { Add-Result "Migrations" $true "Skipped by parameter" }

	if (-not $SkipTests) {
		Invoke-CommandStep "Tests" { & dotnet test $testProject --configuration $Configuration --no-build --no-restore }
	}
	else { Add-Result "Tests" $true "Skipped by parameter" }

	$env:ASPNETCORE_ENVIRONMENT = $Environment
	New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
	$stdoutLog = Join-Path $logDirectory "api.stdout.log"
	$stderrLog = Join-Path $logDirectory "api.stderr.log"
	Remove-Item $stdoutLog, $stderrLog -Force -ErrorAction SilentlyContinue
	$apiProcess = Start-Process dotnet -ArgumentList @("run", "--project", $apiProject, "--no-build", "--no-restore", "--configuration", $Configuration, "--urls", $BaseUrl) -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog
	$ready = $false
	for ($attempt = 1; $attempt -le 120; $attempt++) {
		Start-Sleep -Milliseconds 500
		if ($apiProcess.HasExited) {
			$startupError = if (Test-Path $stderrLog) { Get-Content $stderrLog -Raw } else { "No stderr log" }
			throw "La API terminó durante el arranque con exit code $($apiProcess.ExitCode). $startupError"
		}
		try {
			$health = Invoke-Http "GET" "/hc"
			if ($health.Status -eq 200) { $ready = $true; break }
		} catch { }
	}
	if (-not $ready) {
		$startupOutput = if (Test-Path $stdoutLog) { Get-Content $stdoutLog -Raw } else { "No stdout log" }
		$startupError = if (Test-Path $stderrLog) { Get-Content $stderrLog -Raw } else { "No stderr log" }
		throw "La API no respondió al health check en $BaseUrl/hc.`nSTDOUT:`n$startupOutput`nSTDERR:`n$startupError"
	}
	Add-Result "Start API" $true "PID $($apiProcess.Id)"

	Assert-Http "Root" "GET" "/" @(200) | Out-Null
	Assert-Http "Version" "GET" "/v1" @(200) | Out-Null
	Assert-Http "Swagger" "GET" "/swagger/v1/swagger.json" @(200) | Out-Null
	Assert-Http "Modules requires authentication" "GET" "/v1/auth/me/modules" @(401) | Out-Null

	$token = $AccessToken
	if ([string]::IsNullOrWhiteSpace($token) -and $Email -and $Password) {
		$tokenResponse = Assert-Http "Login" "POST" "/v1/auth/login" @(200) $null @{ email = $Email; password = $Password; deviceId = "backend-flow" }
		$token = ($tokenResponse.Body | ConvertFrom-Json).accessToken
		if ([string]::IsNullOrWhiteSpace($token)) { throw "Login response did not contain accessToken" }
	}
	if ($token) {
		Assert-Http "Authenticated modules" "GET" "/v1/auth/me/modules" @(200) $token | Out-Null
		Assert-Http "Roles" "GET" "/v1/roles" @(200, 403) $token | Out-Null
		Assert-Http "Users" "GET" "/v1/users?page=1&pageSize=1" @(200, 403) $token | Out-Null
		Assert-Http "Approvals" "GET" "/v1/approvals/pending" @(200, 403) $token | Out-Null
	}
	else {
		Add-Result "Authenticated endpoint checks" $true "Skipped; provide -AccessToken or -Email/-Password"
	}

	if ($RunScenario) {
		if (-not $AllowDestructive) { throw "-RunScenario requiere -AllowDestructive porque ejecuta operaciones de escritura y eliminación." }
		if (-not $token) { throw "-RunScenario requiere -AccessToken o credenciales válidas mediante -Email y -Password." }
		$scenarioArgs = @("-BaseUrl", $BaseUrl, "-CompanyId", $CompanyId, "-Token", $token, "-AllowDestructive")
		if ($ReadSweep -or $FullSweep) { $scenarioArgs += "-ReadSweep" }
		if ($FullSweep) { $scenarioArgs += "-FullSweep" }
		& (Join-Path $PSScriptRoot "Invoke-ApiScenario.ps1") @scenarioArgs
		if ($LASTEXITCODE -ne 0) { throw "El escenario de API falló con exit code $LASTEXITCODE" }
		Add-Result "CRUD scenario" $true "Completed"
	}
}
catch {
	Add-Result "Flow" $false $_.Exception.Message
	Write-Error $_.Exception.Message
	exit 1
}
finally {
	if ($apiProcess -and -not $apiProcess.HasExited) {
		Stop-Process -Id $apiProcess.Id -Force
		Add-Result "Stop API" $true "PID $($apiProcess.Id) stopped"
	}
	$env:ASPNETCORE_ENVIRONMENT = $oldEnvironment
	$summaryPath = Join-Path $logDirectory "summary.json"
	$results | ConvertTo-Json -Depth 5 | Set-Content -Path $summaryPath -Encoding UTF8
	Write-Host "`nBackend flow summary" -ForegroundColor Cyan
	$results | Format-Table -AutoSize
	Write-Host "Summary file: $summaryPath"
	if ($results | Where-Object { -not $_.Passed }) { Write-Host "Result: FAILED" -ForegroundColor Red }
	else { Write-Host "Result: PASSED" -ForegroundColor Green }
}
