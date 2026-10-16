# LUMEN API

API RESTful en .NET 10 para el Sistema de Gestión de Cursos Online LUMEN. Implementa registro e inicio de sesión, cursos versionados, auditorías, inscripciones, progreso y reportes.

## Arquitectura

La solución utiliza arquitectura N-Tier e inyección de dependencias por constructor. `API` recibe HTTP y resuelve autenticación, autorización por endpoint y traducción de errores; `BusinessLogic` valida reglas, permisos y transiciones; `DataAccess` concentra entidades, configuración EF Core, repositories y `LumenDbContext`. `Shared` contiene DTOs, enums y excepciones; `Migrations` contiene las migraciones y su ejecutor.

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

Requisitos:

- **SDK .NET 10.0.400**, según [global.json](global.json). La política `rollForward: latestPatch` permite parches posteriores dentro de la misma banda **10.0.4xx**; no cambia de banda ni de versión mayor o menor. El runtime por sí solo no alcanza.
- **Windows PowerShell 5.1 o PowerShell 7** para los bloques de esta guía. Los ejemplos de generación aleatoria son compatibles con ambas versiones.
- Acceso a NuGet para restaurar paquetes que no estén en la caché local. Bruno se necesita únicamente para utilizar su colección; la API y las pruebas .NET no lo requieren.

Desde una copia limpia, abrir PowerShell en la raíz del repositorio:

~~~powershell
Set-Location -LiteralPath '4-Codigo'
dotnet --version
dotnet restore Lumen.slnx
dotnet build Lumen.slnx -c Release --no-restore
~~~

Comprobar que cada comando termine correctamente. Todos los comandos restantes de esta guía parten de **4-Codigo**. Configurar JWT en la sección siguiente y después iniciar la API con el comando indicado allí.

- API: http://localhost:5080
- OpenAPI: http://localhost:5080/openapi/v1.json
- Scalar: http://localhost:5080/scalar/v1

Al arrancar, la API valida JWT antes de abrir la base y aplica las migraciones pendientes. La carga de datos demo depende del entorno, como se detalla más abajo. Verificar siempre el destino SQLite antes de ejecutar la API.

## Configuración JWT local y demostración

No existe una clave de firma predeterminada en los archivos versionados. La API requiere Jwt:Key con contenido no vacío y al menos 32 bytes UTF-8. La clave anterior del repositorio está revocada: suministrarla mediante cualquier proveedor también impide el arranque. Los errores de configuración no muestran la clave.

La firma continúa siendo HS256. La clave se interpreta como texto UTF-8, sin decodificar Base64 ni recortar caracteres. Emisión y validación utilizan las mismas opciones, estables durante la ejecución. Cambiar una clave requiere reiniciar la API y volver a iniciar sesión; los tokens firmados con la clave anterior dejan de ser válidos.

### Desarrollo local: User Secrets

Cada persona que ejecute el proyecto localmente debe configurar su propia clave en su equipo, sin compartirla. El proyecto ya declara UserSecretsId; no ejecutar nuevamente dotnet user-secrets init. Ese identificador es público y no contiene credenciales.

Ejecutar una vez por equipo desde 4-Codigo, antes de iniciar la API. Este procedimiento genera 32 bytes aleatorios criptográficos, conserva su representación Base64 como texto y la envía por entrada estándar a Secret Manager. No imprime la clave ni la incorpora como literal al historial de comandos:

~~~powershell
$jwtBytes = New-Object byte[] 32
$jwtGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $jwtGenerator.GetBytes($jwtBytes)
    $jwtValue = [Convert]::ToBase64String($jwtBytes)
    @{ 'Jwt:Key' = $jwtValue } |
        ConvertTo-Json -Compress |
        dotnet user-secrets set --project API/Lumen.API.csproj
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo guardar la configuración JWT local.' }
}
finally {
    $jwtGenerator.Dispose()
    [Array]::Clear($jwtBytes, 0, $jwtBytes.Length)
    Remove-Variable jwtBytes, jwtValue, jwtGenerator -ErrorAction SilentlyContinue
}
~~~

User Secrets guarda datos fuera del repositorio, en el perfil del usuario, sin cifrarlos. Es una herramienta de desarrollo local. No ejecutar dotnet user-secrets list ni abrir secrets.json durante una grabación: exponen sus valores.

Después de compilar, iniciar la API. El perfil `http` establece la dirección 5080 y `ASPNETCORE_ENVIRONMENT=Development`. El bloque fija también `DOTNET_ENVIRONMENT`, que tiene precedencia, y restaura su valor al cerrar la API con Ctrl+C:

~~~powershell
$previousDotnetEnvironment = [Environment]::GetEnvironmentVariable('DOTNET_ENVIRONMENT', 'Process')
try {
    $env:DOTNET_ENVIRONMENT = 'Development'
    dotnet run --project API/Lumen.API.csproj -c Release --no-build --no-restore --launch-profile http
}
finally {
    [Environment]::SetEnvironmentVariable('DOTNET_ENVIRONMENT', $previousDotnetEnvironment, 'Process')
    Remove-Variable previousDotnetEnvironment
}
~~~

