# Guía completa: cómo añadir una entidad/funcionalidad desde 0 (en español)

Esta guía detalla, paso a paso y por capas, cómo añadir una nueva entidad y su módulo (repositorio, servicio, controlador, DTOs, mappings y pruebas) en este proyecto basado en la arquitectura Onion/Clean. Incluye precauciones para evitar romper la API y mantener compatibilidad.

Objetivo
- Mantener consistencia arquitectónica, seguridad (tenant / roles), y pruebas automatizadas.

Resumen del flujo (alto nivel)
- Domain -> DataAccess (EF) -> Repositorio/UnitOfWork -> BusinessLogic (Servicios) -> Controller/API -> Tests.

Antes de empezar
- Define los casos de uso (CRUD, búsquedas, reglas de negocio).
- Decide si la entidad es tenant-scoped (tiene CompanyId). Si sí, conservar CompanyId y seguridad tenant-aware.

Paso 1 — Modelo de dominio (Domain)
1. Crear la clase en Domain/ (ej.: Domain/MyModule/MyEntity.cs).
2. Heredar BaseEntity cuando la entidad tenga Id/CreationDate/ModifiedBy, para seguir convención.
3. Incluir propiedades simples y relaciones (IDs, colecciónes de navegación). Evitar lógicas complejas en el POCO.

Ejemplo:
```csharp
namespace Onion.Domain.MyModule
{
	public class MyEntity : BaseEntity
	{
		public int CompanyId { get; set; } // si es tenant-scoped
		public string Name { get; set; } = string.Empty;
		public decimal Price { get; set; }

		// Navigation example (optional)
		// public ICollection<MySubEntity>? Items { get; set; }
	}
}
```

Paso 2 — Configuración EF en DataAccess (si corresponde)
1. Si necesitas configuración especial (column types, precision, indices), añade una clase en DataAccess/Configurations/EntityConfigurations, implementando IEntityTypeConfiguration<MyEntity>.
2. Registrar DbSet<MyEntity> MyEntities en OnionDbContext.
3. Si la entidad usa decimal, especifica HasPrecision(...) o HasColumnType("decimal(18,6)") para evitar truncamiento.

Ejemplo de configuración:
```csharp
public class MyEntityConfiguration : IEntityTypeConfiguration<MyEntity>
{
	public void Configure(EntityTypeBuilder<MyEntity> builder)
	{
		builder.ToTable("MyEntities");
		builder.Property(e => e.Price).HasPrecision(18, 6);
		builder.HasIndex(e => new { e.CompanyId, e.Name }).IsUnique(false);
	}
}
```

Paso 3 — Repositorio y UnitOfWork
1. Si los métodos CRUD estándar son suficientes, reutiliza IRepository<T> y GenericRepository<T>.
2. Si necesitas consultas específicas, crea una interfaz en DataAccess/Repositories/Abstract (IMyEntityRepository : IRepository<MyEntity>) y su implementación en DataAccess/Repositories/Concrete.
3. Añade propiedad en IUnitOfWork (concrete IUnitOfWork en DataAccess/Repositories/Concrete/UnitOfWork.cs) y regístrala en el constructor.

Paso 4 — Migraciones EF
1. Desde la raíz del repo ejecutar (DataAccess es el proyecto donde va el DbContext y crud-onion es el host):
   - dotnet ef migrations add AddMyEntity -p DataAccess -s crud-onion
   - dotnet ef database update -p DataAccess -s crud-onion
2. Revisa la migración generada y ajusta precision/indices si fuera necesario.
3. Asegúrate de ejecutar las migraciones en un entorno de staging antes de producción.

Paso 5 — DTOs y AutoMapper
1. Crear DTOs en BusinessLogic/Dtos (p. ej. MyEntityDto, CreateMyEntityDto, UpdateMyEntityDto).
2. Agregar mapping en BusinessLogic/Profiles/MyModuleProfile.cs:
   - CreateMap<MyEntity, MyEntityDto>();
   - CreateMap<CreateMyEntityDto, MyEntity>().ForMember(d => d.Id, opt => opt.Ignore());
3. Evitar exponer entidades EF directamente en la API; usar DTOs.

