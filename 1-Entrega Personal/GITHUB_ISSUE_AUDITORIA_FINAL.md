# [Auditoría Académica] Plan Integral de Remediación y Ajustes de Entrega — LUMEN

> **Materia:** Práctica Profesionalizante I — Tecnicatura Superior en Desarrollo de Software (Ciclo 2026)  
> **Equipo evaluado:** Rodrigo Gabriel Astore, Nahuel Ferrino  
> **Cátedra:** Prof. Gabriel Majluf, Prof. Oscar Spila  
> **Estado:** Aprobado con Observaciones (Requiere subsanación para defensa final)  

Estimados Rodrigo y Nahuel:

A partir del análisis integral que realizamos contrastando **Requerimientos vs. Casos de Uso**, **Documentación vs. Implementación en .NET 10** y el **Análisis Estático de Calidad de Código (Clean Code / QA)**, consolidamos a continuación la lista definitiva de observaciones y tareas de remediación.

El trabajo técnico en el backend es sobresaliente y evidencia un compromiso genuino con las buenas prácticas de la ingeniería de software. No obstante, para garantizar la consistencia plena del proyecto ante el tribunal evaluador, deben resolver los siguientes puntos clasificados por nivel de criticidad. Utilicen este checklist para coordinar las correcciones del equipo.

---

## 🔴 RIESGO ALTO
*Crítico para que el software compile, arranque o preserve la integridad operativa del dominio.*

- [ ] **[BUG-CRIT-01] Flexibilizar la política de SDK en `global.json` para permitir compilación multiplataforma**
  - **Archivo afectado:** `4-Codigo/global.json` (Líneas 1-6)
  - **Problema:** La configuración actual (`version: "10.0.403"` con `rollForward: "latestPatch"`) restringe el uso exclusivamente a parches de la banda `10.0.4xx`. En entornos de corrección docente o servidores de CI con versiones previas de .NET 10 (por ejemplo, `10.0.103`), el CLI aborta inmediatamente la compilación con código de error 1 antes de restaurar paquetes.
  - **Acción requerida:** Modificar la directiva de roll-forward para admitir versiones menores compatibles de .NET 10:
    ```json
    {
      "sdk": {
        "rollForward": "minor",
        "version": "10.0.100"
      }
    }
    ```

- [ ] **[BUG-CRIT-02] Incorporar guía amigable y captura de excepciones al arranque por falta de configuración JWT**
  - **Archivos afectados:** `4-Codigo/API/Program.cs` (Línea 88) y `4-Codigo/API/Security/JwtOptions.cs` (Líneas 23-41)
  - **Problema:** Si se clona el proyecto en un equipo nuevo y se ejecuta `dotnet run` sin haber configurado previamente User Secrets o la variable `Jwt__Key`, `Program.cs` arroja una excepción no controlada (`OptionsValidationException`) que destruye el proceso sin un mensaje claro en la consola.
  - **Acción requerida:** Envolver la resolución inicial de opciones en un bloque `try-catch` dentro de `Program.cs` que imprima en consola un mensaje instructivo claro (explicando cómo ejecutar el bloque de PowerShell para inicializar `dotnet user-secrets`) y termine el proceso con un código de salida controlado (`Environment.Exit(1)`).

- [ ] **[BUG-CRIT-03] Implementar Segregación de Funciones (SoD) en la Auditoría de Cursos**
  - **Archivo afectado:** `4-Codigo/BusinessLogic/Services/CourseService.cs` (Método `ResolveReviewAsync`, Líneas 208-256)
  - **Problema:** Un usuario con rol `Administrador` puede crear un curso, enviarlo a revisión y luego auditarse y aprobarse a sí mismo la solicitud. Esto vulnera el principio de control de cuatro ojos (*Four-Eyes Principle*) en la moderación de contenidos académicos.
  - **Acción requerida:** Agregar una validación explícita al resolver la revisión:
    ```csharp
    if (review.RequestedByUserId == currentUser.UserId)
    {
        throw new ForbiddenOperationException("Un administrador no puede auditar ni aprobar solicitudes de cursos creados por él mismo.");
    }
    ```
    Añadir la prueba de integración correspondiente en `CourseReviewHistoryIntegrationTests.cs` verificando que devuelva HTTP 403 Forbidden.