### Alternativa: variable de entorno del proceso

Jwt__Key corresponde a Jwt:Key. Las variables de entorno tienen precedencia sobre User Secrets y appsettings; los argumentos de línea de comandos tienen precedencia sobre ambas. Una variable antigua puede bloquear el arranque aunque User Secrets tenga una clave nueva. No suministrar secretos como argumentos ni guardarlos en launchSettings.json, archivos .bru, .env o appsettings.

Para una ejecución temporal sin almacenar una clave personal, este ejemplo genera una clave solo para la sesión de la API. Ejecutarlo en una terminal dedicada, desde 4-Codigo:

~~~powershell
$previousJwtKey = [Environment]::GetEnvironmentVariable('Jwt__Key', 'Process')
$previousDotnetEnvironment = [Environment]::GetEnvironmentVariable('DOTNET_ENVIRONMENT', 'Process')
$jwtBytes = New-Object byte[] 32
$jwtGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try {
    $jwtGenerator.GetBytes($jwtBytes)
    $env:Jwt__Key = [Convert]::ToBase64String($jwtBytes)
    $env:DOTNET_ENVIRONMENT = 'Development'
    dotnet run --project API/Lumen.API.csproj -c Release --no-build --no-restore --launch-profile http
}
finally {
    [Environment]::SetEnvironmentVariable('Jwt__Key', $previousJwtKey, 'Process')
    [Environment]::SetEnvironmentVariable('DOTNET_ENVIRONMENT', $previousDotnetEnvironment, 'Process')
    $jwtGenerator.Dispose()
    [Array]::Clear($jwtBytes, 0, $jwtBytes.Length)
    Remove-Variable previousJwtKey, previousDotnetEnvironment, jwtBytes, jwtGenerator
}
~~~

Cada ejecución de ese ejemplo cambia la clave e invalida los tokens anteriores. Las variables de entorno también son accesibles al proceso y no sustituyen un almacén de secretos productivo. Fuera de Development, no se carga User Secrets automáticamente: proporcionar Jwt__Key por un medio externo al repositorio.

### Exposición con SQLite temporal y Bruno

Seguir el [README de Bruno](bruno/README.md): contiene el bloque completo para iniciar una API en **Testing**, con clave efímera, SQLite temporal absoluta y puerto **5081**. No necesita guardar una clave personal ni usar la base habitual. Seleccionar el environment `demo-temporal` y ejecutar los 19 requests de `00-Exposicion` en orden.

La clave JWT permanece en la API. Bruno obtiene tokens mediante los logins y los conserva con `bru.setVar` como variables runtime. Repetir los logins al cambiar de servidor o clave. No publicar respuestas de login, encabezados Bearer ni exports con tokens.

En Windows, el arranque excluye el proveedor Windows EventLog y conserva Console y los demás proveedores. No se requiere un filtro ambiental que silencie EventLog ni permisos para escribir en el registro de eventos.

### Pruebas JWT

LumenApiFactory genera una clave aleatoria por instancia y la configura en memoria, sin variables globales ni User Secrets personales. Las fábricas derivadas conservan la clave de su raíz; fábricas independientes no aceptan los tokens de la otra. La validación de configuración también se aplica en Testing, sin excepciones ni claves predeterminadas.

JwtConfigurationTests verifica claves ausentes/vacías/cortas, longitud UTF-8 y rechazo antes de abrir o crear SQLite. JwtSecurityIntegrationTests comprueba login y roles, firmas inválidas, vencimiento, issuer/audience, aislamiento y estabilidad de opciones. El vector público RevokedJwtTestVector contiene claims ficticios y una firma precomputada de la clave revocada; no contiene la clave ni un token legítimo del entorno actual.

