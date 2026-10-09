# Clerk y multi-tenancy (integración desactivada)

> Este documento se conserva como referencia histórica. La API ya no valida tokens
> de Clerk ni consulta su API. La autenticación vigente usa `POST /v1/auth/login`
> y JWT local firmado con `Jwt:Key`; los roles, permisos y tenants se gestionan en
> las tablas locales de usuarios/RBAC y mediante `CompanyId`.

Las entidades, migraciones y webhooks de Clerk se conservan temporalmente para una
limpieza o migración de datos posterior. No deben configurarse secretos Clerk ni
invocarse sus endpoints en el flujo actual.

## Configuración

La API valida JWT de Clerk con `Clerk:Authority` y descarga la metadata OIDC/JWKS de:

`https://curious-louse-4971.clerk.accounts.dev/.well-known/openid-configuration`

La validación de JWT no necesita la secret key de Clerk. Para operaciones administrativas contra `api.clerk.com`, la aplicación usa `IClerkBackendClient` y lee `Clerk:SecretKey` sin incluirla en el repositorio.

No se debe colocar ningún secreto en `appsettings.json`. Configura User Secrets o variables de entorno:

```powershell
dotnet user-secrets set "Clerk:Webhooks:SigningSecret" "whsec_..." --project crud-onion/crud-onion.csproj
dotnet user-secrets set "Clerk:SecretKey" "<CLERK_SECRET_KEY_ROTADA>" --project crud-onion/crud-onion.csproj
```

En producción configura un secret manager o una variable de entorno equivalente, por ejemplo:

```text
Clerk__SecretKey=<CLERK_SECRET_KEY_ROTADA>
Clerk__Webhooks__SigningSecret=<SVIX_WEBHOOK_SIGNING_SECRET>
```

La clave que haya sido publicada o compartida fuera del secret manager debe revocarse y regenerarse en Clerk. Nunca la envíes al frontend ni la escribas en logs.

Para exigir una marca de acceso emitida por USM, configura el nombre exacto del claim y el valor esperado. Mientras `Clerk:AccessClaim` esté vacío, el acceso se concede únicamente a una sesión autenticada que tenga `org_id`.

```powershell
dotnet user-secrets set "Clerk:AccessClaim" "saas_access" --project crud-onion/crud-onion.csproj
dotnet user-secrets set "Clerk:AccessClaimExpectedValue" "true" --project crud-onion/crud-onion.csproj
```

El issuer real debe coincidir con el claim `iss` de los tokens de Clerk. Si Clerk utiliza un issuer distinto al Authority, establece `Clerk:JwtIssuer` con ese valor. Los fallos de validación JWT se registran mediante `ClerkJwtAuthentication` sin escribir el token en logs.

## Autorización por proyecto

La policy `RequireCuadreEnvAccess` requiere un usuario autenticado y el proyecto exacto `CuadreEnv`. Se puede usar en controladores o acciones:

```csharp
[Authorize(Policy = "RequireCuadreEnvAccess")]
```

El handler acepta `projects` como claims repetidos, como arreglo JSON o como una propiedad `projects` dentro de un claim `publicMetadata`/`public_metadata`. Si Clerk solo guarda `publicMetadata` en su dashboard pero no lo incluye en el session token, el backend no puede autorizarlo: debe agregarse `projects` al template de session token de Clerk.

`ICurrentUserService.ClerkUserId` lee el claim `sub` y `ICurrentUserService.UserEmail` lee `email` o `ClaimTypes.Email`.

## Webhook

Configura en Clerk/Svix el endpoint:

`POST https://<host>/v1/api/webhooks/clerk`

La aplicación verifica `svix-id`, `svix-timestamp` y `svix-signature` antes de procesar:

- `user.created`
- `user.updated`
- `organizationMembership.created`
- `organizationMembership.updated`
- `organizationMembership.deleted`

Los eventos son idempotentes: usuarios y membresías se actualizan por sus claves de Clerk; una eliminación de membresía elimina solo esa relación local.

## Endpoints

Debido al prefijo global ya existente en la API, las rutas son:

- `GET /v1/api/me`
- `GET /v1/api/proyectos`
- `POST /v1/api/proyectos` (requiere `org_role=org:admin`)
- `POST /v1/api/webhooks/clerk` (anónimo, firma Svix obligatoria)

## Aislamiento

`Proyecto.OrganizationId` es obligatorio y tiene una foreign key a `ClerkOrganization`. `OnionDbContext` aplica un `HasQueryFilter` que compara ese campo con `org_id` de la sesión actual, resuelto por `IOrganizationTenantProvider`. Los endpoints multi-tenant siguen requiriendo su contexto de organización/empresa; el endpoint protegido por `MiSaaSAngular` usa el claim `projects` y no exige `org_id` para la comprobación inicial de acceso.

El webhook y las tablas espejo no son la fuente de verdad de identidad: Clerk sigue siendo la autoridad. Las filas locales solo sirven para joins de negocio y reflejar cambios recibidos.

## Base local

Desarrollo usa:

`Server=(localdb)\\MSSQLLocalDB;Database=CuadreEnv;Trusted_Connection=True;TrustServerCertificate=True`

Activa `ApplyMigrationsOnStartup` únicamente en entornos controlados, o genera/aplica una migración EF antes de desplegar.