- [ ] **[GAP-04] Actualizar la Sección 5.2 del Informe General con los NFRs reales del Backend**
  - **Archivo afectado:** `1-Entrega Personal/1 - Informe/Lumen_Informe_General.md` (Sección 5.2) y su versión Word
  - **Problema:** La sección de Requerimientos No Funcionales quedó redactada con foco en un prototipo navegable sin backend (RNF-03 y RNF-07). La arquitectura real implementó mecanismos industriales avanzados que no están respaldados formalmente en los requerimientos.
  - **Acción requerida:** Incorporar en la Sección 5.2 los 4 NFRs de arquitectura de backend:
    - **RNF-15 (Concurrencia Optimista):** Obligatoriedad de encabezados `ETag` e `If-Match` (RFC 6585) en edición y recuperación de borradores.
    - **RNF-16 (Inmutabilidad por Triggers):** Garantía física en SQLite contra modificación o borrado de snapshots de auditoría y reducciones de cupo no autorizadas.
    - **RNF-17 (Rate Limiting de Autenticación):** Restricción de 20 peticiones por minuto por IP con respuesta HTTP 429 y `Retry-After`.
    - **RNF-18 (Seguridad Criptográfica JWT):** Clave externa mínima de 32 bytes UTF-8 con revocación y bloqueo de identidades demo en producción.

---

## 🟡 RIESGO MEDIO
*Afecta la mantenibilidad del código, acoplamiento de servicios o consistencia de especificaciones funcionales.*

- [ ] **[SMELL-01] Descomponer la God Class `CourseService` respetando el Principio de Responsabilidad Única (SRP)**
  - **Archivo afectado:** `4-Codigo/BusinessLogic/Services/CourseService.cs` (533 líneas)
  - **Problema:** Concentra catálogo público, CRUD de cursos, ciclo de vida operacional (pausar/reanudar/finalizar), lógica de auditoría (`CourseReview`), serialización de snapshots y normalización profunda de entradas.
  - **Acción requerida:** Extraer la lógica de auditorías a un nuevo servicio desacoplado:
    - Crear `ICourseReviewService` y `CourseReviewService` para alojar `SubmitForReviewAsync`, `GetPendingReviewsAsync`, `GetMyReviewsAsync`, `GetReviewSnapshotAsync` y `ResolveReviewAsync`.
    - Extraer las rutinas estáticas de validación y normalización a una clase interna `CourseValidator`.

- [ ] **[SMELL-02] Eliminar duplicación de código (DRY) entre `AuthService` y `UserService`**
  - **Archivos afectados:** `4-Codigo/BusinessLogic/Services/AuthService.cs` (Líneas 34-50) y `4-Codigo/BusinessLogic/Services/UserService.cs` (Líneas 41-57)
  - **Problema:** Duplican idénticamente el bloque de instanciación de la entidad `User`, normalización de strings, hashing de contraseña con salt y persistencia en repositorio.
  - **Acción requerida:** Centralizar la creación y hasheo de usuarios en un método factoría de dominio (por ejemplo `User.Create(...)` o un método privado compartido).

- [ ] **[SMELL-03] Reubicar el endpoint de seguimiento de alumnos en el controlador canónico correspondiente**
  - **Archivos afectados:** `4-Codigo/API/Controllers/EnrollmentsController.cs` (Línea 98) y `CoursesController.cs`
  - **Problema:** El endpoint `GET /api/courses/{courseId:guid}/students/progress` está declarado dentro de `EnrollmentsController`, violando la cohesión REST sobre el recurso `/courses`.
  - **Acción requerida:** Trasladar la acción a `CoursesController` inyectando `IProgressService`.

- [ ] **[GAP-01] Subsanar referencias funcionales rotas en cabeceras de CU-01 y CU-10**
  - **Archivos afectados:** `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-01-registrarse.md` y `cu-10-consultar-reportes.md` (y sus versiones Word)
  - **Problema:** Ambas fichas declaran erróneamente *"No especificadas en el documento fuente"* en la celda de referencias funcionales.
  - **Acción requerida:**
    - En **CU-01:** Reemplazar por `RF-01, RF-02, RF-03, RF-04; RN-01, RN-02; políticas VAL-002.`
    - En **CU-10:** Reemplazar por `RF-33, RF-34, RF-35; RN-16; políticas VAL-003.`

