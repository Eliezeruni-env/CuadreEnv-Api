# CuadreEnv API — Descripción del repositorio

## 1. Identificación

- **Solución:** `crud-onion.slnx`
- **Plataforma:** .NET 10 / ASP.NET Core
- **Lenguaje principal:** C# con nullable reference types e implicit usings habilitados
- **Base de datos:** PostgreSQL mediante Entity Framework Core y Npgsql
- **Arquitectura:** Onion Architecture, con separación de dominio, negocio, acceso a datos y API
- **Tipo de aplicación:** API REST SaaS multiempresa
- **Documentación funcional:** [`SISTEMA.md`](SISTEMA.md)

## 2. Mapa del repositorio

```text
.
├── crud-onion/                       API ASP.NET Core y punto de entrada
│   ├── Controllers/                  Endpoints HTTP
│   ├── Middleware/                   Pipeline transversal
│   ├── Authentication/               Componentes de identidad y acceso
│   ├── Authorization/                Convenciones de módulos
│   ├── Health/                       Health checks
│   ├── Hubs/                         SignalR
│   ├── Swagger/                      Filtros de OpenAPI
│   ├── Webhooks/                     Webhooks y verificación de firmas
│   ├── docs/                         Documentos operativos existentes
│   ├── Program.cs                    Composición de la aplicación
│   ├── appsettings*.json             Configuración por ambiente
│   └── Properties/                   Perfiles de ejecución
├── Domain/                           Entidades y reglas del dominio
├── BusinessLogic/                    DTOs, validadores, interfaces y servicios
├── DataAccess/                       EF Core, PostgreSQL, repositorios y migraciones
├── Onion.Common/                     Contratos, modelos y servicios compartidos
├── Domain.Tests/                     Pruebas unitarias de reglas del dominio
└── tests/
	├── Onion.Tests/                  Pruebas de API, middleware, autorización y servicios
	├── Onion.BusinessLogic.Tests/    Pruebas unitarias de lógica de negocio
	└── Onion.IntegrationTests/       Pruebas end-to-end con WebApplicationFactory
```

## 3. Proyectos de la solución

| Proyecto | Responsabilidad | Dependencias principales |
| --- | --- | --- |
| `crud-onion/crud-onion.csproj` | Host web, configuración de ASP.NET Core, controladores, middleware, JWT, Swagger y SignalR | BusinessLogic, DataAccess, EF Core, Npgsql |
| `Domain/Onion.Domain.csproj` | Entidades, value objects y reglas del negocio central | Onion.Common |
| `BusinessLogic/Onion.BusinessLogic.csproj` | Casos de uso, servicios, DTOs, validaciones, mapeos y procesos en background | DataAccess, Onion.Common, AutoMapper, FluentValidation |
| `DataAccess/Onion.DataAccess.csproj` | DbContext, configuraciones EF, repositorios, Unit of Work y migraciones PostgreSQL | Domain, EF Core, Npgsql |
| `Onion.Common/Onion.Common.csproj` | Modelos de respuesta, excepciones, autorización, tenant y servicios compartidos | ASP.NET Core shared framework |
| `Domain.Tests/Domain.Tests.csproj` | Pruebas de invariantes y reglas puras del dominio | Domain, xUnit |
| `tests/Onion.Tests/Onion.Tests.csproj` | Pruebas de contratos de controladores, middleware, tenants y autorización | API, BusinessLogic, DataAccess, Domain, Common |
| `tests/Onion.BusinessLogic.Tests/Onion.BusinessLogic.Tests.csproj` | Pruebas aisladas de servicios de negocio | BusinessLogic, DataAccess, Domain, Common, Moq |
| `tests/Onion.IntegrationTests/Onion.IntegrationTests.csproj` | Flujo completo con servidor de prueba, migraciones y PostgreSQL | API, BusinessLogic, DataAccess, Npgsql, WebApplicationFactory |

Todos los proyectos apuntan a `net10.0`.

## 4. Responsabilidades por capa

### `Onion.Common`

Contiene contratos que pueden ser utilizados por varias capas sin acoplarlas a una implementación concreta: respuestas de API, códigos de error, excepciones, interfaces de usuario actual, tenant, email y alertas, además de atributos y constantes de autorización.

### `Domain`

Define el modelo de negocio. Sus áreas incluyen:

