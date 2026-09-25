# Usuarios demo jerárquicos

La migración `20260908223740_SeedTenHierarchicalDemoUsers` crea 10 usuarios de demostración en dos compañías RBAC. Todos están activos y tienen datos de dirección, ciudad, ubicación operativa y coordenadas.

## Contraseña

Todas las cuentas usan la contraseña demo:

```text
Demo123!
```

No utilizar estas cuentas ni esta contraseña en producción.

## Compañía RBAC Demo A

| Usuario | Email | Role legado | Rol RBAC | Uso |
|---|---|---|---|---|
| Ana Administrador | `hier.admin.a@cuadreenv.local` | Admin | RBAC Admin | Administración completa del tenant A |
| Marta Auditora | `hier.audit.a@cuadreenv.local` | Audit | RBAC Audit | Auditoría y aprobaciones |
| Carla Ventas | `hier.sales.a@cuadreenv.local` | Employee | RBAC Sales Operator | Consulta y creación de ventas |
| Elena Inventario | `hier.inventory.a@cuadreenv.local` | Employee | RBAC Inventory Manager | Consulta y edición de inventario |
| Sofía Operadora | `hier.employee.a@cuadreenv.local` | Employee | RBAC Employee | Operación básica y caja |

## Compañía RBAC Demo B

| Usuario | Email | Role legado | Rol RBAC | Uso |
|---|---|---|---|---|
| Luis Administrador | `hier.admin.b@cuadreenv.local` | Admin | RBAC Admin | Administración completa del tenant B |
| Pedro Auditor | `hier.audit.b@cuadreenv.local` | Audit | RBAC Audit | Auditoría y aprobaciones |
| Jorge Ventas | `hier.sales.b@cuadreenv.local` | Employee | RBAC Sales Operator | Consulta y creación de ventas |
| Rafael Inventario | `hier.inventory.b@cuadreenv.local` | Employee | RBAC Inventory Manager | Consulta y edición de inventario |
| Diego Operador | `hier.employee.b@cuadreenv.local` | Employee | RBAC Employee | Operación básica y caja |

## Aplicación de la base de datos

El error `Invalid column name 'Address'` significa que la base de datos está atrasada respecto al modelo actual. Aplicar el script completo desde la raíz del repositorio:

```powershell
sqlcmd -S <servidor> -d <base-de-datos> -E -i .\artifacts\database-deploy.sql
```

O, en desarrollo con la cadena de conexión configurada:

```powershell
dotnet ef database update `
  --project .\DataAccess\Onion.DataAccess.csproj `
  --startup-project .\crud-onion\crud-onion.csproj `
  --context OnionDbContext
```

El script aplica en orden:

1. `20260908195008_AddOperationalMetricsAndCashLifecycle`.
2. `20260908211720_AddUserGeolocationAndLoginIp`.
3. `20260908223740_SeedTenHierarchicalDemoUsers`.
4. `20260908224629_FixHierarchicalDemoUserRoles`.
5. `20260908225249_FixHierarchicalDemoPasswords`.

Después verificar el login de una cuenta y consultar `/hc`. Si la API se inicia con `RunDemoSeedOnStartup=true`, el seeder existente puede crear datos adicionales, pero los 10 usuarios jerárquicos se crean mediante la migración anterior y no dependen de ese flag.

La migración `FixHierarchicalDemoPasswords` corrige el hash de las diez cuentas `hier.*` para que la contraseña `Demo123!` funcione. Si una base ya tiene aplicada esa migración y el login continúa fallando, confirmar que la API está usando la misma base de datos configurada en `ConnectionStrings:OnionCrud`.
