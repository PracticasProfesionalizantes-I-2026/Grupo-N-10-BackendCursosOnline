# Bruno — ejecución y exposición de LUMEN

La colección contiene 50 requests: los 31 originales y los 19 de `00-Exposicion`. Los dos environments son `local` (API habitual, puerto 5080) y `demo-temporal` (API aislada, puerto 5081). Para este recorrido usar exclusivamente `demo-temporal`.

## 1. Iniciar una API con SQLite temporal

Requisitos: SDK **.NET 10.0.400** (roll-forward `latestPatch` dentro de 10.0.4xx), Windows PowerShell **5.1** o PowerShell **7**, y Bruno instalado. Ver también la [guía del backend](../README.md). Desde la raíz del repositorio, abrir una terminal PowerShell dedicada y ejecutar el bloque completo. El puerto 5081 debe estar libre; si está ocupado, detener únicamente la instancia temporal propia o elegir otro puerto y adaptar `baseUrl` antes de continuar.

```powershell
Set-Location -LiteralPath '4-Codigo'

dotnet restore Lumen.slnx
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron restaurar los paquetes.' }
dotnet build Lumen.slnx -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar Release.' }

$demoDirectory = Join-Path ([IO.Path]::GetTempPath()) ('lumen-bruno-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $demoDirectory | Out-Null
$demoDatabase = Join-Path $demoDirectory 'lumen.db'
$demoEntropy = New-Object byte[] 32
$demoRandom = [Security.Cryptography.RandomNumberGenerator]::Create()
$demoRandom.GetBytes($demoEntropy)
$demoRandom.Dispose()
$demoSettings = @{
    DOTNET_ENVIRONMENT = 'Testing'
    ASPNETCORE_ENVIRONMENT = 'Testing'
    Jwt__Key = [Convert]::ToBase64String($demoEntropy)
    ConnectionStrings__Lumen = "Data Source=$demoDatabase;Pooling=False"
}
[Array]::Clear($demoEntropy, 0, $demoEntropy.Length)
$demoPrevious = @{}
try {
    foreach ($name in $demoSettings.Keys) {
        $demoPrevious[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $demoSettings[$name], 'Process')
    }
    dotnet run --project API/Lumen.API.csproj -c Release --no-build --no-restore --no-launch-profile --urls http://127.0.0.1:5081
}
finally {
    foreach ($name in $demoPrevious.Keys) {
        [Environment]::SetEnvironmentVariable($name, $demoPrevious[$name], 'Process')
    }
    $demoSettings.Clear()
    $demoPrevious.Clear()
}
```

La clave aleatoria es efímera, no es una clave personal ni se guarda en archivos. `Testing` evita cargar User Secrets de desarrollo; la conexión absoluta dirige la inicialización y las migraciones solamente a la nueva SQLite temporal. La API ya excluye Windows EventLog y conserva Console; no es necesario desactivar el proveedor mediante variables de entorno.

Esperar a que la API escuche en 5081. La inicialización incorpora las cuentas demo existentes del proyecto: `alumno@lumen.local`, `profesor@lumen.local` y `admin@lumen.local`. Los logins ya contienen la contraseña pública de esos fixtures; no introducir credenciales personales. Mantener abierta esta terminal durante la demostración.

El seeding automático solo se permite en Development o Testing y cuando no existen usuarios. Production aplica migraciones y puede iniciar vacía, pero no crea demos. El ejecutor de migraciones respeta la misma política; no elimina ni modifica cuentas existentes.

## 2. Ejecutar desde Bruno

