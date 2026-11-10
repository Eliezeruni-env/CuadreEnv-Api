[CmdletBinding()]
param(
	[string]$ConnectionString = $env:ConnectionStrings__OnionCrud
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
	throw 'Configure ConnectionStrings__OnionCrud with a PostgreSQL connection before running integration tests.'
}

if ($ConnectionString -notmatch '(?i)(Host|Server)=') {
	throw 'ConnectionStrings__OnionCrud must be a PostgreSQL/Npgsql connection string.'
}

$env:ConnectionStrings__OnionCrud = $ConnectionString

dotnet test tests/Onion.IntegrationTests/Onion.IntegrationTests.csproj --no-restore
