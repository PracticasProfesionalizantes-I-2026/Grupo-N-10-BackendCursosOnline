# Seguimiento de issues docentes de LUMEN

Registro maestro de las 20 issues docentes. Ronda 1: #14–#17 (ALTO), integrada en main. Ronda 2: #18–#25 (MEDIO), preparada en esta rama. Las ocho de riesgo BAJO siguen pendientes. Estado local; las issues de GitHub no se modifican.

La [matriz de trazabilidad](AUDITORIA_Y_PLAN_DE_CORRECCIONES.md#matriz-de-trazabilidad) vincula las correcciones verificadas con requerimientos, casos de uso, código, pruebas, Bruno/OpenAPI y documentos.

| Issue | Título | Riesgo | Estado |
|---|---|---|---|
| [#14](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/14) | [BUG-CRIT-01] Flexibilizar la política de SDK en global.json | ALTO | COMPLETADA |
| [#15](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/15) | [BUG-CRIT-02] Captura amigable al arranque por falta de configuración JWT | ALTO | COMPLETADA |
| [#16](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/16) | [BUG-CRIT-03] Implementar Segregación de Funciones (SoD) en Auditoría de Cursos | ALTO | COMPLETADA |
| [#17](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/17) | [GAP-04] Actualizar Sección 5.2 del Informe General con NFRs de Backend | ALTO | COMPLETADA |
| [#18](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/18) | [SMELL-01] Descomponer la God Class CourseService respetando SRP | MEDIO | COMPLETADA |
| [#19](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/19) | [SMELL-02] Eliminar duplicación de código (DRY) entre AuthService y UserService | MEDIO | COMPLETADA |
| [#20](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/20) | [SMELL-03] Reubicar endpoint de progreso de alumnos en CoursesController | MEDIO | COMPLETADA |
| [#21](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/21) | [GAP-01] Reparar referencias funcionales rotas en cabeceras de CU-01 y CU-10 | MEDIO | COMPLETADA |
| [#22](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/22) | [GAP-02] Incorporar Requerimiento Funcional de Completitud de Módulos (RF-27b) | MEDIO | COMPLETADA |
| [#23](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/23) | [GAP-05] Corregir Actor Principal en CU-01 Registrarse asignando Visitante | MEDIO | COMPLETADA |
| [#24](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/24) | [GAP-07] Desacoplar documentalmente las metas divergentes de CU-12 | MEDIO | COMPLETADA |
| [#25](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/25) | [SMELL-06] Desacoplar serialización de Snapshots del DTO de presentación | MEDIO | COMPLETADA |
| [#26](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/26) | [BUG-MEN-01] Validar ModelState.IsValid en ReportsController | BAJO | PENDIENTE |
| [#27](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/27) | [BUG-MEN-02] Incorporar cota superior de negocio al Cupo Máximo (MaxCapacity) | BAJO | PENDIENTE |
| [#28](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/28) | [BUG-MEN-03] Homogeneizar la semántica del campo ToUtc en el DTO de Reportes | BAJO | PENDIENTE |
| [#29](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/29) | [BUG-MEN-04] Reemplazar incremento artificial de ticks por token de versión numérico | BAJO | PENDIENTE |
| [#30](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/30) | [BUG-MEN-05] Sanear búsqueda de cadenas de error SQLite en UserRepository | BAJO | PENDIENTE |
| [#31](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/31) | [SMELL-05] Precompilar expresión regular de Código Postal con source generator | BAJO | PENDIENTE |
| [#32](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/32) | [GAP-06] Documentar procedimiento de Bootstraping del Administrador inicial | BAJO | PENDIENTE |
| [#33](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/33) | [GAP-08] Desacoplar precondiciones de Pausar y Reanudar en CU-14 | BAJO | PENDIENTE |

## Detalle y trazabilidad

### #14 — [BUG-CRIT-01] Flexibilizar la política de SDK en global.json

- **Riesgo:** ALTO.
- **Estado:** COMPLETADA.
- **Archivos afectados:** `4-Codigo/global.json`.
- **Corrección aplicada:** Se configuró global.json con SDK mínimo 10.0.100 y rollForward minor.
- **Pruebas ejecutadas:** `dotnet --info`, restore, build Release sin advertencias; 102 pruebas unitarias y 317 de integración con SDK 10.0.400. No se dispone de otra banda de SDK para verificarla.
- **Documentación sincronizada:** README raíz, backend y Bruno.
- **Dependencias:** Desbloquea restauración, compilación y pruebas.
- **Observaciones:** La API sigue exigiendo runtime ASP.NET Core 10.0.14 o posterior, independiente del SDK; se probó con una instalación temporal de 10.0.14.

### #15 — [BUG-CRIT-02] Captura amigable al arranque por falta de configuración JWT

- **Riesgo:** ALTO.
- **Estado:** COMPLETADA.
- **Archivos afectados:** `4-Codigo/API/Program.cs`. La validación existente en `JwtOptions.cs` se conserva.
- **Corrección aplicada:** Se captura la validación JWT al inicio con instrucciones seguras y salida no exitosa en consola.
- **Pruebas ejecutadas:** 18 casos de `JwtConfigurationTests`; proceso directo con clave ausente, vacía, corta y solo espacios (salida 1, instrucciones, sin filtración); arranque válido en Development con OpenAPI y Scalar HTTP 200.
- **Documentación sincronizada:** README backend.
- **Dependencias:** Requiere #14 para ejecutar; no cambia la política criptográfica.
- **Observaciones:** La validación criptográfica, el mínimo UTF-8 y la clave revocada permanecen vigentes. Las pruebas de integración usan bases SQLite temporales.

### #16 — [BUG-CRIT-03] Implementar Segregación de Funciones (SoD) en Auditoría de Cursos

- **Riesgo:** ALTO.
- **Estado:** COMPLETADA.
- **Archivos afectados:** `4-Codigo/BusinessLogic/Services/CourseService.cs`, `4-Codigo/API/Controllers/CoursesController.cs`, `4-Codigo/Tests/Lumen.API.IntegrationTests/CourseReviewHistoryIntegrationTests.cs`, `4-Codigo/bruno/Courses/17–22`, CU-03/CU-04/CU-05 y compendio.
- **Corrección aplicada:** BusinessLogic impide resolver la revisión propia con 403 antes de modificar estado o publicación.
- **Pruebas ejecutadas:** 8 casos de `CourseReviewHistoryIntegrationTests` y suite completa de 317 integraciones; Bruno 7 requests y 8 aserciones aprobadas sobre SQLite temporal.
- **Documentación sincronizada:** CU-03, CU-04, CU-05; compendio; Informe; README; Bruno.
- **Dependencias:** Requiere #14 para probar; #18 podrá extraer el servicio sin perder RN-17.
- **Observaciones:** La semilla demo incluye un solo Administrador; el recorrido Bruno crea un segundo usuario temporal. No se guardan credenciales nuevas en el repositorio.

### #17 — [GAP-04] Actualizar Sección 5.2 del Informe General con NFRs de Backend

- **Riesgo:** ALTO.
- **Estado:** COMPLETADA.
- **Archivos afectados:** `Lumen_Informe_General.md/.docx`, `Lumen_Actores_CasosDeUso.md/.docx`, CU-02/CU-03/CU-04/CU-05 y README técnicos.
- **Corrección aplicada:** Se incorporaron RNF-15 a RNF-18 y RN-17 en Informe y casos de uso, en Word y Markdown.
- **Pruebas ejecutadas:** Comprobación de RNF-15 a RNF-18 y RN-17 contra código y pruebas; comparación Markdown/Word; exportación de ambos Word a PDF e inspección visual de sus 10 y 23 páginas.
- **Documentación sincronizada:** Informe y compendio en Word y Markdown; casos individuales afectados; README.
- **Dependencias:** Describe el comportamiento verificado de #14–#16 y mecanismos previos.
- **Observaciones:** Se preservaron RNF-01 a RNF-14. El límite de login es local por instancia. El renderizador de la skill no encontró LibreOffice; se utilizó Word instalado para la revisión visual.

### #18 — [SMELL-01] Descomponer la God Class CourseService respetando SRP

- **Riesgo:** MEDIO.
- **Estado:** COMPLETADA.
- **Archivos afectados:** `CourseService.cs`, `CourseValidator.cs`, `CourseRules.cs`, `CourseReviewService.cs`, `ICourseReviewService.cs`, `ICourseService.cs`, `DependencyInjection.cs`, `CoursesController.cs`, pruebas unitarias.
- **Corrección aplicada:** La auditoría pasó a un servicio inyectado; la normalización y validación de cursos a `CourseValidator`; reglas compartidas de acceso y reloj a `CourseRules`.
- **Pruebas ejecutadas:** Build Release; CourseServiceTests, CourseUpdateServiceTests, CourseReviewHistoryIntegrationTests y suite .NET.
- **Documentación sincronizada:** README backend; matriz de trazabilidad.
- **Dependencias:** Depende de #25 para conservar la serialización al extraer auditoría.
- **Observaciones:** Se mantuvieron las rutas y DTO HTTP; la recuperación del borrador sigue en CourseService.

### #19 — [SMELL-02] Eliminar duplicación de código (DRY) entre AuthService y UserService

- **Riesgo:** MEDIO.
- **Estado:** COMPLETADA.
- **Archivos afectados:** `UserFactory.cs`, `AuthService.cs`, `UserService.cs`.
- **Corrección aplicada:** La factoría compartida normaliza, valida y hashea cuentas; los servicios conservan permisos y persistencia.
- **Pruebas ejecutadas:** AuthServiceTests, AccountValidationIntegrationTests y suite .NET.
- **Documentación sincronizada:** CU-01 y consolidado; matriz.
- **Dependencias:** No requiere cambios de esquema ni de cuentas existentes.
- **Observaciones:** El registro público mantiene Alumno/Profesor; el alta interna exige Administrador.

### #20 — [SMELL-03] Reubicar endpoint de progreso de alumnos en CoursesController

- **Riesgo:** MEDIO.
- **Estado:** COMPLETADA.
- **Archivos afectados:** `CoursesController.cs`, `EnrollmentsController.cs`.
- **Corrección aplicada:** La acción de progreso por curso pasó al controlador de cursos sin cambiar ruta ni contrato.
- **Pruebas ejecutadas:** ProgressIntegrationTests, PagingIntegrationTests, OpenApiContractTests y suite .NET.
- **Documentación sincronizada:** README backend; matriz.
- **Dependencias:** Usa el mismo IProgressService y la misma paginación.
- **Observaciones:** No se duplicó el endpoint; Bruno conserva la ruta.

### #21 — [GAP-01] Reparar referencias funcionales rotas en cabeceras de CU-01 y CU-10

- **Riesgo:** MEDIO.
- **Estado:** COMPLETADA.
- **Archivos afectados:** CU-01 y CU-10 individuales; consolidado Markdown y Word.
- **Corrección aplicada:** Se incorporaron RF-01 a RF-04, RN-01/02, VAL-002 y RF-33 a RF-35, RN-16, VAL-003 en las referencias funcionales.
- **Pruebas ejecutadas:** Cotejo con Informe General y Word/Markdown.
- **Documentación sincronizada:** Casos individuales, consolidado y matriz.
- **Dependencias:** Se resolvió junto con #23 para evitar cabeceras contradictorias.
- **Observaciones:** No cambian los enunciados de RF ni RN.

### #22 — [GAP-02] Incorporar Requerimiento Funcional de Completitud de Módulos (RF-27b)

- **Riesgo:** MEDIO.
- **Estado:** COMPLETADA.
- **Archivos afectados:** Informe General Markdown y Word; CU-16 individual; consolidado Markdown y Word.
- **Corrección aplicada:** Se agregó RF-27b en 5.1.3 y se vinculó con CU-16; RF-27 permanece intacto.
- **Pruebas ejecutadas:** Cotejo documental y ProgressIntegrationTests.
- **Documentación sincronizada:** Informe, CU-16, consolidado y matriz.
- **Dependencias:** Documenta la finalización idempotente ya implementada.
- **Observaciones:** RF-27b se identifica expresamente como nuevo requerimiento de la issue docente.

### #23 — [GAP-05] Corregir Actor Principal en CU-01 Registrarse asignando Visitante

- **Riesgo:** MEDIO.
- **Estado:** COMPLETADA.
- **Archivos afectados:** CU-01 individual; consolidado Markdown y Word.
- **Corrección aplicada:** Se definió Visitante sin sesión como actor principal, con elección de Alumno/Profesor y postcondición del registro.
- **Pruebas ejecutadas:** Cotejo documental y AccountValidationIntegrationTests.
- **Documentación sincronizada:** CU-01, actores del consolidado y matriz.
- **Dependencias:** Alineada con #21 y RF-01 a RF-04.
- **Observaciones:** Los permisos de la API no cambian.

### #24 — [GAP-07] Desacoplar documentalmente las metas divergentes de CU-12

- **Riesgo:** MEDIO.
- **Estado:** COMPLETADA.
- **Archivos afectados:** CU-12 individual, CU-06/08/13/16, consolidado Markdown y Word.
- **Corrección aplicada:** CU-12 conserva su ID histórico y separa las metas en CU-12A (estado) y CU-12B (contenido aprobado).
- **Pruebas ejecutadas:** Cotejo de rutas, permisos, respuestas, PagingIntegrationTests y EnrollmentCreationIntegrationTests.
- **Documentación sincronizada:** Casos individuales, consolidado y matriz.
- **Dependencias:** CU-12A usa RF-26/30; CU-12B usa RF-24 y RN-11.
- **Observaciones:** No se crean endpoints nuevos ni se renumeran los 16 casos originales.

### #25 — [SMELL-06] Desacoplar serialización de Snapshots del DTO de presentación

- **Riesgo:** MEDIO.
- **Estado:** COMPLETADA.
- **Archivos afectados:** `CourseReviewSnapshotCodec.cs`, `CourseReviewService.cs`, `CourseReviewHistoryIntegrationTests.cs`, modelo de datos.
- **Corrección aplicada:** Los envíos nuevos guardan `CourseReviewSnapshotPayload` con `SchemaVersion: 1`; la lectura reconoce JSON histórico sin versión y filas sin snapshot.
- **Pruebas ejecutadas:** Pruebas de snapshot nuevo e histórico, recuperación, permisos e inmutabilidad SQLite; suite .NET.
- **Documentación sincronizada:** Modelo de datos, README backend y matriz.
- **Dependencias:** Implementada antes de la extracción de #18.
- **Observaciones:** Sin migración: se conserva `ContentJson`, no se reescriben instantáneas antiguas.

### #26 — [BUG-MEN-01] Validar ModelState.IsValid en ReportsController

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/API/Controllers/ReportsController.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo MEDIO.

### #27 — [BUG-MEN-02] Incorporar cota superior de negocio al Cupo Máximo (MaxCapacity)

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/BusinessLogic/Services/CourseService.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo MEDIO.

### #28 — [BUG-MEN-03] Homogeneizar la semántica del campo ToUtc en el DTO de Reportes

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/BusinessLogic/Services/ReportService.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo MEDIO.

### #29 — [BUG-MEN-04] Reemplazar incremento artificial de ticks por token de versión numérico

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/BusinessLogic/Services/CourseService.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo MEDIO.

### #30 — [BUG-MEN-05] Sanear búsqueda de cadenas de error SQLite en UserRepository

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/DataAccess/Repositories/UserRepository.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo MEDIO.

### #31 — [SMELL-05] Precompilar expresión regular de Código Postal con source generator

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/BusinessLogic/Services/ServiceGuard.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo MEDIO.

### #32 — [GAP-06] Documentar procedimiento de Bootstraping del Administrador inicial

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `1-Entrega Personal/1 - Informe/Lumen_Informe_General.md`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo MEDIO.

### #33 — [GAP-08] Desacoplar precondiciones de Pausar y Reanudar en CU-14

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-14-pausar-o-reanudar-curso.md`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo MEDIO.

## Cierre de la Ronda 2 (16/11/2026)

Estado local: **4 ALTO y 8 MEDIO COMPLETADAS; 8 BAJO PENDIENTES**. Las ocho issues de GitHub siguen abiertas; esta rama no hizo commit, push ni merge. La línea base pasó 102 pruebas unitarias y 317 de integración; la verificación final pasó 102 unitarias y 318 de integración tras agregar la regresión del endpoint de progreso, con build Release sin advertencias. El frontend pasó 12 pruebas, lint y build sin cambios de código. En SQLite temporal se aplicaron seis migraciones, `PRAGMA integrity_check` devolvió `ok` y dos snapshots nuevos registraron `SchemaVersion: 1`. Las pruebas de integración leyeron formatos con y sin versión, recuperaron borradores y rechazaron modificaciones de snapshots. Bruno aprobó 7 requests/8 tests de auditoría con dos administradores y 19 requests/54 tests de exposición y progreso. OpenAPI y Scalar respondieron HTTP 200; la operación de progreso conserva su ruta.

Los dos Word se exportaron a PDF e inspeccionaron visualmente (11 y 23 páginas). El cotejo bidireccional con Markdown no encontró diferencias entre los 191 párrafos del Informe y los 638 del consolidado. El nuevo requerimiento RF-27b no renumera RF-27 ni cambia la API. El payload versionado reutiliza `ContentJson` sin migración ni reescritura de historial.