1. Abrir como colección esta carpeta, donde está `bruno.json`.
2. Seleccionar el environment **demo-temporal** y comprobar `baseUrl = http://127.0.0.1:5081`.
3. Para reproducir el motor validado, seleccionar **Developer Mode / NodeVM** en la configuración del sandbox JavaScript de Bruno. Los scripts utilizan únicamente las APIs incluidas de Bruno y Chai.
4. Ejecutar solamente la carpeta **00-Exposicion**, en orden ascendente de `seq`, con una iteración y concurrencia 1. También se puede enviar cada request manualmente en ese orden, dentro de la misma sesión de la colección.
5. Detener el recorrido ante un error HTTP, de script o de test. No continuar con identificadores de otra ejecución. Ejecutar nuevamente la carpeta completa vuelve a autenticar y crea otro curso con nuevos IDs.

No ejecutar toda la raíz como una secuencia automática: las solicitudes generales incluyen edición, cancelación y finalización y tienen precondiciones diferentes. El recorrido principal no cancela inscripciones ni finaliza cursos.

## 3. Secuencia exacta

Todos los nombres de esta tabla están dentro de `00-Exposicion/`. `S`, `P` y `A` representan `studentToken`, `teacherToken` y `adminToken`, siempre runtime. Todas las solicitudes requieren `baseUrl`. Los IDs y snapshots indicados también son runtime. La dependencia identifica los pasos que deben haber terminado correctamente.

| Request | Rol | Variables de entrada | Variables generadas/actualizadas | HTTP | Depende de |
|---|---|---|---|---|---|
| `01-login-alumno.bru` | Público → Alumno | Credenciales demo del body | S, `studentUserId` | 200 | API temporal iniciada |
| `02-login-profesor.bru` | Público → Profesor | Credenciales demo del body | P, `teacherUserId` | 200 | API temporal iniciada |
| `03-login-admin.bru` | Público → Administrador | Credenciales demo del body | A, `adminUserId`, `userId` | 200 | API temporal iniciada |
| `04-crear-curso.bru` | Profesor | P | `courseId`; limpia contexto del curso anterior | 201 | 02 |
| `05-enviar-revision.bru` | Profesor | P, `courseId` | `reviewId`, `reviewRevisionId` | 200 | 04 |
| `06-auditorias-pendientes.bru` | Administrador | A, `courseId`, `reviewId` | — | 200 | 03, 05 |
| `07-aprobar-auditoria.bru` | Administrador | A, `courseId`, `reviewId` | — | 200 | 06 |
| `08-solicitar-inscripcion.bru` | Alumno | S, `studentUserId`, `courseId` | `enrollmentId`; limpia contexto de módulos | 201 | 01, 07 |
| `09-inscripciones-pendientes.bru` | Administrador | A, `enrollmentId`, `courseId` | — | 200 | 08 |
| `10-aprobar-inscripcion.bru` | Administrador | A, `enrollmentId`, `courseId` | — | 200 | 09 |
| `11-contenido-publicado.bru` | Alumno | S, `enrollmentId`, `courseId` | `moduleIds`, `moduleId` | 200 | 10 |
| `12-progreso-inicial.bru` | Alumno | S, `enrollmentId`, `courseId`, `studentUserId` | `progressSnapshot` | 200 | 11; espera 0% |
| `13-completar-modulo-1.bru` | Alumno | S, `enrollmentId`, `courseId`, `studentUserId`, `moduleIds` | `moduleId` primero, `progressSnapshot` | 200 | 12; espera 33% |
| `14-progreso-parcial.bru` | Alumno | S, `enrollmentId`, `courseId`, `studentUserId` | `progressSnapshot` | 200 | 13; espera 33% |
| `15-completar-modulo-2.bru` | Alumno | S, `enrollmentId`, `courseId`, `studentUserId`, `moduleIds` | `moduleId` segundo, `progressSnapshot` | 200 | 14; espera 67% |
| `16-completar-modulo-3.bru` | Alumno | S, `enrollmentId`, `courseId`, `studentUserId`, `moduleIds` | `moduleId` tercero, `progressSnapshot` | 200 | 15; espera 100% |
| `17-repetir-modulo-3.bru` | Alumno | S, `enrollmentId`, `courseId`, `studentUserId`, `moduleIds`, `progressSnapshot` | `moduleId`, `repeatSnapshot`, `progressSnapshot` | 200 | 16; mismo 100% y mismo conjunto completado |
| `18-progreso-final-alumno.bru` | Alumno | S, `enrollmentId`, `courseId`, `studentUserId` | `progressSnapshot` | 200 | 17; espera 100% |
| `19-progreso-final-profesor.bru` | Profesor | P, `courseId`, `enrollmentId`, `studentUserId` | — | 200 | 18; mismo alumno al 100% |