Referencias: [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0), [configuración y precedencia](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0) y [variables runtime de Bruno](https://docs.usebruno.com/variables/runtime-variables).

## Inicialización por entorno y usuarios demo

`DbInitializer` aplica migraciones en todos los entornos. Solo crea los datos demo si el entorno es **Development o Testing** y la tabla de usuarios está vacía. Si ya existen usuarios, no los elimina, modifica ni completa con otras cuentas demo.

| Entorno | Esquema y migraciones | Datos demo automáticos |
|---|---|---|
| Development | Sí | Solo si no existen usuarios |
| Testing | Sí | Solo si no existen usuarios |
| Production u otro entorno | Sí | No |

Sin un entorno explícito, la API utiliza Production y el ejecutor de migraciones tampoco genera demos. El perfil `http` y los bloques anteriores seleccionan Development explícitamente. Las pruebas y el recorrido temporal de Bruno utilizan Testing.

Una Production vacía puede arrancar sin cuentas. Su primer Administrador requiere aprovisionamiento controlado fuera de este recorrido; el registro público sigue permitiendo únicamente Alumno y Profesor. Esta política no elimina cuentas demo que ya existieran.

En una base nueva de Development o Testing, las cuentas demo siguientes utilizan la contraseña pública de prueba `Lumen123!`:

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

La API expone **29 operaciones**. El documento generado en [OpenAPI](http://localhost:5080/openapi/v1.json) incluye esquemas de respuesta, códigos HTTP, Bearer JWT por operación, enums y los parámetros obligatorios de reportes. [Scalar](http://localhost:5080/scalar/v1) permite explorarlo. Registro y login son públicos; las otras 27 operaciones requieren autenticación y los permisos indicados abajo. En la API temporal de Bruno, utilizar el puerto 5081.

OpenAPI es la referencia técnica del contrato HTTP actual; las fichas académicas anteriores conservan propuestas de trazabilidad de etapas previas.

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

La API aplica las migraciones incluidas al iniciar; no es necesario instalar `dotnet-ef` ni crear una migración nueva para ejecutar esta copia.

Para usar el ejecutor por separado, después de compilar Release, este ejemplo crea una base temporal nueva y carga demos en Testing. El argumento `--connection` establece su destino; el ejecutor no toma `ConnectionStrings__Lumen` de la configuración de la API:

~~~powershell
$migrationDatabase = Join-Path ([IO.Path]::GetTempPath()) ('lumen-migrations-' + [Guid]::NewGuid().ToString('N') + '.db')
$previousDotnetEnvironment = [Environment]::GetEnvironmentVariable('DOTNET_ENVIRONMENT', 'Process')
try {
    $env:DOTNET_ENVIRONMENT = 'Testing'
    dotnet run --project Migrations/Lumen.Migrations.csproj -c Release --no-build --no-restore -- "--connection=Data Source=$migrationDatabase;Pooling=False"
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron aplicar las migraciones.' }
}
finally {
    [Environment]::SetEnvironmentVariable('DOTNET_ENVIRONMENT', $previousDotnetEnvironment, 'Process')
    Remove-Variable previousDotnetEnvironment
}
~~~

El ejemplo no requiere JWT y deja la base en `$migrationDatabase`. No cambia la base utilizada por la API. Para migrar un destino elegido, detener las instancias que lo usan y proporcionar su ruta absoluta en el mismo argumento `--connection`. El ejecutor usa `DOTNET_ENVIRONMENT`, luego `ASPNETCORE_ENVIRONMENT`; Production, un entorno desconocido o la ausencia de ambos aplican solamente el esquema. No cambia la política de cuentas existentes.

## Validación

~~~powershell
dotnet build Lumen.slnx -c Release --no-restore
dotnet test Lumen.slnx -c Release --no-build --no-restore
~~~

Las pruebas unitarias usan xUnit y Moq sin base de datos. Las pruebas de integración usan WebApplicationFactory y bases SQLite únicas en el directorio temporal, con pooling deshabilitado y limpieza al finalizar. Las pruebas de migración de CU-06 usan SQLite en memoria. No utilizan las bases lumen.db de desarrollo.

La última auditoría del código aprobó **350 pruebas: 101 unitarias y 249 de integración**, incluidas las regresiones JWT, concurrencia, EventLog, OpenAPI e inicialización por entorno. No se necesita configurar una clave personal para ejecutarlas: las fábricas de integración generan claves independientes en memoria.

Las suites EnrollmentCreationServiceTests, EnrollmentCreationIntegrationTests y EnrollmentPersistenceIntegrationTests verifican CU-06, incluyendo HTTP, identidad JWT, persistencia sin alterar cursos/revisiones/módulos, concurrencia, carreras con pausa/finalización, nueva solicitud tras rechazo/cancelación, progreso separado y migración de datos existentes.

## Bruno

La colección contiene **50 requests** y dos environments: `local` para el puerto 5080 y `demo-temporal` para el 5081. El [README de Bruno](bruno/README.md) documenta preparación, arranque aislado, precondiciones, orden y limpieza.

`00-Exposicion` contiene **19 requests**: autentica los tres roles, crea un curso de tres módulos, lo publica mediante auditoría, solicita y aprueba una inscripción y registra progreso hasta 100 %, incluida una repetición idempotente. Ejecutar esa carpeta completa en orden, una iteración y concurrencia 1. Los 31 requests generales tienen precondiciones propias y no deben ejecutarse como una secuencia automática de toda la raíz.

En la auditoría se verificaron 69 llamadas HTTP reales sobre los 50 requests distintos y **171 tests Bruno aprobados**, mediante los parsers y motores ScriptRuntime/TestRuntime NodeVM del Bruno instalado. La interfaz gráfica y QuickJS no forman parte de esa comprobación. Los tokens e IDs se mantienen en variables runtime; no guardarlos en environments ni compartir exports o capturas que los contengan.
