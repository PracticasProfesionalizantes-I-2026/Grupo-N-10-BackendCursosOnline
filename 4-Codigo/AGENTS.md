# AGENTS.md

## Propósito

Este directorio contiene la API RESTful de LUMEN, un LMS académico con usuarios, cursos versionados, auditorías, inscripciones y progreso.

## Alcance

- Trabajar únicamente dentro de 4-Codigo.
- No modificar la documentación funcional ni el frontend del repositorio padre.
- No exponer entidades de EF Core desde controllers.
- No agregar funcionalidades sin respaldo en los casos de uso.

## Comandos

~~~powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project API/Lumen.API.csproj
dotnet run --project Migrations/Lumen.Migrations.csproj
~~~

OpenAPI: http://localhost:5080/openapi/v1.json

Scalar: http://localhost:5080/scalar/v1

## Arquitectura obligatoria

~~~text
Controller → Service → Repository → LumenDbContext → SQLite
~~~

- API: transporte HTTP, JWT, autorización y traducción explícita de excepciones.
- BusinessLogic: reglas, permisos, transiciones y mapeos manuales a DTO.
- DataAccess: entidades, Fluent API, repositories, DbContext e inicialización.
- Shared: DTOs, enums y excepciones tipadas.
- Migrations: migraciones y ejecutor de inicialización.

Controllers y repositories no contienen reglas de negocio. Los services no construyen consultas LINQ-to-Entities ni acceden al DbContext.

## Persistencia

- EF Core 10 con SQLite.
- Guid asignados por métodos de creación de repositories.
- AsNoTracking en consultas de solo lectura.
- DeleteBehavior.Restrict en relaciones.
- No existen operaciones DELETE documentadas.
- DbInitializer debe permanecer idempotente.

## Versionado de cursos

Course.PublishedRevisionId y Course.WorkingRevisionId resuelven necesidades diferentes.

- Catálogo, contenido del alumno y progreso leen PublishedRevision.
- Edición y auditoría trabajan sobre WorkingRevision.
- Editar una revisión publicada crea una revisión nueva.
- Una aprobación reemplaza el puntero publicado.
- Rechazo o cambios solicitados no modifican la versión publicada anterior.

No simplificar ambos punteros en un único estado o registro.

## Reglas principales

- Registro público: solo Alumno y Profesor.
- Administradores: creación interna por otro Administrador.
- Profesor: solo administra cursos propios.
- Envío a revisión: datos obligatorios y al menos un módulo.
- Duración: suma de duraciones de módulos.
- Inscripción: solo sobre curso públicamente disponible.
- No más de una inscripción Pendiente o Aprobada por alumno y curso.
- Acceso y progreso: inscripción Aprobada.
- Progreso: derivado de módulos completados, nunca recibido como porcentaje.
- Profesor: solo consulta progreso de sus cursos.
- Reportes: Desde menor o igual que Hasta.

## Convenciones

- Mantener nullable reference types habilitado.
- Usar métodos privados MapToResponseDTO en services.
- Cada excepción de negocio vive en su propio archivo en Shared/Exceptions.
- Mantener respuestas 400, 401, 403, 404 y 409 coherentes.
- Agregar unit tests con Moq y tests de integración para contratos HTTP.
- Actualizar Bruno y README cuando cambien endpoints.
