# CuadreEnv — Descripción del sistema

## 1. Propósito

CuadreEnv es una API SaaS para la operación administrativa y comercial de empresas con punto de venta. Centraliza ventas, compras, inventario, almacenes, caja, clientes, proveedores, usuarios, suscripciones, reportes y documentos fiscales en una aplicación ASP.NET Core sobre .NET 10.

La API expone contratos HTTP versionados bajo `/v1`, entrega respuestas JSON y utiliza PostgreSQL como fuente de persistencia. El diseño aplica una arquitectura Onion: el dominio no depende de la API y las reglas de negocio se separan de los repositorios y controladores.

## 2. Capacidades funcionales

### Identidad y acceso

- Registro, inicio de sesión, renovación y revocación de tokens mediante `POST /v1/auth/register`, `POST /v1/auth/login`, `POST /v1/auth/refresh` y `POST /v1/auth/revoke`.
- Consulta del usuario actual, empresas disponibles y módulos habilitados.
- Cambio de empresa activa para usuarios asociados a más de una empresa.
- Administración de usuarios, invitaciones, membresías y solicitudes de gestión.
- JWT local firmado; la configuración vigente no usa Clerk como proveedor activo de tokens. Los componentes de Clerk y sus tablas espejo se conservan para compatibilidad y migración futura.

### Operación comercial

- Catálogo de productos, categorías, tipos de producto y unidades de medida.
- Clientes y proveedores.
- Compras, órdenes y recepción de mercancías.
- Ventas, devoluciones, pagos y notas de crédito.
- Dashboard, métricas y reportes operativos.

### Inventario y almacenes

- Inventarios por empresa y almacén.
- Movimientos y transferencias de existencias.
- Reservación y confirmación de stock durante la venta.
- Actualizaciones condicionales para evitar que dos operaciones consuman simultáneamente la última unidad disponible.

### Caja y finanzas

- Apertura, movimientos y cierre de sesiones/cajas.
- Cálculo server-side del importe esperado y de la diferencia de caja.
- Clasificación de diferencias como sobrante, faltante o arqueo exacto.
- Historial de cierre inmutable: una caja cerrada no se edita; las correcciones deben registrarse como nuevos eventos.
- Cuentas por cobrar, cuentas por pagar, créditos, cuotas y planes de pago.
- Uso de `ETag`/`If-Match` y de la versión `xmin` de PostgreSQL para detectar cierres concurrentes obsoletos.

### Facturación y fiscalidad

- Secuencias de comprobantes fiscales y documentos fiscales.
- Integración opcional con DGII mediante un cliente HTTP configurado.
- Cola/outbox persistente para entregas fiscales asíncronas.
- Reintentos durables, auditoría de envíos y clave de idempotencia determinista por venta (`SALE:{saleId}`).
- En desarrollo o cuando `Dgii:Enabled` es falso se utiliza el proveedor simulado configurado por la aplicación.

### Suscripciones SaaS

- Planes, suscripciones y derechos de uso por empresa.
- Validación de licencia USM al iniciar la aplicación cuando está habilitada.
- Las operaciones de lectura permanecen disponibles; las operaciones de escritura pueden bloquearse con HTTP 402 cuando la empresa no está activa o su suscripción no está en estado `Active` o `Trialing`.
- La validación de inicio es fail-closed en producción: si USM debe validar la licencia y no responde después de los reintentos configurados, la aplicación no inicia.

### Seguridad y observabilidad

- Autenticación Bearer JWT con issuer, audience, expiración y clave configurables.
- Contexto multiempresa resuelto mediante `CompanyId`, encabezado `X-Company-Id` o el contexto de la sesión, con comprobación de pertenencia.
- Filtros globales de Entity Framework para aislar datos de cada empresa.
- Respuestas de error normalizadas, identificador de correlación y logging de Serilog en consola y archivo.
- Rate limiting para login/refresh y búsquedas.
- Health check de base de datos en `/hc`.
- Hub SignalR en `/hubs/security-alerts` para alertas de seguridad.
- Swagger/OpenAPI en `/swagger` cuando `Swagger:Enabled` está habilitado.

## 3. Arquitectura y flujo de una solicitud

```text
Cliente web/móvil
	  |
	  v
ASP.NET Core API (/v1)
	  |
	  +-- CorrelationId + manejo global de excepciones
	  +-- Autenticación JWT
	  +-- Contexto de empresa/tenant
	  +-- Estado de cuenta y suscripción
	  +-- Autorización y rate limiting
	  v
Controlador -> Servicio de negocio -> Repositorio/Unit of Work
									  |
									  v
							  OnionDbContext / PostgreSQL
```

1. El cliente envía un JWT Bearer y, cuando corresponde, el contexto de empresa.
2. El pipeline valida el token y resuelve la empresa activa.
3. `TenantMiddleware` confirma que la empresa existe, está activa y es accesible para el usuario.
4. Entity Framework aplica el aislamiento mediante filtros globales de tenant.
5. El controlador valida el DTO y delega la operación al servicio de negocio.
6. El servicio ejecuta las reglas de negocio a través de repositorios y `UnitOfWork`.
7. Las operaciones críticas de POS se confirman en una transacción que cubre venta, detalle, stock, caja, outbox fiscal y secuencia de factura.
8. La API devuelve una respuesta JSON consistente y registra el identificador de correlación.

## 4. Modelo de datos principal

El contexto `OnionDbContext` administra, entre otras, las siguientes áreas:

