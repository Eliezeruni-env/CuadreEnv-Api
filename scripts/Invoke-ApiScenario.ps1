[CmdletBinding()]
param(
	[string]$BaseUrl = "http://127.0.0.1:5199",
	[string]$Email,
	[string]$Password,
	[string]$CompanyId = "1",
	[string]$Token,
	[switch]$ReadSweep,
	[switch]$FullSweep,
 [switch]$AllowDestructive,
 [string]$ArtifactDirectory = "artifacts\api-scenario"
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$BaseUrl = $BaseUrl.TrimEnd('/')
$results = [System.Collections.Generic.List[object]]::new()
$repo = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$artifactPath = Join-Path $repo $ArtifactDirectory
$customerId = $null
$categoryId = $null

function Add-Result([string]$Name, [bool]$Passed, [string]$Detail) {
	[void]$results.Add([pscustomobject]@{ Name = $Name; Passed = $Passed; Detail = $Detail })
}

function Invoke-FullOperationSweep {
	$swagger = Assert-Http "Swagger for operation sweep" "GET" "/swagger/v1/swagger.json" @(200)
	$document = $swagger.Body | ConvertFrom-Json
	$safeStatuses = @(200, 201, 202, 204, 400, 401, 403, 404, 405, 415, 422, 501)
	foreach ($pathProperty in $document.paths.psobject.Properties) {
		$path = [string]$pathProperty.Name
		if ($path.StartsWith('/swagger') -or $path.StartsWith('/hubs') -or $path.StartsWith('/internal')) { continue }
		foreach ($operationProperty in $pathProperty.Value.psobject.Properties) {
			$method = $operationProperty.Name.ToUpperInvariant()
			if ($method -notin @('GET', 'POST', 'PUT', 'PATCH', 'DELETE')) { continue }
			if ($path.Contains('{')) {
				Add-Result "$method $path" $true "Discovered; skipped because route parameters require scenario data"
				continue
			}
			if ($method -eq 'GET') {
				$response = Invoke-Http $method $path
				if ($safeStatuses -notcontains $response.Status) { throw "$method $path returned unexpected HTTP $($response.Status): $($response.Body)" }
				Add-Result "$method $path" $true "HTTP $($response.Status)"
				continue
			}
			$response = Invoke-Http $method $path
			if ($safeStatuses -notcontains $response.Status) { throw "$method $path returned unexpected HTTP $($response.Status): $($response.Body)" }
			Add-Result "$method $path" $true "Contract probe HTTP $($response.Status); no mutation body sent"
		}
	}
}

function Invoke-ReadSweep {
	$swagger = Assert-Http "Swagger for read sweep" "GET" "/swagger/v1/swagger.json" @(200)
	$document = $swagger.Body | ConvertFrom-Json
	foreach ($pathProperty in $document.paths.psobject.Properties) {
		$path = [string]$pathProperty.Name
		if ($path.Contains('{') -or $path.StartsWith('/swagger') -or $path.StartsWith('/hubs') -or $path.StartsWith('/internal')) { continue }
		$operation = $pathProperty.Value.get
		if ($null -eq $operation) { continue }
		$response = Invoke-Http "GET" $path
		if (@(200, 204, 400, 401, 403, 404, 405, 501) -notcontains $response.Status) {
			throw "Read sweep failed at GET $path with HTTP $($response.Status): $($response.Body)"
		}
		Add-Result "GET $path" $true "HTTP $($response.Status)"
	}
}

function Invoke-Http([string]$Method, [string]$Path, [object]$Body = $null) {
	$headers = @{ Authorization = "Bearer $Token" }
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

function Assert-Http([string]$Name, [string]$Method, [string]$Path, [int[]]$Expected, [object]$Body = $null) {
	$response = Invoke-Http $Method $Path $Body
	if ($Expected -notcontains $response.Status) { throw "$Name returned HTTP $($response.Status): $($response.Body)" }
	Add-Result $Name $true "HTTP $($response.Status)"
	return $response
}

function Get-Id([string]$Body, [string]$Property = "id") {
 $value = $Body | ConvertFrom-Json
 $candidates = @($value, $value.data, $value.result)
 foreach ($candidate in $candidates) {
  if ($null -eq $candidate) { continue }
  foreach ($name in @($Property, "id", "customerId", "categoryId")) {
   $propertyValue = $candidate.$name
   if ($null -ne $propertyValue -and "$propertyValue" -match '^\d+$') { return [int]$propertyValue }
  }
 }
	throw "No se encontró '$Property' en la respuesta: $Body"
}

try {
	if (-not $AllowDestructive) { throw "Este runner crea, modifica y elimina datos. Use -AllowDestructive explícitamente." }
	if (-not $BaseUrl.StartsWith("http://127.0.0.1") -and -not $BaseUrl.StartsWith("http://localhost")) {
		throw "Por seguridad, el runner solo permite una API local."
	}
	New-Item -ItemType Directory -Path $artifactPath -Force | Out-Null

	if ([string]::IsNullOrWhiteSpace($Token)) {
		if (-not $Email -or -not $Password) { throw "Proporcione -Token o ambos -Email y -Password." }
		try {
			$tokenRequest = @{ email = $Email; password = $Password; deviceId = "backend-scenario" } | ConvertTo-Json
			$tokenResponse = Invoke-WebRequest -Uri "$BaseUrl/v1/auth/login" -Method Post -Body $tokenRequest -ContentType "application/json" -UseBasicParsing
			if ([int]$tokenResponse.StatusCode -ne 200) { throw "No se pudo obtener el token de Development." }
			$Token = ($tokenResponse.Content | ConvertFrom-Json).accessToken
		}
		catch { throw "No se pudo iniciar sesión con el usuario de pruebas: $($_.Exception.Message)" }
	}

	Assert-Http "Session" "GET" "/v1/auth/me" @(200) | Out-Null
	Assert-Http "Session modules" "GET" "/v1/auth/me/modules" @(200) | Out-Null
	Assert-Http "Roles" "GET" "/v1/roles" @(200, 403) | Out-Null
	Assert-Http "Users" "GET" "/v1/users?page=1&pageSize=5" @(200, 403) | Out-Null
	Assert-Http "Approvals" "GET" "/v1/approvals/pending" @(200, 403) | Out-Null
	Assert-Http "Customers list" "GET" "/v1/customer?pageNumber=1&pageSize=5" @(200) | Out-Null
	Assert-Http "Categories list" "GET" "/v1/category" @(200) | Out-Null
	Assert-Http "Products list" "GET" "/v1/product/paged?PageNumber=1&PageSize=5" @(200, 403) | Out-Null
	if ($ReadSweep) { Invoke-ReadSweep }
	if ($FullSweep) { Invoke-FullOperationSweep }

	$suffix = Get-Date -Format "yyyyMMddHHmmssfff"
	$customer = @{
		name = "API Flow Customer $suffix"
		identification = "FLOW-$suffix"
		phone = "0000000000"
		email = "api-flow-$suffix@local"
		companyId = [int]$CompanyId
		isGeneric = $false
	}
	$createdCustomer = Assert-Http "Customer create" "POST" "/v1/customer" @(201, 200) $customer
	$customerId = Get-Id $createdCustomer.Body
	Assert-Http "Customer read" "GET" "/v1/customer/$customerId" @(200) | Out-Null
	$customerUpdate = $customer.Clone()
	$customerUpdate.name = "$($customer.name) Updated"
	$customerUpdate.id = $customerId
	Assert-Http "Customer update" "PUT" "/v1/customer" @(204, 200) $customerUpdate | Out-Null
	Assert-Http "Customer read after update" "GET" "/v1/customer/$customerId" @(200) | Out-Null
	Assert-Http "Customer delete" "DELETE" "/v1/customer/$customerId" @(204, 200) | Out-Null
	$customerId = $null

	$category = @{ description = "API Flow Category $suffix" }
	$createdCategory = Assert-Http "Category create" "POST" "/v1/category" @(201, 200) $category
	$categoryId = Get-Id $createdCategory.Body
	Assert-Http "Category read" "GET" "/v1/category/$categoryId" @(200) | Out-Null
	Assert-Http "Category delete" "DELETE" "/v1/category/$categoryId" @(204, 200) | Out-Null
	$categoryId = $null
}
catch {
	Add-Result "Scenario" $false $_.Exception.Message
	Write-Error $_.Exception.Message
	throw
}
finally {
	if ($customerId) { try { Invoke-Http "DELETE" "/v1/customer/$customerId" | Out-Null } catch { } }
	if ($categoryId) { try { Invoke-Http "DELETE" "/v1/category/$categoryId" | Out-Null } catch { } }
	New-Item -ItemType Directory -Path $artifactPath -Force | Out-Null
	$results | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $artifactPath "summary.json") -Encoding UTF8
	Write-Host "`nAPI scenario summary" -ForegroundColor Cyan
	$results | Format-Table -AutoSize
	Write-Host "Summary file: $(Join-Path $artifactPath 'summary.json')"
	if ($results | Where-Object { -not $_.Passed }) { Write-Host "Result: FAILED" -ForegroundColor Red }
	else { Write-Host "Result: PASSED" -ForegroundColor Green }
}
