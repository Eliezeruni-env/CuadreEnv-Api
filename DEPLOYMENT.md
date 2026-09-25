# Despliegue de CuadreEnv API

## Requisitos

- .NET 10 Runtime en el servidor o una imagen publicada con `dotnet publish`.
- SQL Server accesible desde la API.
- Una identidad de despliegue con permisos para ejecutar el script de esquema.
- Secretos configurados fuera del repositorio.

## Variables de entorno obligatorias

En ASP.NET Core, las secciones anidadas usan doble guion bajo (`__`):

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__OnionCrud=Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=False;Encrypt=True
Jwt__Key=<secreto aleatorio de al menos 32 caracteres>
Jwt__Issuer=onion-api
Jwt__Audience=onion-api-audience
Internal__ApiKey=<clave interna aleatoria>
```

Para habilitar correo, configurar también:

```text
Smtp__Host=<servidor SMTP>
Smtp__Port=587
Smtp__User=<usuario SMTP>
Smtp__Password=<secreto SMTP>
Smtp__FromEmail=<correo remitente>
Smtp__FromName=CuadreEnv
Smtp__EnableSsl=true
```

No definir estos valores en `appsettings.json` en producción. Usar el administrador de secretos del proveedor cloud, variables protegidas del pipeline o un almacén como Azure Key Vault.

## Publicación

```powershell
dotnet restore .\crud-onion\crud-onion.csproj
dotnet publish .\crud-onion\crud-onion.csproj -c Release -o .\publish --no-restore
```

Desplegar el contenido de `publish` junto con `appsettings.json` y los archivos de configuración del ambiente. No desplegar `appsettings.Development.json` en producción.

## Actualización de la base de datos

El archivo `artifacts/database-deploy.sql` es un script idempotente generado desde las migraciones EF. Revisarlo y ejecutarlo con una cuenta de despliegue contra la base de datos destino:

```powershell
sqlcmd -S <servidor> -d <base-de-datos> -U <usuario> -P <password> -i .\artifacts\database-deploy.sql
```

Alternativamente, desde un entorno controlado con la cadena de conexión configurada:

```powershell
dotnet ef database update `
  --project .\DataAccess\Onion.DataAccess.csproj `
  --startup-project .\crud-onion\crud-onion.csproj `
  --context OnionDbContext
```

No ejecutar estos comandos contra producción desde una estación local sin revisión, respaldo y ventana de cambio. Verificar que las migraciones `20260908195008_AddOperationalMetricsAndCashLifecycle` y `20260908211720_AddUserGeolocationAndLoginIp` queden registradas en `__EFMigrationsHistory`.

## Verificación posterior

1. Confirmar que el proceso inicia sin errores de configuración.
2. Consultar `GET /hc` y esperar HTTP 200.
3. Consultar `GET /` y validar la respuesta `status: ok`.
4. Revisar logs de arranque, conexión SQL y errores de autenticación.
5. Probar login, refresh token y una operación representativa por cada módulo desplegado.

En producción la aplicación no inicia si falta la cadena `ConnectionStrings:OnionCrud`, un `Jwt:Key` de al menos 32 caracteres o `Internal:ApiKey`. Esto evita desplegar una instancia aparentemente sana pero insegura o sin conexión a la base de datos.

## Seguridad antes de liberar

- Rotar inmediatamente las credenciales SMTP y la API key que hayan estado previamente en el repositorio.
- Generar una clave JWT nueva por ambiente; no reutilizar la de desarrollo.
- Restringir `Frontend__Url` al origen real del frontend y usar HTTPS.
- Configurar backups y recuperación de SQL Server antes de aplicar migraciones.
- No habilitar migraciones automáticas al arrancar la API en producción; usar el script revisado o un job de despliegue separado.
