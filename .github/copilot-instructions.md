# Copilot Instructions

## Project Guidelines
- El usuario prefiere evitar datos hardcodeados en el backend y que los datos operativos provengan exclusivamente de la base de datos.
- En el FE SaaS, la renovación de tokens debe usar un HttpClient directo creado con HttpBackend para evitar interceptores y dependencias circulares.
- La invalidación de sesión debe permanecer desacoplada de AuthService.
- El servicio de roles debe usar /roles, sin fallback /Role.
- El acceso de sidebar/rutas debe cerrarse si falla la carga de licencias.