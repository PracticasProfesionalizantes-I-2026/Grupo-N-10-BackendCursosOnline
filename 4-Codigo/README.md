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

### CU-06: criterios técnicos propuestos

Estos criterios completan aspectos que los requisitos originales no definen expresamente y fueron acordados para esta implementación:

- Después de una inscripción Rechazada o Cancelada se permite una nueva solicitud Pendiente con otro identificador. Se conserva el historial y no se copia el progreso anterior.
- Durante una nueva auditoría se admiten solicitudes si el curso conserva PublishedRevisionId y su estado operativo es Activo. Los cambios sin aprobar no sustituyen la versión pública.
- Curso inexistente devuelve 404; curso existente sin publicación, Pausado o Finalizado devuelve 409.
- CourseId ausente o vacío devuelve 400. Una identidad autenticada sin identificador de alumno válido devuelve 403. Los identificadores de alumno y el estado recibidos como campos extra del cuerpo no se utilizan.
- No se incorporan restricciones de cupo a la solicitud.

El servicio conserva las verificaciones de rol, identidad, curso y duplicados. El repositorio inserta solamente la nueva fila mediante ExecuteSqlInterpolatedAsync de EF, con parámetros y un SELECT condicionado a la publicación y al estado operativo requerido por el servicio. La comprobación final y la inserción forman una única sentencia: si pausa/finalización se confirma antes, la solicitud se rechaza; si la inserción se confirma primero, la solicitud queda registrada y la transición posterior no la cancela. Esto evita insertar el grafo del curso obtenido con AsNoTracking.

SQLite garantiza una única inscripción Pendiente o Aprobada por alumno/curso mediante un índice único parcial sobre StudentId y CourseId. Las solicitudes concurrentes que infringen esa restricción se traducen específicamente a DuplicateEnrollmentException (409); otros errores de persistencia no se presentan como duplicados.

Referencias técnicas: [sentencias y transacciones SQLite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions) e [índices de EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/indexes).

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

La migración incremental EnforceSingleActiveEnrollment agrega el índice único parcial sin cambiar la migración inicial ni el historial de inscripciones. Si ya existen duplicados Pendiente/Aprobada, su aplicación falla y no los elimina ni cambia sus estados automáticamente.

Antes de aplicar esta migración a una base existente, verificar la ruta absoluta de destino, conservar un respaldo y ejecutar sobre ese destino esta consulta de diagnóstico:

~~~sql
SELECT StudentId, CourseId, COUNT(*) AS ActiveCount
FROM Enrollments
WHERE Status IN ('Pendiente', 'Aprobada')
GROUP BY StudentId, CourseId
HAVING COUNT(*) > 1;
~~~

Si devuelve filas, la resolución de esas inconsistencias requiere una decisión sobre los registros; no debe inferirse qué solicitud conservar. Solo después de comprobar el destino y resolver los conflictos puede aplicarse la migración. El arranque de la API aplica migraciones automáticamente, por lo que esta comprobación debe hacerse antes de arrancarla contra una base existente.

~~~powershell
dotnet ef migrations add NombreMigracion --project Migrations/Lumen.Migrations.csproj --startup-project Migrations/Lumen.Migrations.csproj --context LumenDbContext --output-dir DatabaseMigrations
dotnet run --project Migrations/Lumen.Migrations.csproj
~~~

## Validación

~~~powershell
dotnet build
dotnet test
~~~

Las pruebas unitarias usan xUnit y Moq sin base de datos. Las pruebas de integración usan WebApplicationFactory y bases SQLite únicas en el directorio temporal, con pooling deshabilitado y limpieza al finalizar. Las pruebas de migración de CU-06 usan SQLite en memoria. No utilizan las bases lumen.db de desarrollo.

Las suites EnrollmentCreationServiceTests, EnrollmentCreationIntegrationTests y EnrollmentPersistenceIntegrationTests verifican CU-06, incluyendo HTTP, identidad JWT, persistencia sin alterar cursos/revisiones/módulos, concurrencia, carreras con pausa/finalización, nueva solicitud tras rechazo/cancelación, progreso separado y migración de datos existentes.

## Bruno

Abrir la carpeta bruno como colección y seleccionar el environment local. Ejecutar primero los requests de login para guardar los tokens de Alumno, Profesor y Administrador.