- Productos y catálogo.
- Ventas, compras, pagos y devoluciones.
- Finanzas, caja, cuentas por cobrar/pagar y créditos.
- Inventario y almacenes.
- Usuarios, empresas, invitaciones y RBAC.
- Facturación fiscal y secuencias.
- Citas, recursos y disponibilidad.
- Auditoría y suscripciones.

Las reglas que protegen cierres de caja, discrepancias y estados de entidades deben permanecer en esta capa cuando no requieran infraestructura.

### `DataAccess`

Implementa `OnionDbContext`, configuraciones de entidades, filtros globales de empresa, repositorios concretos, `UnitOfWork`, proveedores de tenant y migraciones. La aplicación usa Npgsql y PostgreSQL; el contexto recibe el tenant desde claims, request o procesos de background.

### `BusinessLogic`

Orquesta casos de uso mediante interfaces y servicios concretos. Aquí se encuentran los DTOs, validadores FluentValidation, perfiles AutoMapper, servicios de ventas/caja/inventario, suscripciones, reportes, autenticación, fiscalidad y procesos hospedados.

El outbox fiscal y el servicio de vencimiento de créditos son procesos hospedados y deben considerarse al revisar el comportamiento de despliegue.

### `crud-onion`

Es el adaptador HTTP y el ejecutable. `Program.cs` registra dependencias, autenticación, CORS, compresión, rate limiting, Swagger, health checks, middleware, hubs, servicios hosted y rutas.

## 5. Convenciones importantes

- Las rutas de controladores reciben el prefijo global `/v1`.
- Las interfaces de negocio están en `BusinessLogic/Services/Abstract`; las implementaciones en `BusinessLogic/Services/Concrete`.
- Las interfaces de repositorio están en `DataAccess/Repositories/Abstract`; las implementaciones en `DataAccess/Repositories/Concrete`.
- Los DTOs están separados de las entidades y se ubican en `BusinessLogic/Dtos`.
- La validación transversal de modelos y el formato de excepciones se resuelven desde la API.
- El acceso a datos operativos debe provenir de la configuración o de PostgreSQL, no de valores hardcodeados en servicios.
- Las nuevas rutas y controladores deben recibir el mapeo de módulo correspondiente antes de depender de autorización granular.
- El contexto de empresa debe conservarse durante toda la operación y nunca debe permitirse el acceso cruzado entre tenants.

## 6. Configuración y secretos

Los archivos `appsettings.json` y `appsettings.Development.json` sirven como plantilla de configuración, pero no deben contener secretos reales, credenciales, contraseñas, claves JWT ni API keys compartidas.

Usar User Secrets en desarrollo o variables de entorno:

```powershell
dotnet user-secrets init --project crud-onion/crud-onion.csproj
dotnet user-secrets set "ConnectionStrings:OnionCrud" "Host=localhost;Port=5432;Database=cuadreenv;Username=postgres;Password=<secret>" --project crud-onion/crud-onion.csproj
dotnet user-secrets set "Jwt:Key" "<clave-local-de-al-menos-32-caracteres>" --project crud-onion/crud-onion.csproj
```

Para despliegue, las claves jerárquicas de .NET pueden expresarse con doble guion bajo, por ejemplo `Jwt__Key`, `Internal__ApiKey` y `ConnectionStrings__OnionCrud`.

> Si una credencial fue incluida accidentalmente en un archivo de configuración versionado, debe revocarse/rotarse y retirarse del historial según la política del repositorio. Esta documentación no reproduce ningún valor sensible.

## 7. Desarrollo local

Requisitos:

- SDK de .NET 10.
- PostgreSQL accesible desde la máquina de desarrollo.
- Una base de datos configurada en `ConnectionStrings:OnionCrud`.
- Variables de entorno o User Secrets para las claves requeridas.

Comandos recomendados desde la raíz:

```powershell
dotnet restore crud-onion.slnx
dotnet build crud-onion.slnx
dotnet run --project crud-onion/crud-onion.csproj
```

Perfiles disponibles en `crud-onion/Properties/launchSettings.json`:

| Perfil | URL |
| --- | --- |
| `http` | `http://localhost:8080` |
| `https` | `https://localhost:7060` y `http://localhost:8080` |
| `IIS Express` | `http://localhost:52510` y `https://localhost:44324` |

