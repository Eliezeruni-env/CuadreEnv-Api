@echo off
set ASPNETCORE_ENVIRONMENT=Development
cd /d "c:\Users\pelie\source\repos\MyDev\cuadreEnv\CuadreEnv-Api\crud-onion"
start "CuadreEnv-API" dotnet run --no-build --launch-profile http
