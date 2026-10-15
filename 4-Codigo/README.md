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

Requiere el SDK .NET 10 y una clave JWT externa. Configurar primero la clave según la sección siguiente. Desde 4-Codigo:

~~~powershell
dotnet restore
dotnet run --project API/Lumen.API.csproj
~~~

- API: http://localhost:5080
- OpenAPI: http://localhost:5080/openapi/v1.json
- Scalar: http://localhost:5080/scalar/v1

Al arrancar, la API valida la configuración JWT antes de abrir la base. Si es válida, DbInitializer aplica las migraciones pendientes y carga datos demo de forma idempotente. Verificar siempre el destino SQLite antes de ejecutar la API.

## Configuración JWT local y demostración

No existe una clave de firma predeterminada en los archivos versionados. La API requiere Jwt:Key con contenido no vacío y al menos 32 bytes UTF-8. La clave anterior del repositorio está revocada: suministrarla mediante cualquier proveedor también impide el arranque. Los errores de configuración no muestran la clave.

La firma continúa siendo HS256. La clave se interpreta como texto UTF-8, sin decodificar Base64 ni recortar caracteres. Emisión y validación utilizan las mismas opciones, estables durante la ejecución. Cambiar una clave requiere reiniciar la API y volver a iniciar sesión; los tokens firmados con la clave anterior dejan de ser válidos.

### Rodrigo y Nahuel: User Secrets

Cada integrante debe configurar su propia clave en su equipo, sin compartirla. El proyecto ya declara UserSecretsId; no ejecutar nuevamente dotnet user-secrets init. Ese identificador es público y no contiene credenciales.

Ejecutar una sola vez desde 4-Codigo, antes de compartir pantalla. Este procedimiento genera 32 bytes aleatorios criptográficos, conserva su representación Base64 como texto y la envía por entrada estándar a Secret Manager. No imprime la clave ni la incorpora como literal al historial de comandos:

