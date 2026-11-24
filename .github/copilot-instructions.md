# Copilot Instructions

## Project Guidelines
- El usuario prefiere evitar datos hardcodeados en el backend y que los datos operativos provengan exclusivamente de la base de datos.
- Para el backend CuadreEnv SaaS, priorizar implementación completa y validación final; mantener multitenancy estricto por CompanyId, respuestas ApiResponse globales, transacciones ACID para inventario/caja/NCF, y preservar las convenciones de contratos del frontend.
- En el FE SaaS, la renovación de tokens debe usar un HttpClient directo creado con HttpBackend para evitar interceptores y dependencias circulares.
- La invalidación de sesión debe permanecer desacoplada de AuthService.
- El servicio de roles debe usar /roles, sin fallback /Role.
- El acceso de sidebar/rutas debe cerrarse si falla la carga de licencias.
- Para módulos SaaS, preservar literalmente los IDs granulares de AllowedModulesJson en la respuesta de sesión y claims JWT; usar aliases únicamente para autorización. Los endpoints críticos deben exigir el módulo granular correspondiente o wildcard, sin inyectar módulos adicionales para usuarios no SuperAdmin.
- Posponer el control de permisos por roles/módulos y permitir temporalmente que todos los usuarios autenticados accedan a todas las operaciones del sistema, dejando la autorización para una etapa posterior.
- Priorizar auditorías de producción y correcciones de integridad/reliabilidad, especialmente IDs que no llegan, contratos entre capas, persistencia y fallos intermitentes; proteger el funcionamiento completo antes de hacer cambios cosméticos.
- Para CuadreEnv, los endpoints de almacén deben procesar productDetails en una única transacción y movimiento, conservar costos e idempotencia; las notas de crédito deben exponer cantidades originales/acreditadas y ajustar factura, stock y cuentas por cobrar; los clientes deben aceptar campos opcionales y mantener el cliente contado por tenant sin vulnerar el aislamiento.