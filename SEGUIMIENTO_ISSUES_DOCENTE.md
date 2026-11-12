# Seguimiento de issues docentes de LUMEN

Registro maestro de las 20 issues docentes. Ronda 1: #14–#17 (riesgo ALTO). Las otras 16 quedan pendientes para las rondas posteriores. Estado local; las issues de GitHub no se modifican.

La [matriz de trazabilidad](AUDITORIA_Y_PLAN_DE_CORRECCIONES.md#matriz-de-trazabilidad) vincula las cuatro correcciones de esta ronda con requerimientos, casos de uso, código, pruebas, Bruno/OpenAPI y documentos.

| Issue | Título | Riesgo | Estado |
|---|---|---|---|
| [#14](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/14) | [BUG-CRIT-01] Flexibilizar la política de SDK en global.json | ALTO | COMPLETADA |
| [#15](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/15) | [BUG-CRIT-02] Captura amigable al arranque por falta de configuración JWT | ALTO | COMPLETADA |
| [#16](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/16) | [BUG-CRIT-03] Implementar Segregación de Funciones (SoD) en Auditoría de Cursos | ALTO | COMPLETADA |
| [#17](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/17) | [GAP-04] Actualizar Sección 5.2 del Informe General con NFRs de Backend | ALTO | COMPLETADA |
| [#18](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/18) | [SMELL-01] Descomponer la God Class CourseService respetando SRP | MEDIO | PENDIENTE |
| [#19](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/19) | [SMELL-02] Eliminar duplicación de código (DRY) entre AuthService y UserService | MEDIO | PENDIENTE |
| [#20](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/20) | [SMELL-03] Reubicar endpoint de progreso de alumnos en CoursesController | MEDIO | PENDIENTE |
| [#21](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/21) | [GAP-01] Reparar referencias funcionales rotas en cabeceras de CU-01 y CU-10 | MEDIO | PENDIENTE |
| [#22](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/22) | [GAP-02] Incorporar Requerimiento Funcional de Completitud de Módulos (RF-27b) | MEDIO | PENDIENTE |
| [#23](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/23) | [GAP-05] Corregir Actor Principal en CU-01 Registrarse asignando Visitante | MEDIO | PENDIENTE |
| [#24](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/24) | [GAP-07] Desacoplar documentalmente las metas divergentes de CU-12 | MEDIO | PENDIENTE |
| [#25](https://github.com/PracticasProfesionalizantes-I-2026/Grupo-N-10-BackendCursosOnline/issues/25) | [SMELL-06] Desacoplar serialización de Snapshots del DTO de presentación | MEDIO | PENDIENTE |
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
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/BusinessLogic/Services/CourseService.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #19 — [SMELL-02] Eliminar duplicación de código (DRY) entre AuthService y UserService

- **Riesgo:** MEDIO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/BusinessLogic/Services/AuthService.cs`, `4-Codigo/BusinessLogic/Services/UserService.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #20 — [SMELL-03] Reubicar endpoint de progreso de alumnos en CoursesController

- **Riesgo:** MEDIO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/API/Controllers/EnrollmentsController.cs`, `CoursesController.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #21 — [GAP-01] Reparar referencias funcionales rotas en cabeceras de CU-01 y CU-10

- **Riesgo:** MEDIO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-01-registrarse.md`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #22 — [GAP-02] Incorporar Requerimiento Funcional de Completitud de Módulos (RF-27b)

- **Riesgo:** MEDIO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `1-Entrega Personal/1 - Informe/Lumen_Informe_General.md`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #23 — [GAP-05] Corregir Actor Principal en CU-01 Registrarse asignando Visitante

- **Riesgo:** MEDIO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/Lumen_Actores_CasosDeUso.md`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #24 — [GAP-07] Desacoplar documentalmente las metas divergentes de CU-12

- **Riesgo:** MEDIO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-12-consultar-inscripciones-y-acceder-a-cursos.md`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #25 — [SMELL-06] Desacoplar serialización de Snapshots del DTO de presentación

- **Riesgo:** MEDIO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/BusinessLogic/Services/CourseService.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #26 — [BUG-MEN-01] Validar ModelState.IsValid en ReportsController

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/API/Controllers/ReportsController.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #27 — [BUG-MEN-02] Incorporar cota superior de negocio al Cupo Máximo (MaxCapacity)

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/BusinessLogic/Services/CourseService.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #28 — [BUG-MEN-03] Homogeneizar la semántica del campo ToUtc en el DTO de Reportes

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/BusinessLogic/Services/ReportService.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #29 — [BUG-MEN-04] Reemplazar incremento artificial de ticks por token de versión numérico

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/BusinessLogic/Services/CourseService.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #30 — [BUG-MEN-05] Sanear búsqueda de cadenas de error SQLite en UserRepository

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/DataAccess/Repositories/UserRepository.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #31 — [SMELL-05] Precompilar expresión regular de Código Postal con source generator

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `4-Codigo/BusinessLogic/Services/ServiceGuard.cs`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #32 — [GAP-06] Documentar procedimiento de Bootstraping del Administrador inicial

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `1-Entrega Personal/1 - Informe/Lumen_Informe_General.md`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

### #33 — [GAP-08] Desacoplar precondiciones de Pausar y Reanudar en CU-14

- **Riesgo:** BAJO.
- **Estado:** PENDIENTE.
- **Archivos afectados:** `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-14-pausar-o-reanudar-curso.md`.
- **Corrección aplicada:** Pendiente de la ronda correspondiente.
- **Pruebas ejecutadas:** No ejecutadas en esta ronda.
- **Documentación sincronizada:** Pendiente.
- **Dependencias:** Evaluar en su ronda.
- **Observaciones:** No se modifica en la ronda de riesgo ALTO.