- Empresas, usuarios, relaciones usuario-empresa, invitaciones y tokens de refresco.
- Productos, categorías, tipos, clientes y proveedores.
- Compras, ventas, detalles, pagos, devoluciones y notas de crédito.
- Cajas, sesiones, movimientos, discrepancias y auditorías.
- Almacenes, inventarios, movimientos y reservas.
- Secuencias, documentos fiscales y auditorías de envío.
- Suscripciones, planes, solicitudes de gestión y bitácora de auditoría.

Las entidades comparten reglas de auditoría y soft delete donde aplica. Las migraciones activas se encuentran en `DataAccess/Migrations` y están construidas para PostgreSQL/Npgsql.

## 5. Contrato HTTP y grupos de endpoints

Todos los controladores reciben el prefijo global `/v1`, salvo rutas explícitas de compatibilidad. Los grupos principales son:

| Grupo | Responsabilidad |
| --- | --- |
| `auth` y `me` | Sesión, JWT, usuario actual, empresas y módulos |
| `companies`, `companysettings` | Empresas y configuración operativa |
| `products`, `categories`, `producttype` | Catálogo |
| `customer`, `supplier` | Terceros comerciales |
| `sale`, `purchase`, `payment`, `return`, `creditnote`, `credits` | Ciclo comercial y financiero |
| `warehouse`, `warehousemovement`, `inventory` | Existencias y movimientos |
| `cashregister`, `cashsession`, `cashmovement`, `caja` | Operación de caja |
| `appointments` | Citas, disponibilidad y recursos |
| `reports`, `dashboard`, `metrics`, `dgiireports` | Consultas, indicadores y reportes |
| `subscription` | Planes y suscripciones |
| `users`, `usermanagement`, `invitations`, `rbac` | Administración de usuarios y acceso |
| `fiscalsequence` | Secuencias y documentos fiscales |
| `internal/*` | Operaciones internas protegidas por API key o rol administrativo |

La especificación detallada de parámetros y respuestas debe consultarse en Swagger cuando esté habilitado y en los DTO/controladores correspondientes. Las respuestas de validación y errores se normalizan mediante los filtros y middleware globales.

## 6. Configuración de ejecución

La configuración se obtiene de `appsettings.json`, archivos por ambiente, User Secrets o variables de entorno. Nunca deben almacenarse secretos reales en el repositorio.

Variables/configuraciones esenciales:

- `ConnectionStrings:OnionCrud`: conexión PostgreSQL.
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`: firma y validación de JWT.
- `Internal:ApiKey`: protección de endpoints internos.
- `Cors:AllowedOrigins`: orígenes permitidos; es obligatorio configurarlo en producción.
- `Usm:*`: validación de licencias y derechos SaaS.
- `Dgii:*` y `Fiscal:*`: envío fiscal, timeouts, reintentos y certificados.
- `Smtp:*`: correo transaccional.
- `Swagger:Enabled`: publicación de OpenAPI/Swagger.
- `ApplyMigrationsOnStartup`: solo debe habilitarse en desarrollo o entornos de integración aislados.

En producción la aplicación exige, entre otros, una clave JWT y una API key interna de al menos 32 caracteres, una cadena de conexión y CORS configurado.

## 7. Puesta en marcha rápida

Desde la raíz del repositorio:

```powershell
dotnet restore crud-onion.slnx
dotnet build crud-onion.slnx
$env:ConnectionStrings__OnionCrud = "Host=localhost;Port=5432;Database=cuadreenv;Username=postgres;Password=<secret>"
dotnet run --project crud-onion/crud-onion.csproj
```

Perfiles incluidos:

- HTTP Kestrel: `http://localhost:8080`.
- HTTPS Kestrel: `https://localhost:7060` y HTTP `8080`.
- IIS Express: HTTP `52510` y HTTPS `44324`.

Comprobaciones básicas:

- `GET /` devuelve el estado básico del servicio.
- `GET /v1` devuelve la versión de la API.
- `GET /hc` ejecuta el health check de PostgreSQL.
- `GET /swagger` muestra la documentación si está habilitada.

## 8. Estado y límites conocidos

- La autorización fina por roles/módulos está preparada mediante atributos y convenciones, pero el bypass configurado para desarrollo/integración puede dejar el control granular pospuesto. La autenticación y el aislamiento de empresa siguen siendo requisitos del pipeline.
- La integración histórica de Clerk no representa el flujo de autenticación vigente.
- Las pruebas de concurrencia y de integración requieren un PostgreSQL real y una base aislada `OnionTest_*`.
- `crud-onion/apply_migrations.sql` es un script histórico con sintaxis SQL Server. Para la aplicación actual debe usarse la cadena de migraciones EF Core en `DataAccess/Migrations` con Npgsql.
- El envío a DGII es externo a la transacción local; la garantía local se basa en outbox, reintentos e idempotencia.

## 9. Documentos relacionados

- [`REPOSITORIO.md`](REPOSITORIO.md): estructura, desarrollo y operación del repositorio.
- [`crud-onion/docs/frontend-setup.md`](crud-onion/docs/frontend-setup.md): conexión de un frontend local.
- [`crud-onion/docs/financial-integrity-audit.md`](crud-onion/docs/financial-integrity-audit.md): decisiones de integridad financiera.
- [`crud-onion/docs/clerk-saas.md`](crud-onion/docs/clerk-saas.md): referencia histórica de Clerk y multi-tenancy.
