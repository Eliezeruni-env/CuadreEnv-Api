# ESTRUCTURA DESCRIPTIVA DEL REPOSITORIO (CUADRE-ENV API)

> **Guía Técnica de Organización del Backend, Capas Onion Architecture, Servicios y Motor Multi-Tenant**  
> *Repositorio:* `CuadreEnv-Api` (.NET 10 Web API / C#) + Base de Datos PostgreSQL / Supabase

---

## 1. ARQUITECTURA GENERAL (ONION / CLEAN ARCHITECTURE)

El backend de CuadreEnv está diseñado bajo el patrón **Onion Architecture** (Arquitectura Limpia), separando estrictamente las responsabilidades para garantizar mantenibilidad, aislamiento de la lógica de negocio y facilidad de pruebas:

```
                    ┌────────────────────────────────────────┐
                    │      Capa de Presentación / API        │
                    │   (Controllers, Middlewares, Program)   │
                    └───────────────────┬────────────────────┘
                                        │
                    ┌───────────────────▼────────────────────┐
                    │       Capa de Lógica de Negocio        │
                    │      (BusinessLogic - Services, DTOs)  │
                    └───────────────────┬────────────────────┘
                                        │
                    ┌───────────────────▼────────────────────┐
                    │        Capa de Acceso a Datos          │
                    │     (DataAccess - DbContext, Repos)    │
                    └───────────────────┬────────────────────┘
                                        │
                    ┌───────────────────▼────────────────────┐
                    │            Capa de Dominio             │
                    │        (Domain - Entities, Enums)      │
                    └────────────────────────────────────────┘
```

---

## 2. ÁRBOL PRINCIPAL DE PROYECTOS Y DIRECTORIOS

```
CuadreEnv-Api/
├── Domain/                                 # Capa de Dominio (Entidades puras sin dependencias externas)
│   ├── Entities/
│   │   ├── Company.cs                     # Empresa / Tenant principal
│   │   ├── User.cs                        # Usuario / Colaborador vinculado a CompanyId
│   │   ├── Product.cs                     # Producto con SKU, inventario, precios y banderas fiscales
│   │   ├── Sale.cs & SaleDetail.cs        # Transacciones de venta y detalle de líneas
│   │   ├── CashRegister.cs                # Terminal física/lógica de caja
│   │   ├── CashSession.cs                 # Turno de caja abierto/cerrado con desglose de arqueo
│   │   ├── Warehouse.cs & StockMovement.cs# Almacenes, lotes y trazabilidad de inventario
│   │   ├── Customer.cs                    # Cartera de clientes y crédito
│   │   └── Supplier.cs                    # Proveedores y cuentas por pagar
│   └── Enums/                             # Estados de caja, métodos de pago, tipos de NCF
│
├── DataAccess/                             # Capa de Persistencia y ORM (EF Core)
│   ├── OnionDbContext.cs                  # Contexto EF Core con Global Query Filters por CompanyId
│   ├── Migrations/                        # Historial de migraciones para PostgreSQL
│   └── Repositories/
│       ├── Abstract/                      # Interfaces de repositorios (IUserRepository, ISaleRepository...)
│       └── Concrete/                      # Implementaciones con LINQ / EF Core
│
├── BusinessLogic/                          # Capa de Servicios y Casos de Uso
│   ├── Services/
│   │   ├── Abstract/                      # Interfaces de servicio (IAuthService, ISaleService...)
│   │   └── Concrete/                      # Lógica de negocio (AuthService, SaleService, CashRegisterService...)
│   └── DTOs/                              # Contratos de entrada y salida (Requests / Responses)
│
├── crud-onion/ (o src/ Presentation)       # Capa Web API HTTP (.NET 10)
│   ├── Controllers/                       # Endpoints REST (AuthController, SalesController, etc.)
│   ├── Middlewares/                       # TenantMiddleware, JwtMiddleware, ExceptionHandling
│   └── Program.cs                         # Pipeline de configuración, DI y servicios
│
├── Dockerfile                              # Despliegue en contenedor Docker
├── heroku.yml                              # Configuración de despliegue en Heroku Container
└── DESCRIPCION_PROYECTO_CUADRE_ENV.md      # Visión integral del ecosistema y multi-tenancy
```

---

## 3. AISLAMIENTO MULTI-TENANT EN EL BACKEND

### 3.1. Extracción del Tenant en Middleware
Cada petición entrante pasa por la verificación del token JWT. La identidad extrae el claim `companyId` y lo inyecta en el servicio con ámbito por solicitud (`ScopedCurrentUserService`):
```csharp
var companyIdClaim = context.User.Claims.FirstOrDefault(c => c.Type == "companyId")?.Value;
if (int.TryParse(companyIdClaim, out var companyId))
{
    currentUserService.SetCompanyId(companyId);
}
```

### 3.2. Filtro Global en `OnionDbContext`
A nivel de Entity Framework Core, ninguna consulta a tablas de datos de negocio (`Products`, `Sales`, `CashRegisters`, `Customers`, etc.) se ejecuta sin el filtro automático:
```csharp
modelBuilder.Entity<Product>().HasQueryFilter(e => 
    this.IsGlobalTenantAccess || EF.Property<int>(e, "CompanyId") == this.TenantCompanyIdForQuery);
```
Esto asegura que ni una brecha en un controlador ni un error de programación permita fuga de datos entre empresas.

---

## 4. COMANDOS ÚTILES DE CONSTRUCCIÓN Y DESPLIEGUE

### Compilación limpia:
```powershell
dotnet build --configuration Release
```

### Aplicación de Migraciones a PostgreSQL / Supabase:
```powershell
dotnet ef database update --project DataAccess --startup-project crud-onion
```

### Despliegue en Heroku:
```powershell
git push heroku migracion-postgres:main
```