- [ ] **[GAP-02] Incorporar formalmente el Requerimiento Funcional de Completitud de Módulos (RF-27b)**
  - **Archivo afectado:** `1-Entrega Personal/1 - Informe/Lumen_Informe_General.md` (Sección 5.1.3) y `cu-16-realizar-modulos-o-actividades-del-curso.md`
  - **Problema:** El caso de uso `CU-16` implementa la acción de completar módulos, pero en los requerimientos solo se enunció el cálculo matemático del porcentaje (RF-27), quedando la interacción del alumno huérfana de requerimiento declarativo.
  - **Acción requerida:** Añadir en la Sección 5.1.3:
    > **RF-27b:** El sistema debe permitir al Alumno con inscripción APROBADA registrar como completado cada módulo perteneciente a la versión publicada del curso, recalculando su progreso en forma inmediata e idempotente.
    Vincular `RF-27b` en la cabecera de `cu-16-realizar-modulos-o-actividades-del-curso.md`.

- [ ] **[GAP-05] Corregir el Actor Principal en `CU-01 Registrarse`**
  - **Archivos afectados:** `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/Lumen_Actores_CasosDeUso.md` (Sección 1) y `cu-01-registrarse.md`
  - **Problema:** Se asignó como actor principal a *"Alumno / Profesor"*. Quien ejecuta el registro no posee cuenta ni rol activo en el sistema.
  - **Acción requerida:** Incorporar al actor **Visitante (o Usuario No Registrado)** en el catálogo de actores y asignarlo como actor principal en `CU-01`.

- [ ] **[GAP-07] Desacoplar documentalmente las metas divergentes de `CU-12`**
  - **Archivo afectado:** `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-12-consultar-inscripciones-y-acceder-a-cursos.md`
  - **Problema:** Fusiona en una sola ficha dos intenciones disjuntas: consultar el estado administrativo de solicitudes y acceder al aula virtual con los contenidos publicados (lo que forzó en la API dos endpoints separados: `/enrollments/mine` y `/course-content`).
  - **Acción requerida:** Dividir la especificación en dos subflujos o fichas autónomas: **CU-12A** (*Consultar Solicitudes*) y **CU-12B** (*Acceder a Contenidos del Curso Aprobado*).

- [ ] **[SMELL-06] Desacoplar la serialización de Snapshots de auditoría del DTO de presentación**
  - **Archivo afectado:** `4-Codigo/BusinessLogic/Services/CourseService.cs` (Líneas 176 y 205)
  - **Problema:** Se serializa a JSON directamente la clase `CourseResponseDTO`. Cambios futuros en propiedades del DTO romperán la deserialización de snapshots históricos congelados en base de datos.
  - **Acción requerida:** Modelar un esquema de snapshot dedicado e inmutable (`CourseReviewSnapshotPayload`) con versión de esquema explícita.

---

## 🟢 RIESGO BAJO
*Deuda técnica menor, mejoras de rendimiento localizadas y consistencia de mensajes.*

- [ ] **[BUG-MEN-01] Validar `ModelState.IsValid` antes de comprobar nulidad en `ReportsController`**
  - **Archivo afectado:** `4-Codigo/API/Controllers/ReportsController.cs` (Líneas 21-29)
  - **Problema:** Enviar fechas sintácticamente inválidas (ej. `?from=abc`) arroja *"Falta el parámetro obligatorio: from."* porque el binder deja la variable en null al fallar el parseo.
  - **Acción requerida:** Evaluar `if (!ModelState.IsValid)` y responder con un error de formato claro antes de comprobar si `from` o `to` tienen valor.

- [ ] **[BUG-MEN-02] Incorporar cota superior de negocio al Cupo Máximo (`MaxCapacity`)**
  - **Archivo afectado:** `4-Codigo/BusinessLogic/Services/CourseService.cs` (Línea 313)
  - **Problema:** Solo valida `MaxCapacity <= 0`, permitiendo valores arbitrarios como 2.000.000.000.
  - **Acción requerida:** Restringir el cupo a un límite lógico: `if (dto.MaxCapacity is <= 0 or > 10_000) throw new LumenValidationException("El cupo debe estar entre 1 y 10.000 alumnos.");`.