En desarrollo Swagger está habilitado por `appsettings.Development.json`; en otros ambientes debe activarse explícitamente y protegerse adecuadamente.

## 8. Persistencia y migraciones

Las migraciones canónicas están en `DataAccess/Migrations` y corresponden al proveedor PostgreSQL. Para revisar el estado o generar una migración:

```powershell
dotnet ef migrations list --project DataAccess/Onion.DataAccess.csproj --startup-project crud-onion/crud-onion.csproj
dotnet ef migrations add NombreDescriptivo --project DataAccess/Onion.DataAccess.csproj --startup-project crud-onion/crud-onion.csproj
dotnet ef database update --project DataAccess/Onion.DataAccess.csproj --startup-project crud-onion/crud-onion.csproj
```

`ApplyMigrationsOnStartup` está deshabilitado por defecto. Solo debe activarse en desarrollo o en una base de integración aislada. El archivo `crud-onion/apply_migrations.sql` es histórico y usa sintaxis de SQL Server; no sustituye las migraciones Npgsql actuales.

## 9. Pruebas

### Pruebas unitarias

```powershell
dotnet test Domain.Tests/Domain.Tests.csproj
dotnet test tests/Onion.BusinessLogic.Tests/Onion.BusinessLogic.Tests.csproj
dotnet test tests/Onion.Tests/Onion.Tests.csproj
```

Cubren reglas de dominio, discrepancias y cierres de caja, servicios de ventas/fiscalidad, contratos de controladores, aislamiento de empresa, middleware, autorización, suscripciones y endpoints administrativos.

### Pruebas de integración

Requieren PostgreSQL real. El proyecto crea una base temporal con nombre `OnionTest_<identificador>` y aplica migraciones al iniciar. Configurar antes de ejecutar:

```powershell
$env:ConnectionStrings__OnionCrud = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=<secret>"
dotnet test tests/Onion.IntegrationTests/Onion.IntegrationTests.csproj
```

La prueba integrada cubre, entre otros, registro, login, decodificación del JWT y acceso a almacenes. No ejecutar contra una base de producción.

### Validación completa

```powershell
dotnet test crud-onion.slnx --no-restore
```

Las pruebas de concurrencia de inventario, cierre de caja obsoleto y rollback integral deben validarse contra PostgreSQL, no únicamente con dobles o el proveedor InMemory.

## 10. Flujo de entrega

1. Crear una rama de trabajo a partir de la rama objetivo.
2. Implementar cambios respetando la separación de capas.
3. Agregar o actualizar pruebas junto al comportamiento modificado.
4. Ejecutar `dotnet build` y las suites relevantes.
5. Revisar migraciones, contratos HTTP, aislamiento de tenant y configuración.
6. Verificar que no se incluyan secretos, logs sensibles o bases locales.
7. Aplicar migraciones de forma controlada en el ambiente destino.
8. Confirmar `/hc`, logs, CORS, JWT, licencia USM y dependencias externas después del despliegue.

## 11. Integraciones externas

- **USM:** validación de licencia y derechos SaaS por HTTP.
- **DGII:** envío fiscal opcional y asíncrono.
- **SMTP:** correo transaccional.
- **Clerk/Svix:** componentes heredados y webhooks conservados para compatibilidad; no son el flujo local vigente de autenticación.
- **Frontend:** el backend acepta los orígenes configurados en CORS. Las instrucciones para Angular/proxy se encuentran en `crud-onion/docs/frontend-setup.md`.

## 12. Documentación existente

- [`SISTEMA.md`](SISTEMA.md): visión completa funcional y técnica.
- [`DEPLOYMENT.md`](DEPLOYMENT.md): referencia histórica de despliegue; debe actualizarse antes de usarla porque todavía menciona SQL Server y scripts que no corresponden al proveedor PostgreSQL actual.
- [`crud-onion/docs/frontend-setup.md`](crud-onion/docs/frontend-setup.md): conectividad frontend-backend.
- [`crud-onion/docs/financial-integrity-audit.md`](crud-onion/docs/financial-integrity-audit.md): auditoría de integridad financiera, decisiones y brechas de pruebas.
- [`crud-onion/docs/clerk-saas.md`](crud-onion/docs/clerk-saas.md): referencia histórica de Clerk y multi-tenancy.