El paso 04 crea explícitamente un curso controlado de tres módulos (30, 45 y 60 minutos). El recorrido muestra CU-03 (04–05), CU-04 (06–07), CU-07 (09–10), CU-16 (11, 13, 15–17) y CU-08 (12, 14, 18–19), con los logins y la solicitud de inscripción como operaciones auxiliares.

## 4. Captura y comprobaciones

- Los logins obtienen `accessToken` e identidad de respuestas reales, validan el rol y guardan con `bru.setVar`. Antes del login eliminan el token anterior del mismo rol.
- `courseId` sale de `POST /api/courses`, campo `id`.
- `reviewId` sale de `POST /api/courses/{courseId}/submit-review`, campo **`id` de la auditoría**. `revisionId` se conserva por separado como `reviewRevisionId` y nunca sustituye al ID usado para decidir la auditoría.
- `enrollmentId` sale de `POST /api/enrollments`, campo `id`, correlacionado con curso y alumno actuales.
- `moduleIds` y `moduleId` salen de `GET /api/enrollments/{enrollmentId}/course-content`, campos `modules[*].id` de la revisión publicada. No se toman los IDs del borrador ni una fila arbitraria de una lista.
- Los tests verifican HTTP, GUIDs, estados, correlaciones, duración, porcentaje, módulos únicos y ausencia de duplicación al completar nuevamente el mismo módulo. El porcentaje reproduce el redondeo del backend; nunca se envía un porcentaje como entrada.

No hay tokens ni IDs de una ejecución guardados en los environments. No exportar variables runtime ni publicar respuestas/capturas del login. Al cambiar de base o reiniciar la API con otra clave, ejecutar nuevamente los tres logins y el recorrido desde el comienzo.

## 5. Requests generales conservados

Las 31 solicitudes de `Auth`, `Users`, `Courses`, `Catalog`, `Enrollments` y `Reports` conservan métodos, rutas, cuerpos y roles originales. Ahora tienen bloques multilínea, guardas y aserciones. Su numeración dentro de cada carpeta no define un recorrido global válido.

| Operación | Precondición |
|---|---|
| `Auth/01`–`03` | Usuarios demo inicializados; generan tokens e identidades |
| `Auth/04-register`, `Users/03-create-admin` | Emails de sus bodies todavía libres; el segundo requiere Administrador |
| `Users/01-list`, `Users/02-detail` | Administrador autenticado; detalle usa `userId` del login de Administrador |
| `Courses/01-list-manageable`, `03-create` | Profesor autenticado |
| `Courses/02-detail`, `04-update`, `05-submit-review` | Curso propio creado en la misma base; editar solo con estado editable, luego enviar a revisión |
| `Courses/09-pending-reviews`, `10-my-reviews`, `11-resolve-review` | Administrador para pendientes/decisión; Profesor para auditorías propias; decidir usa `reviewId` de submit-review |
| `Catalog/01-list`, `02-detail` | Alumno autenticado en estos requests; detalle requiere `courseId` publicado y disponible |
| `Enrollments/01-create`, `02-mine` | Alumno autenticado; crear exige curso disponible y sin otra inscripción pendiente/aprobada del mismo alumno |
| `Enrollments/03-list-pending`, `06-resolve` | Administrador autenticado; decisión requiere `enrollmentId` pendiente |
| `Enrollments/04-detail` | Alumno titular autenticado; requiere `enrollmentId` de su inscripción |
| `Enrollments/05-course-content`, `08-progress`, `09-complete-module` | Alumno titular con inscripción aprobada; obtener contenido antes de completar un módulo |
| `Enrollments/10-course-progress` | Profesor propietario; `courseId` del curso publicado |
| `Reports/01-summary` | Administrador; intervalo de fechas válido del request original |
| `Courses/06-pause`, `07-resume` | Profesor propietario; publicado → pausado → reanudado |
| `Courses/08-finish` | Administrador autenticado; finalizar al terminar las comprobaciones |
| `Enrollments/07-cancel` | Alumno titular; cancelar después de consultar/completar módulos |