- [ ] **[BUG-MEN-03] Homogeneizar la semántica del campo `ToUtc` en el DTO de Reportes**
  - **Archivo afectado:** `4-Codigo/BusinessLogic/Services/ReportService.cs` (Líneas 31-32 y 39)
  - **Problema:** Con `timeZone` devuelve `00:00:00 UTC` del día final; sin `timeZone` devuelve el valor crudo recibido.
  - **Acción requerida:** Documentar en el DTO y OpenAPI la semántica formal del campo o retornar consistentemente el extremo superior del intervalo evaluado.

- [ ] **[BUG-MEN-04] Reemplazar el incremento artificial de ticks por un token de concurrencia numérico**
  - **Archivo afectado:** `4-Codigo/BusinessLogic/Services/CourseService.cs` (Líneas 401-410)
  - **Problema:** `NextTimestamp` suma `AddTicks(1)` cuando las operaciones ocurren en el mismo instante, desplazando artificialmente fechas hacia el futuro.
  - **Acción requerida:** Evaluar a futuro el reemplazo por una columna `RowVersion` o contador entero de revisión para el versionado optimista.

- [ ] **[BUG-MEN-05] Sanear la búsqueda de cadenas de error SQLite en `UserRepository`**
  - **Archivo afectado:** `4-Codigo/DataAccess/Repositories/UserRepository.cs` (Línea 21)
  - **Problema:** Busca `"Users.NormalizedEmail'"` con una comilla de cierre que acopla la lógica a una versión de driver puntual.
  - **Acción requerida:** Quitar la comilla simple final buscando `"Users.NormalizedEmail"` como en los demás repositorios.

- [ ] **[SMELL-05] Precompilar la expresión regular de validación de Código Postal**
  - **Archivo afectado:** `4-Codigo/BusinessLogic/Services/ServiceGuard.cs` (Línea 80)
  - **Problema:** Invoca `Regex.IsMatch` estático sin timeout en cada registro de usuario.
  - **Acción requerida:** Usar el source generator de .NET 10:
    ```csharp
    [GeneratedRegex(@"^(?:[0-9]{4}|[A-Za-z][0-9]{4}[A-Za-z]{3})$", RegexOptions.None, matchTimeoutMilliseconds: 250)]
    private static partial Regex PostalCodeRegex();
    ```

- [ ] **[GAP-06] Documentar el procedimiento de *Bootstraping* del Administrador inicial**
  - **Archivo afectado:** `1-Entrega Personal/1 - Informe/Lumen_Informe_General.md` (RF-06)
  - **Problema:** No se explicita cómo se genera el primer usuario administrador en un despliegue productivo limpio.
  - **Acción requerida:** Añadir una nota operativa en RF-06 aclarando que la primera cuenta se aprovisiona mediante script seguro o seeder fuera de banda.

- [ ] **[GAP-08] Desacoplar las precondiciones de Pausar y Reanudar en `CU-14`**
  - **Archivo afectado:** `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-14-pausar-o-reanudar-curso.md`
  - **Problema:** Agrupa dos acciones con precondiciones opuestas (`Publicado` vs `Pausado`).
  - **Acción requerida:** Separar claramente en dos subflujos autónomos con sus respectivas precondiciones y postcondiciones.

---

## 3. Criterio de Aceptación para la Defensa Final

Para la aprobación definitiva del proyecto en la mesa examinadora, se exigirá:
1. Resolución del **100% de los ítems de RIESGO ALTO**.
2. Resolución de al menos el **80% de los ítems de RIESGO MEDIO** (priorizando la descomposición de `CourseService`, la desduplicación de usuarios y la actualización documental).
3. Compilación limpia sin errores ni advertencias con `dotnet build Lumen.slnx -c Release`.
4. Ejecución exitosa de la suite completa de pruebas (+416 tests) con `dotnet test Lumen.slnx -c Release --no-build`.

¡Felicitaciones nuevamente por el excelente nivel técnico demostrado! Quedamos a disposición ante cualquier duda puntual sobre la implementación de estas mejoras.

