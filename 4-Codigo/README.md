# LUMEN API

API RESTful en .NET 10 para el Sistema de Gestión de Cursos Online LUMEN. Implementa registro e inicio de sesión, cursos versionados, auditorías, inscripciones, progreso y reportes.

## Arquitectura

La solución utiliza arquitectura N-Tier e inyección de dependencias por constructor.

~~~text
Cliente HTTP
    ↓
API / Controller
    ↓
BusinessLogic / Service
    ↓
DataAccess / Repository
    ↓
LumenDbContext
    ↓
SQLite
~~~

~~~text
4-Codigo/
├── API/                         Controllers, JWT, OpenAPI y arranque
├── BusinessLogic/               Interfaces, services y reglas de negocio
├── DataAccess/                  Entidades, EF Core, repositories y DbInitializer
├── Shared/                      DTOs, enums y excepciones tipadas
├── Migrations/                  Migraciones y ejecutor de inicialización
├── Tests/
│   ├── Lumen.BusinessLogic.Tests/
│   └── Lumen.API.IntegrationTests/
├── bruno/                       Colección de requests
├── Lumen.slnx
└── global.json
~~~

Los controllers no contienen reglas de negocio. Los services validan permisos, datos y transiciones; los repositories realizan únicamente consultas y persistencia.

## Requisitos y ejecución

Requiere el SDK .NET 10. Desde 4-Codigo:

~~~powershell
dotnet restore
dotnet run --project API/Lumen.API.csproj
~~~

- API: http://localhost:5080
- OpenAPI: http://localhost:5080/openapi/v1.json
- Scalar: http://localhost:5080/scalar/v1

Al arrancar, DbInitializer aplica las migraciones pendientes y carga datos demo de forma idempotente.

## Usuarios demo

Todos utilizan la contraseña Lumen123!.

| Rol | Email |
|---|---|
| Alumno | alumno@lumen.local |
| Profesor | profesor@lumen.local |
| Administrador | admin@lumen.local |

## Persistencia y versiones

SQLite se configura mediante ConnectionStrings:Lumen. La base predeterminada es lumen.db.

Un curso mantiene dos referencias independientes:

- PublishedRevisionId: última versión aprobada visible en el catálogo y para alumnos.
- WorkingRevisionId: borrador o versión actualmente sometida a auditoría.

Al editar un curso publicado se crea una revisión de trabajo nueva. La versión pública anterior permanece intacta hasta que el Administrador aprueba la auditoría. Si se solicitan cambios o se rechaza la revisión, el catálogo continúa usando la última revisión publicada.

## Autenticación

La API usa JWT Bearer.

~~~http
POST /api/auth/login
Content-Type: application/json

{
  "email": "alumno@lumen.local",
  "password": "Lumen123!"
}
~~~

Respuesta 200:

~~~json
{
  "accessToken": "<jwt>",
  "expiresAtUtc": "2026-09-10T22:00:00Z",
  "user": {
    "id": "00000000-0000-0000-0000-000000000000",
    "email": "alumno@lumen.local",
    "role": "Alumno",
    "firstName": "María",
    "lastName": "Pérez",
    "isActive": true
  }
}
~~~

Los endpoints protegidos reciben el encabezado:

~~~http
Authorization: Bearer <jwt>
~~~

## Endpoints

### Autenticación y usuarios

| Método | Ruta | Acceso |
|---|---|---|
| POST | /api/auth/register | Público |
| POST | /api/auth/login | Público |
| GET | /api/users | Administrador |
| GET | /api/users/{id} | Administrador |
| POST | /api/users/administrators | Administrador |

### Catálogo, cursos y auditorías

| Método | Ruta | Acceso |
|---|---|---|
| GET | /api/catalog/courses | Autenticado |
| GET | /api/catalog/courses/{id} | Autenticado |
| GET | /api/courses | Profesor / Administrador |
| GET | /api/courses/{id} | Profesor propietario / Administrador |
| POST | /api/courses | Profesor / Administrador |
| PUT | /api/courses/{id} | Profesor propietario / Administrador |
| POST | /api/courses/{id}/submit-review | Profesor propietario / Administrador |
| POST | /api/courses/{id}/pause | Profesor propietario / Administrador |
| POST | /api/courses/{id}/resume | Profesor propietario / Administrador |
| POST | /api/courses/{id}/finish | Administrador |
| GET | /api/course-reviews | Administrador |
| GET | /api/course-reviews/mine | Profesor / Administrador |
| POST | /api/course-reviews/{id}/decision | Administrador |

Ejemplo de creación:

~~~json
{
  "title": "Introducción a .NET",
  "description": "Fundamentos de desarrollo con .NET",
  "category": "Programación",
  "level": "Inicial",
  "modality": "Online",
  "maxCapacity": 30,
  "learningObjectives": ["Crear aplicaciones básicas"],
  "suggestedPrerequisites": ["Programación básica"],
  "modules": [
    {
      "name": "Fundamentos",
      "description": "Primeros conceptos",
      "durationMinutes": 90,
      "content": "Material del módulo",
      "resources": ["guia.pdf"]
    }
  ]
}
~~~

El curso se crea como Borrador; el envío se realiza mediante submit-review.

### Inscripciones y progreso

| Método | Ruta | Acceso |
|---|---|---|
| POST | /api/enrollments | Alumno |
| GET | /api/enrollments/mine | Alumno |
| GET | /api/enrollments?status=Pendiente | Administrador |
| GET | /api/enrollments/{id} | Alumno propietario / Administrador |
| GET | /api/enrollments/{id}/course-content | Alumno con inscripción aprobada |
| POST | /api/enrollments/{id}/decision | Administrador |
| POST | /api/enrollments/{id}/cancel | Alumno propietario / Administrador |
| GET | /api/enrollments/{id}/progress | Alumno, Profesor propietario o Administrador |
| PUT | /api/enrollments/{id}/modules/{moduleId}/completion | Alumno propietario |
| GET | /api/courses/{courseId}/students/progress | Profesor propietario / Administrador |

Solicitud de inscripción:

~~~json
{ "courseId": "00000000-0000-0000-0000-000000000000" }
~~~

Resolución administrativa:

~~~json
{ "decision": "Aprobada" }
~~~

El progreso se deriva de los módulos completados de la versión publicada vigente. Una inscripción cancelada conserva el historial, pero no permite consultar ni registrar progreso mientras no esté aprobada.

### Reportes

| Método | Ruta | Acceso |
|---|---|---|
| GET | /api/reports/summary?from=2026-01-01&to=2026-12-31 | Administrador |

## Errores HTTP

Las excepciones de negocio son tipadas y se convierten explícitamente en cada controller.

| HTTP | Uso |
|---|---|
| 400 | Datos o rango de fechas inválidos |
| 401 | Credenciales inválidas o JWT ausente |
| 403 | Rol, propiedad o acceso no permitido |
| 404 | Recurso inexistente |
| 409 | Duplicados o transición incompatible |

## Migraciones

~~~powershell
dotnet ef migrations add NombreMigracion --project Migrations/Lumen.Migrations.csproj --startup-project Migrations/Lumen.Migrations.csproj --context LumenDbContext --output-dir DatabaseMigrations
dotnet run --project Migrations/Lumen.Migrations.csproj
~~~

## Validación

~~~powershell
dotnet build
dotnet test
~~~

Las pruebas unitarias usan xUnit y Moq sin base de datos. Las pruebas de integración usan WebApplicationFactory y una base SQLite aislada.

## Bruno

Abrir la carpeta bruno como colección y seleccionar el environment local. Ejecutar primero los requests de login para guardar los tokens de Alumno, Profesor y Administrador.