Para reproducir la ejecución completa de los 31 originales sobre una base temporal nueva: Auth 01–04 → Users 01–03 → Courses 01, 03, 02, 04, 05, 09, 10, 11 → Catalog 01–02 → Enrollments 01, 02, 03, 04, 06, 05, 08, 09, 10 → Reports 01 → Courses 06, 07 → Enrollments 07 → Courses 08. Esos números se refieren a los prefijos de archivo de cada carpeta, no a los pasos de exposición.

Repetir registro/alta de Administrador en la misma base produce 409 por email existente; esto es una precondición del contrato, no se disimula aceptando errores como éxitos. Esas altas no forman parte de `00-Exposicion`, que sí puede repetirse íntegramente en la misma base temporal creando otro curso.

## 6. Cerrar y limpiar

Detener únicamente la API temporal con Ctrl+C en su terminal. El `finally` restaura las variables de proceso previas. La base queda en `$demoDirectory` para inspección si se desea. Para eliminar solo esa base y sus archivos auxiliares, en la misma terminal y con la API ya detenida:

```powershell
$demoResolved = [IO.Path]::GetFullPath($demoDirectory)
$demoTempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
if (-not $demoResolved.StartsWith($demoTempRoot, [StringComparison]::OrdinalIgnoreCase) -or
    -not ([IO.Path]::GetFileName($demoResolved)).StartsWith('lumen-bruno-')) {
    throw 'La ruta no corresponde a la base temporal de esta demostración.'
}
foreach ($file in @('lumen.db', 'lumen.db-wal', 'lumen.db-shm', 'lumen.db-journal')) {
    $demoFile = Join-Path $demoResolved $file
    if (Test-Path -LiteralPath $demoFile) { Remove-Item -LiteralPath $demoFile }
}
Remove-Item -LiteralPath $demoResolved
```

## 7. Validación realizada

Bruno instalado: **4.2.1**. Parser incluido `@usebruno/lang`: **0.40.0**. Parseo satisfactorio de **50 requests y 2 environments**, usando los parsers respectivos. Comparación contra los contratos anteriores: los 31 requests originales conservan método, URL, body JSON y autenticación.

Se ejecutaron **69 llamadas HTTP reales correspondientes a los 50 requests distintos**: dos recorridos completos de exposición (19 + 19) y los 31 originales en el orden indicado. Resultado: **171 tests Bruno aprobados, 0 fallidos**; cada test agrupa una o más aserciones. Los tres tokens y los cuatro identificadores requeridos se propagaron por variables runtime, sin escribir environments. Se utilizó una API con clave JWT efímera y SQLite temporal absoluta; se detuvo esa instancia y se eliminó su base al terminar.

La ejecución se automatizó con un harness que leyó los `.bru` mediante el parser instalado, envió HTTP real a la API y ejecutó scripts/tests con **ScriptRuntime/TestRuntime NodeVM y Chai del Bruno instalado**. No fueron respuestas simuladas. Los resultados corresponden al motor NodeVM: no se ejecutaron la interfaz gráfica, un CLI nativo de Bruno ni QuickJS.

La auditoría posterior del backend aprobó **350 pruebas .NET** (101 unitarias y 249 de integración), incluidas las correcciones de concurrencia, EventLog, OpenAPI y seeding por entorno. Los comandos para reproducirlas están en la [guía del backend](../README.md).
