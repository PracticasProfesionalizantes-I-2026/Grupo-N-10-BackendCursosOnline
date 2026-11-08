# Devolución Académica y Plan de Mejoras para el Alumno
**Proyecto:** LUMEN — Sistema de Gestión de Cursos Online (LMS)  
**Materia:** Práctica Profesionalizante I — Tecnicatura Superior en Desarrollo de Software (2026)  
**Alumnos:** Rodrigo Gabriel Astore, Nahuel Ferrino  
**Docentes Evaluadores:** Prof. Gabriel Majluf, Prof. Oscar Spila  
**Fecha de Devolución:** Noviembre de 2026  
**Documentos de Respaldo:** [`INFORME_AUDITORIA_FUNCIONAL_Y_ARQUITECTURA.md`](INFORME_AUDITORIA_FUNCIONAL_Y_ARQUITECTURA.md), [`INFORME_CLEAN_CODE_Y_QA.md`](INFORME_CLEAN_CODE_Y_QA.md) y [`GITHUB_ISSUE_AUDITORIA_FINAL.md`](GITHUB_ISSUE_AUDITORIA_FINAL.md)

---

## 1. Devolución General y Felicitaciones al Equipo

Estimados Rodrigo y Nahuel:

Luego de una revisión exhaustiva de la documentación de análisis, las especificaciones de Casos de Uso, la solución técnica desarrollada en .NET 10 y un análisis estático de código fuente con enfoque de QA, queremos felicitar al equipo por el **altísimo nivel de ingeniería de software demostrado en la implementación del backend**.

El proyecto LUMEN supera con holgura los estándares habituales de una Práctica Profesionalizante I:
- La arquitectura en capas (**N-Tier**) está estrictamente respetada: la API desacopla el transporte HTTP, la lógica de negocio no contiene dependencias de persistencia directa, y el acceso a datos utiliza repositorios tipados.
- La gestión de estados complejos mediante el **doble puntero de revisión** (`PublishedRevisionId` vs. `WorkingRevisionId`) resuelve de forma impecable el versionado de cursos en producción.
- La aplicación de **control de concurrencia optimista** mediante encabezados `ETag` e `If-Match` (conforme a RFC 6585) y los códigos HTTP 428 / 412 demuestran madurez profesional.
- La integridad de datos no depende solo de validaciones en memoria: han implementado **defensa en profundidad** mediante triggers nativos en SQLite (inmutabilidad de auditorías y control de cupo) e índices únicos parciales.
- La batería de más de **416 pruebas automatizadas** (unitarias e integración con SQLite efímero) asegura que la solución sea confiable y reproducible.

Sin embargo, el objetivo de esta etapa formativa es asegurar la **consistencia y coherencia del 100% entre lo que se especifica en los documentos de análisis, lo que se construye en el código y las buenas prácticas de diseño de software**.

Para facilitar el seguimiento del equipo en GitHub, hemos consolidado todos los hallazgos en formato de Issue con checkboxes organizados por nivel de riesgo en el archivo [`GITHUB_ISSUE_AUDITORIA_FINAL.md`](GITHUB_ISSUE_AUDITORIA_FINAL.md).

A continuación, les presentamos la **clasificación jerárquica de tareas** para coordinar las correcciones del equipo antes de la defensa final.

---

## 2. Plan de Acción Jerarquizado por Nivel de Riesgo

### 🔴 RIESGO ALTO (Prioridad Inmediata — Bloqueante para la Defensa)
*Deben resolver el 100% de estos puntos para asegurar que la aplicación compile en cualquier entorno, no crashee en despliegues limpios y garantice el control interno.*

1. **[BUG-CRIT-01] Flexibilizar la política de SDK en `global.json`:**
   - *Archivo:* `4-Codigo/global.json`
   - *Acción:* Cambiar `rollForward: "latestPatch"` por `"minor"`. La configuración actual exige la banda `10.0.4xx` y bloquea la compilación con código de error 1 en equipos de corrección o servidores con SDKs como `10.0.103`.
2. **[BUG-CRIT-02] Captura amigable al arranque por falta de configuración JWT:**
   - *Archivos:* `4-Codigo/API/Program.cs` y `4-Codigo/API/Security/JwtOptions.cs`
   - *Acción:* Evitar que la falta de User Secrets o `Jwt__Key` destruya el proceso con una excepción no controlada (`OptionsValidationException`). Capturar la excepción en `Program.cs`, emitir un mensaje de ayuda claro por consola y salir de forma controlada (`Environment.Exit(1)`).
3. **[BUG-CRIT-03] Implementar Segregación de Funciones (SoD) en Auditoría:**
   - *Archivo:* `4-Codigo/BusinessLogic/Services/CourseService.cs` (`ResolveReviewAsync`)
   - *Acción:* Agregar validación para impedir que un usuario Administrador audite y apruebe cursos creados por él mismo (`if (review.RequestedByUserId == currentUser.UserId) throw new ForbiddenOperationException(...)`), asegurando el principio de cuatro ojos.
4. **[GAP-04] Actualizar la Sección 5.2 del Informe General con los NFRs reales del Backend:**
   - *Archivo:* `1-Entrega Personal/1 - Informe/Lumen_Informe_General.md` (y su versión Word)
   - *Acción:* Incorporar los **RNF-15 a RNF-18** reflejando la arquitectura real construida: Concurrencia con ETag (RFC 6585), Inmutabilidad de auditoría por triggers en SQLite, Rate Limiting (20 req / 60s) y Seguridad Criptográfica JWT.

---

### 🟡 RIESGO MEDIO (Prioridad Media — Calidad y Mantenibilidad)
*Afecta la arquitectura, modularidad y sincronización entre especificaciones y código.*

