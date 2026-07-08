param(
	[string]$BaseUrl = 'http://localhost:5000'
)

function Wait-Api {
	param([string]$url)
	for ($i=0; $i -lt 20; $i++) {
		try {
			$r = Invoke-WebRequest -Uri $url -UseBasicParsing -Method Head -TimeoutSec 2 -ErrorAction Stop
			return $true
		} catch { Start-Sleep -Seconds 1 }
	}
	return $false
}

if (-not (Wait-Api -url "$BaseUrl/")) {
	Write-Host "API not responding at $BaseUrl - continuing anyway (endpoints may still respond)."
}

$timestamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
$email = "testuser+$timestamp@example.com"
$password = "P@ssw0rd!"

Write-Host "Using test user: $email"

$regBody = @{ Email = $email; Password = $password } | ConvertTo-Json
try {
	$reg = Invoke-RestMethod -Uri "$BaseUrl/auth/register" -Method Post -Body $regBody -ContentType 'application/json'
	Write-Host "Register response:`n" ($reg | ConvertTo-Json -Depth 5)
} catch {
	Write-Host "Register failed: $_"
}

$loginBody = @{ Email = $email; Password = $password } | ConvertTo-Json
try {
	$login = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method Post -Body $loginBody -ContentType 'application/json'
	Write-Host "Login response:`n" ($login | ConvertTo-Json -Depth 5)
	$access = $login.accessToken; $refresh = $login.refreshToken
} catch {
	Write-Host "Login failed: $_"
	exit 1
}

# Refresh
try {
	$refreshBody = @{ RefreshToken = $refresh } | ConvertTo-Json
	$refRes = Invoke-RestMethod -Uri "$BaseUrl/auth/refresh" -Method Post -Body $refreshBody -ContentType 'application/json'
	Write-Host "Refresh response:`n" ($refRes | ConvertTo-Json -Depth 5)
} catch {
	Write-Host "Refresh failed: $_"
}

# Revoke
try {
	$revokeBody = @{ RefreshToken = $refresh } | ConvertTo-Json
	$rv = Invoke-RestMethod -Uri "$BaseUrl/auth/revoke" -Method Post -Body $revokeBody -ContentType 'application/json'
	Write-Host "Revoke response: success"
} catch {
	Write-Host "Revoke failed or no-content: $_"
}

Write-Host "Test flow completed"
