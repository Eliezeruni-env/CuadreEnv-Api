Frontend <-> Backend dev connectivity

This project supports two common local development flows. Pick ONE and follow the steps.

Option A — Visual Studio / IIS Express (HTTPS)

1. Backend
- Start the API from Visual Studio (IIS Express) or via dotnet run if you prefer the https Kestrel endpoint.
- Confirm the app is listening on an HTTPS port, e.g. https://localhost:44324 or https://localhost:7060 (see Properties/launchSettings.json).

2. Frontend
- Configure your FE base URL to point to the HTTPS backend:
  // src/environments/environment.ts
  export const environment = {
	production: false,
	apiUrl: 'https://localhost:44324'
  };

- (Optional) Add a proxy for local development to avoid CORS and cert issues:
  // proxy.conf.json
  {
	"/v1": {
	  "target": "https://localhost:44324",
	  "secure": false,
	  "changeOrigin": true,
	  "logLevel": "debug"
	},
	"/hc": {
	  "target": "https://localhost:44324",
	  "secure": false,
	  "changeOrigin": true
	}
  }

- Start the FE (Angular example):
  ng serve --proxy-config proxy.conf.json

3. Backend CORS
- Program.cs includes the local FE origins and calls app.UseCors("DefaultCors") before authentication/authorization.

Option B — Run API with Kestrel HTTP on port 8080

1. Backend
- Start the API on http://localhost:8080:
  dotnet run --project crud-onion\crud-onion.csproj
- (Alternatively) set environment variable and run:
  $env:ASPNETCORE_URLS = "http://localhost:8080;http://localhost:5160"
  dotnet run --project crud-onion\crud-onion.csproj

2. Frontend
- Configure FE base URL to http://localhost:8080:
  export const environment = {
	production: false,
	apiUrl: 'http://localhost:8080'
  };

- Optional proxy.conf.json for Angular:
  {
	"/v1": {
	  "target": "http://localhost:8080",
	  "secure": false,
	  "changeOrigin": true
	},
	"/hc": {
	  "target": "http://localhost:8080",
	  "secure": false,
	  "changeOrigin": true
	}
  }

3. Backend CORS
- Program.cs allows the documented local origins; ensure app.UseCors("DefaultCors") runs before authentication.

Quick test commands (PowerShell)

# From repo root, start backend (Kestrel http 8080)
$env:ASPNETCORE_URLS = "http://localhost:8080;http://localhost:5160"
dotnet run --project crud-onion\crud-onion.csproj

# In another shell test login
$body = @{ email = 'test@admin.com'; password = '123456'; deviceId = 'web-browser' } | ConvertTo-Json
Invoke-RestMethod -Uri 'http://localhost:8080/v1/auth/login' -Method Post -ContentType 'application/json' -Body $body

# If using IIS Express https
Invoke-RestMethod -Uri 'https://localhost:44324/v1/auth/login' -Method Post -ContentType 'application/json' -Body $body -SkipCertificateCheck

Troubleshooting
- If you get net::ERR_CONNECTION_REFUSED, confirm the backend is running and listening on the port (netstat -ano | Select-String ':8080|:44324').
- If you get mixed-content blocked, ensure FE and BE use the same scheme (both https or both http).
- If you get CORS errors, confirm the origin in the browser exactly matches one of the allowed origins in Program.cs.

Notes
- If FE runs on a different port (e.g. 4200), add that origin to the CORS list or use the proxy config.
- For production, replace local origins with your actual production frontend origin and secure JWT keys.