~~~powershell
$jwtBytes = [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
try {
    $jwtValue = [Convert]::ToBase64String($jwtBytes)
    @{ 'Jwt:Key' = $jwtValue } |
        ConvertTo-Json -Compress |
        dotnet user-secrets set --project API/Lumen.API.csproj
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo guardar la configuración JWT local.' }
}
finally {
    Remove-Variable jwtBytes, jwtValue -ErrorAction SilentlyContinue
}
~~~

User Secrets guarda datos fuera del repositorio, en el perfil del usuario, sin cifrarlos. Es una herramienta de desarrollo local. No ejecutar dotnet user-secrets list ni abrir secrets.json durante una grabación: exponen sus valores.

El perfil http configura Development, que habilita la carga de User Secrets:

~~~powershell
dotnet run --project API/Lumen.API.csproj --launch-profile http
~~~

### Alternativa: variable de entorno del proceso

Jwt__Key corresponde a Jwt:Key. Las variables de entorno tienen precedencia sobre User Secrets y appsettings; los argumentos de línea de comandos tienen precedencia sobre ambas. Una variable antigua puede bloquear el arranque aunque User Secrets tenga una clave nueva. No suministrar secretos como argumentos ni guardarlos en launchSettings.json, archivos .bru, .env o appsettings.

Para una ejecución temporal sin almacenar una clave personal, este ejemplo genera una clave solo para la sesión de la API. Ejecutarlo en una terminal dedicada, desde 4-Codigo:

~~~powershell
$env:Jwt__Key = [Convert]::ToBase64String(
    [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
try {
    dotnet run --project API/Lumen.API.csproj --launch-profile http
}
finally {
    Remove-Item Env:Jwt__Key -ErrorAction SilentlyContinue
}
~~~

Cada ejecución de ese ejemplo cambia la clave e invalida los tokens anteriores. Las variables de entorno también son accesibles al proceso y no sustituyen un almacén de secretos productivo. Fuera de Development, no se carga User Secrets automáticamente: proporcionar Jwt__Key por un medio externo al repositorio.

### Exposición con SQLite temporal y Bruno

Configurar la clave antes de mostrar la pantalla. Para mantener la demostración aislada, en una terminal dedicada con User Secrets configurado:

~~~powershell
$demoDatabase = Join-Path ([IO.Path]::GetTempPath()) ("lumen-demo-" + [Guid]::NewGuid().ToString('N') + '.db')
$env:ConnectionStrings__Lumen = "Data Source=$demoDatabase;Pooling=False"
try {
    dotnet run --project API/Lumen.API.csproj --launch-profile http
}
finally {
    Remove-Item Env:ConnectionStrings__Lumen -ErrorAction SilentlyContinue
    Remove-Variable demoDatabase
}
~~~

La base de esa demostración queda en el directorio temporal; este comando no elimina sus archivos. La clave JWT no se envía a Bruno. El cliente obtiene tokens mediante los tres logins y los guarda con bru.setVar como variables runtime, sin modificar el environment versionado. Los requests conservan {{studentToken}}, {{teacherToken}} y {{adminToken}}. Repetir los logins después de cerrar Bruno o cambiar de servidor/clave. No publicar respuestas de login, encabezados Bearer ni exports con tokens.

El resto de la sintaxis de la colección Bruno y el problema ambiental de EventLog siguen pendientes de etapas separadas; esta corrección JWT no los resuelve.

### Pruebas JWT

LumenApiFactory genera una clave aleatoria por instancia y la configura en memoria, sin variables globales ni User Secrets personales. Las fábricas derivadas conservan la clave de su raíz; fábricas independientes no aceptan los tokens de la otra. La validación de configuración también se aplica en Testing, sin excepciones ni claves predeterminadas.

JwtConfigurationTests verifica claves ausentes/vacías/cortas, longitud UTF-8 y rechazo antes de abrir o crear SQLite. JwtSecurityIntegrationTests comprueba login y roles, firmas inválidas, vencimiento, issuer/audience, aislamiento y estabilidad de opciones. El vector público RevokedJwtTestVector contiene claims ficticios y una firma precomputada de la clave revocada; no contiene la clave ni un token legítimo del entorno actual.

Referencias: [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0), [configuración y precedencia](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0) y [variables runtime de Bruno](https://docs.usebruno.com/variables/runtime-variables).

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

### CU-05: edición, persistencia y concurrencia

PUT /api/courses/{id} guarda la revisión de trabajo. POST /api/courses/{id}/submit-review valida la información completa y registra la solicitud de auditoría. Profesor opera sobre cursos propios; Administrador puede gestionar todos los cursos. Alumno no puede editar ni enviar solicitudes de auditoría.

La actualización recibe la lista completa de módulos en su orden final. Un ID existente conserva la identidad del módulo; un ID nulo agrega uno nuevo. Los módulos omitidos se retiran únicamente de la revisión de trabajo. IDs vacíos, repetidos o ajenos a esa revisión devuelven ModuleCourseMismatchException (409). Los recursos son referencias textuales, no archivos cargados por este endpoint.

El servicio valida y construye los cambios antes de modificar entidades rastreadas. Comprueba elementos nulos, cupo no negativo, duración positiva y suma representable como int. Aplica los límites ya definidos por el modelo: título y nombre de módulo 200 caracteres; descripción del curso 4000; categoría, nivel y modalidad 100; descripción de módulo 2000; contenido 8000; observación de auditoría 2000. Las entradas inválidas generan errores controlados (400). Guardar un borrador permite información incompleta y cero módulos; enviar requiere los campos principales, cupo positivo, al menos un módulo y contenido o recursos en cada módulo.

El repositorio guarda la retirada explícita, la modificación y el agregado de módulos en una única transacción. Para intercambiar posiciones bajo el índice único (RevisionId, Order), utiliza posiciones negativas temporales y luego aplica el orden positivo final. Esas posiciones nunca se confirman como resultado de la actualización: cualquier error revierte toda la transacción, incluido UpdatedAtUtc.

EF usa como tokens de concurrencia los campos existentes UpdatedAtUtc y WorkingRevisionId de Course, UpdatedAtUtc y Status de CourseRevision, y Decision de CourseReview. El servicio hace avanzar estrictamente la fecha de actualización, aun si el reloj devuelve un valor igual o anterior. Las escrituras comprueban los valores originales; un conflicto se traduce a CourseStateConflictException (409). Esta protección cubre envíos duplicados, ediciones concurrentes con el envío, primeras actualizaciones simultáneas y decisiones administrativas tardías. No agrega columnas ni requiere una migración. La creación de una revisión y el cambio del puntero de trabajo son atómicos; el envío guarda la solicitud y la transición juntos. Una decisión solo se aplica a una solicitud pendiente de la revisión de trabajo vigente.

Los siguientes aspectos funcionales siguen abiertos en las fuentes oficiales. CU-05 conserva el comportamiento previo, sin establecer reglas nuevas: la edición depende del estado de la revisión aunque el curso esté Pausado o Finalizado; guardar correcciones de CambiosSolicitados pasa a Borrador; objetivos y requisitos sugeridos pueden estar vacíos; alumnos y progreso usan la publicación vigente, conservando finalizaciones por ID de módulo. Las solicitudes conservan decisiones y observaciones, pero no una fotografía inmutable del contenido corregido posteriormente.

Las pruebas CourseUpdateServiceTests, CourseUpdateIntegrationTests y CourseConcurrencyIntegrationTests cubren permisos, validaciones, módulos, rollback con fallo de persistencia, versionado, auditoría, acceso y progreso. Las pruebas de concurrencia fuerzan dos lecturas del mismo estado antes de confirmar la primera escritura y comprobar que la segunda devuelve 409 sin cambios parciales. Las bases de integración son SQLite únicas en el directorio temporal, con pooling deshabilitado y limpieza al finalizar; no usan lumen.db de desarrollo.

LumenApiFactory conserva un único constructor público params IInterceptor[], compatible con las llamadas sin argumentos, con un interceptor individual (incluido DbCommandInterceptor) y con un arreglo, como en CU-06. La configuración adicional de CU-05 se establece mediante la propiedad init ConfigureTestServices; no compite con la resolución del constructor. Un arreglo de interceptores nulo o un elemento nulo se rechaza explícitamente. LumenApiFactoryCompatibilityTests compila y ejecuta esas formas de llamada sobre el host aislado. Las carreras entre edición de módulos y envío modifican contenido, recursos, duración, orden y composición; comprueban tanto una escritura tardía como la liberación conjunta de ambas operaciones, con un único éxito y un conflicto 409.

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

Abrir la carpeta bruno como colección y seleccionar el environment local. Los tres requests de login conservan los tokens de Alumno, Profesor y Administrador en variables runtime mediante bru.setVar, solo si el login responde 200 con accessToken. No guardar tokens en local.bru ni compartir exports o capturas que los contengan. La clave de firma permanece exclusivamente en la API. Los demás requests mantienen las limitaciones de sintaxis identificadas en la auditoría, pendientes de otra etapa.