1. **[SMELL-01] Descomponer la God Class `CourseService` (533 líneas):**
   - *Acción:* Separar la lógica de auditoría en un nuevo `CourseReviewService` (`ICourseReviewService`) y extraer las validaciones a una clase interna `CourseValidator`, respetando el Principio de Responsabilidad Única (SRP).
2. **[SMELL-02] Eliminar duplicación de código (DRY) en creación de usuarios:**
   - *Archivos:* `4-Codigo/BusinessLogic/Services/AuthService.cs` y `UserService.cs`
   - *Acción:* Extraer la instanciación, normalización y hasheo de la entidad `User` a un método factoría compartido.
3. **[SMELL-03] Reubicar endpoint desubicado en `EnrollmentsController`:**
   - *Acción:* Trasladar `GET /api/courses/{courseId:guid}/students/progress` a `CoursesController` respetando el recurso canónico `/courses`.
4. **[GAP-01] Reparar referencias funcionales rotas en cabeceras de CU-01 y CU-10:**
   - *Archivos:* `cu-01-registrarse.md` y `cu-10-consultar-reportes.md`
   - *Acción:* Reemplazar *"No especificadas en el documento fuente"* por `RF-01 a RF-04` en CU-01 y `RF-33 a RF-35` en CU-10.
5. **[GAP-02] Incorporar el Requerimiento Funcional de Completitud de Módulos (RF-27b):**
   - *Archivos:* `Lumen_Informe_General.md` (Sección 5.1.3) y `cu-16-realizar-modulos-o-actividades-del-curso.md`
   - *Acción:* Enunciar el requerimiento que respalda la interacción de completar módulos en `CU-16`.
6. **[GAP-05] Corregir el Actor Principal en `CU-01 Registrarse`:**
   - *Archivos:* `Lumen_Actores_CasosDeUso.md` y `cu-01-registrarse.md`
   - *Acción:* Definir formalmente al actor **Visitante** y asignarlo como actor de CU-01.
7. **[GAP-07] Desacoplar documentalmente las metas divergentes de `CU-12`:**
   - *Archivo:* `cu-12-consultar-inscripciones-y-acceder-a-cursos.md`
   - *Acción:* Separar la consulta de estado de inscripciones (`CU-12A`) del acceso al contenido educativo (`CU-12B`).
8. **[SMELL-06] Desacoplar la serialización de Snapshots del DTO de presentación:**
   - *Archivo:* `4-Codigo/BusinessLogic/Services/CourseService.cs`
   - *Acción:* Utilizar un modelo inmutable dedicado para persistir el JSON de auditoría en SQLite.

---

### 🟢 RIESGO BAJO (Deuda Técnica Menor y Buenas Prácticas)
*Mejoras incrementales de robustez y consistencia.*

1. **[BUG-MEN-01] Validar `ModelState.IsValid` en `ReportsController`:** Evitar mensajes engañosos de parámetro faltante ante fechas mal formadas.
2. **[BUG-MEN-02] Imponer cota superior de negocio a `MaxCapacity`:** Restringir el cupo máximo a un valor coherente (ej. máx. 10.000 alumnos).
3. **[BUG-MEN-03] Homogeneizar la semántica de `ToUtc` en Reportes:** Documentar o unificar si representa el inicio o fin del intervalo civil evaluado.
4. **[BUG-MEN-04] Reemplazar `NextTimestamp` (`AddTicks(1)`):** Evaluar el paso a un contador entero de versión numérica.
5. **[BUG-MEN-05] Sanear la búsqueda de cadenas de error SQLite en `UserRepository`:** Quitar la comilla simple final `"Users.NormalizedEmail'"`.
6. **[SMELL-05] Precompilar la Regex de Código Postal:** Implementar `[GeneratedRegex]` en `ServiceGuard.cs`.
7. **[GAP-06] Documentar el *Bootstraping* del Administrador inicial:** Añadir una nota en RF-06 aclarando el aprovisionamiento de la primera cuenta fuera de banda.
8. **[GAP-08] Desacoplar las precondiciones de Pausar y Reanudar en `CU-14`:** Separar formalmente en dos subflujos autónomos.

---

## 3. Checklist de Verificación Final para los Alumnos

Antes de dar por cerrada la carpeta de entrega, verifiquen el cumplimiento de la siguiente lista:

- [ ] **Sincronización Markdown / Word:** Asegurarse de que cualquier corrección de texto realizada en los archivos `.md` de `1-Entrega Personal` se replique en los documentos `.docx` oficiales (`Lumen_Informe_General.docx` y `Lumen_Actores_CasosDeUso.docx`).
- [ ] **Compilación limpia de la solución:** Ejecutar desde `4-Codigo`:
  ```powershell
  dotnet build Lumen.slnx -c Release
  ```
  *(Debe arrojar 0 errores y 0 advertencias en cualquier SDK de .NET 10)*.
- [ ] **Ejecución total de la suite de pruebas:**
  ```powershell
  dotnet test Lumen.slnx -c Release --no-build
  ```
  *(Deben aprobar las 102 pruebas unitarias y las 314 de integración)*.
- [ ] **Colección Bruno:** Verificar que la carpeta `00-Exposicion` complete exitosamente sus 19 solicitudes encadenadas sobre el puerto temporal.

---

## 4. Conclusión

El trabajo realizado por el equipo es de un calibre técnico sobresaliente. Resolviendo el 100% de los ítems de Riesgo Alto y la mayoría de los de Riesgo Medio, el proyecto LUMEN se consolida como un **proyecto de referencia metodológica y técnica** para la carrera.

¡Muchos éxitos en la etapa final de defensa!

**Prof. Gabriel Majluf — Prof. Oscar Spila**  
*Cátedra de Práctica Profesionalizante I — Ciclo 2026*