Paso 6 — Servicios de negocio (BusinessLogic)
1. Crear interfaz IMyEntityService en BusinessLogic/Services/Abstract.
2. Implementar MyEntityService en BusinessLogic/Services/Concrete, inyectando IUnitOfWork.
3. Puntos críticos:
   - Validaciones de negocio -> lanzar CustomException con Error modelo cuando sea necesario.
   - Transacciones: si necesitas operación compuesta, usar uow.BeginTransactionAsync() + Commit/rollback.
   - Tenant safety: comprobar ownership antes de modificar recursos (puedes usar GenericRepository/UnitOfWork o IAuthorizationService para validar).

Ejemplo de método Create:
```csharp
public async Task<MyEntity> CreateAsync(CreateMyEntityDto dto, int companyId)
{
	var entity = new MyEntity { CompanyId = companyId, Name = dto.Name, Price = dto.Price };
	await _uow.MyEntities.AddAsync(entity);
	await _uow.SaveChangesAsync();
	return entity;
}
```

Paso 7 — Controller / API (crud-onion)
1. Crear controlador en crud-onion/Controllers/MyEntityController.cs.
2. Usar DTOs en los contratos y AutoMapper para mapear entre entidad y DTO.
3. Seguridad y tenant handling:
   - Si endpoint necesita el companyId del usuario para crear recurso: inyecta Onion.Common.Authorization.IAuthorizationService y llama TryGetCompanyId(User, out var companyId) y validar.
   - Si necesita autorización role/owner: usa [RequirePermission(...)] con route param cuando corresponda.
4. Mantener respuestas coherentes con ApiResponse<T> cuando el patrón del proyecto lo usa.

Ejemplo básico (crear):
```csharp
[Route("[controller]")]
public class MyEntityController : ControllerBase
{
	private readonly IMyEntityService _svc;
	private readonly IMapper _mapper;
	private readonly IAuthorizationService _auth;

	public MyEntityController(IMyEntityService svc, IMapper mapper, IAuthorizationService auth) { ... }

	[HttpPost]
	public async Task<IActionResult> Post([FromBody] CreateMyEntityDto dto)
	{
		if (!_auth.TryGetCompanyId(User, out var companyId)) return BadRequest(ApiResponse<string>.Fail("CompanyId claim missing"));
		var e = await _svc.CreateAsync(dto, companyId);
		var outDto = _mapper.Map<MyEntityDto>(e);
		return CreatedAtAction(nameof(Get), new { id = e.Id }, ApiResponse<object>.Ok(outDto));
	}
}
```

Paso 8 — DI y registro
1. Registrar servicio y repositorio en los módulos de DI existentes (ServiceCollection helpers en BusinessLogic/Configurations/DependencyInjection y DataAccess/Configurations):
   - services.AddScoped<IMyEntityService, MyEntityService>();
   - if you added custom repo: register it in UnitOfWork constructor.

Paso 9 — Validaciones
1. Añadir validadores (FluentValidation o validaciones manuales) para DTOs.
2. Validar datos en el servicio o en un pipeline (preferible en validator antes de llegar al servicio).

Paso 10 — Tests
1. Unit tests para servicio: mock IUnitOfWork y repositorios; probar reglas de negocio y errores esperados.
2. Unit tests para controlador: crear Fake services/authorization, probar respuestas 200/201/400/403/404.
3. Integration tests: WebApplicationFactory para endpoints, incluir tests de autorización (token con CompanyId y roles), y pruebas de ciclo completo.

Buenas prácticas para no romper la API
- No cambiar nombres públicos ni formatos JSON de DTOs existentes; si debes cambiar, versiona la API (/v2) o añadir campos opcionales.
- Mantén ApiResponse<T> y códigos HTTP coherentes con el resto de la API.
- No quites claims usados por clientes (CompanyId, role) de los tokens sin migración.
- Especifica precisión decimal en EF para evitar truncamientos.
- Registrar AutoMapper mappings y cubrir con tests unitarios para evitar sorpresas en serialización.
- Mantén los mensajes de error y códigos (Errors.Code) estables o documenta cambios.

Despliegue y migraciones
- Prueba migraciones en staging; usar backups antes de aplicar en producción.
- Si agregas índices/constraints, medir impacto en tablas grandes.

Checklist final antes de merge
- dotnet build
- dotnet test (unit + integration)
- Revisar swagger y contract tests
- Revisar migración SQL
- Revisar logs y errores locales

Exportar a PDF
- Usa pandoc o VS Code Markdown PDF si necesitas el PDF localmente.

Deuda técnica pendiente
- Reintroducir y refactorizar CompanySettingsControllerTests.cs para cobertura end-to-end.

Fin.

Fin de la guía — manos a la obra.
