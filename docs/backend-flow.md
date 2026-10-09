# Flujo automatizado del backend

Los scripts están diseñados para PowerShell y para una API local en Development.

## Flujo completo

Desde la raíz del repositorio:

```powershell
.\scripts\Invoke-BackendFlow.ps1 -RunScenario -AllowDestructive
```

Para ejecutar el flujo con un usuario autenticado, proporcione credenciales mediante
variables de entorno o use un token obtenido previamente; no se almacenan credenciales
en el repositorio:

```powershell
$env:API_TEST_EMAIL = "usuario-local@example.test"
$env:API_TEST_PASSWORD = "<contraseña-temporal>"
.\scripts\Invoke-BackendFlow.ps1 -RunScenario -AllowDestructive -Email $env:API_TEST_EMAIL -Password $env:API_TEST_PASSWORD
```

El comando ejecuta, en orden:

1. `dotnet restore` de `crud-onion.slnx`.
2. Compilación de la solución.
3. `dotnet ef database update` usando `DataAccess` y `crud-onion` como startup project.
4. Pruebas de `tests/Onion.Tests`.
5. Arranque temporal de la API.
6. Health check, Swagger, versión, autenticación y endpoints críticos.
7. Escenario CRUD opcional y detención garantizada del proceso.

El proceso se detiene aun cuando una etapa falla. El resumen final indica cada etapa y su estado.

Opciones útiles:

```powershell
# Solo pipeline sin escribir datos de negocio
.\scripts\Invoke-BackendFlow.ps1 -SkipMigrations

# Pipeline y barrido de todas las operaciones GET publicadas en Swagger
.\scripts\Invoke-BackendFlow.ps1 -RunScenario -ReadSweep -AllowDestructive -Email $env:API_TEST_EMAIL -Password $env:API_TEST_PASSWORD

# Inventario y contrato de todos los métodos publicados por Swagger
.\scripts\Invoke-BackendFlow.ps1 -RunScenario -FullSweep -AllowDestructive -Email $env:API_TEST_EMAIL -Password $env:API_TEST_PASSWORD

# Usar otro puerto local y compañía de pruebas
.\scripts\Invoke-BackendFlow.ps1 -BaseUrl http://127.0.0.1:5299 -CompanyId 1 -RunScenario -AllowDestructive

# Ejecutar en Release y guardar artefactos en otra carpeta
.\scripts\Invoke-BackendFlow.ps1 -Configuration Release -ArtifactDirectory artifacts\release-flow -SkipMigrations
```

`-AllowDestructive` es obligatorio cuando se ejecuta el escenario porque crea, modifica y elimina datos. La URL está restringida a `localhost`/`127.0.0.1` para evitar ejecutar accidentalmente el flujo contra producción.

## Escenario de usuario simulado

Para ejecutar solo el escenario contra una API ya levantada:

```powershell
.\scripts\Invoke-ApiScenario.ps1 -BaseUrl http://127.0.0.1:5199 -CompanyId 1 -AllowDestructive -ReadSweep
```

Para una prueba completa con credenciales reales de un usuario activo:

```powershell
.\scripts\Invoke-ApiScenario.ps1 -BaseUrl http://127.0.0.1:5199 -Email $env:API_TEST_EMAIL -Password $env:API_TEST_PASSWORD -CompanyId 1 -AllowDestructive -FullSweep
```

El escenario obtiene un token de Development si no se proporciona `-Token`, valida la sesión y módulos, comprueba roles/users/approvals, consulta catálogos y ejecuta:

- crear, leer, editar y eliminar un cliente;
- crear, leer y eliminar una categoría;
- lecturas paginadas de productos;
- barrido opcional de los `GET` definidos en Swagger.
- inventario de todos los métodos `GET/POST/PUT/PATCH/DELETE` definidos en Swagger cuando se usa `-FullSweep`.

Los recursos creados usan un sufijo temporal y se eliminan inmediatamente después de cada flujo. Si el proceso falla, el bloque `finally` intenta eliminar los IDs que hayan quedado pendientes.

Cada ejecución genera logs de la API y un `summary.json` en `artifacts/backend-flow`.
El escenario independiente genera su resumen en `artifacts/api-scenario` (o en la ruta
indicada con `-ArtifactDirectory`). Estos archivos permiten revisar el resultado sin
depender de la salida de Visual Studio.

## Límites intencionales

Las operaciones POST/PUT/PATCH/DELETE sin parámetros se someten a un probe de contrato sin cuerpo; se valida que la ruta responda con un código controlado y no con 5xx, pero no se considera un flujo de negocio completo. Las rutas con parámetros se registran como pendientes de escenario específico. Muchas requieren catálogos, parámetros fiscales, archivos, cajas abiertas, dependencias financieras o servicios externos. Para probar un flujo de negocio completo de un módulo nuevo, añádase un bloque explícito a `Invoke-ApiScenario.ps1` con sus datos válidos y cleanup correspondiente.

Las migraciones se aplican mediante `dotnet ef database update`; revisar la cadena `ConnectionStrings:OnionCrud` antes de ejecutar el comando. El flujo no debe utilizarse contra una base de datos productiva.

El flujo completo está limitado deliberadamente a `localhost` y `127.0.0.1`. `-AllowDestructive`
es obligatorio para escenarios que crean, modifican o eliminan datos. El smoke test sin
credenciales solo valida disponibilidad, Swagger, versión y que una ruta protegida rechace
una solicitud no autenticada; para validar operaciones protegidas se necesita un usuario
activo o un token explícito.
