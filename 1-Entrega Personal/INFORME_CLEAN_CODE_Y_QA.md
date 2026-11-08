# Informe de Análisis Estático, Clean Code y QA
**Proyecto:** LUMEN — Sistema de Gestión de Cursos Online (LMS)  
**Materia:** Práctica Profesionalizante I — Tecnicatura Superior en Desarrollo de Software (2026)  
**Alumnos:** Rodrigo Gabriel Astore, Nahuel Ferrino  
**Docentes:** Gabriel Majluf, Oscar Spila  
**Fecha de Evaluación:** Noviembre de 2026  
**Componente Evaluado:** `4-Codigo` (Backend .NET 10, Entity Framework Core, SQLite, Suite de Pruebas)  
**Rol:** Ingeniero de Software Senior especializado en Clean Code y QA  

---

## 1. Resumen Ejecutivo de Calidad de Código

Se ha completado el análisis estático exhaustivo del código fuente del backend. La solución destaca por su sólida arquitectura en capas (**N-Tier**), la implementación de invariantes relacionales mediante disparadores nativos en SQLite, el control estricto de concurrencia optimista y una cobertura sobresaliente de pruebas automatizadas (+416 tests unitarios y de integración).

Sin embargo, desde la perspectiva de **Clean Code, Principios SOLID y Aseguramiento de la Calidad (QA)**, se han detectado oportunidades de mejora, desvíos arquitectónicos y casos borde que deben ser atendidos para alcanzar un estándar industrial y garantizar la máxima mantenibilidad.

### Matriz Resumen de Hallazgos

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                       DISTRIBUCIÓN DE HALLAZGOS                             │
├─────────────────────────┬──────────────┬────────────────────────────────────┤
│ Categoría               │ Cantidad     │ Severidad / Impacto                │
├─────────────────────────┼──────────────┼────────────────────────────────────┤
│ 1. Bugs Críticos        │ 3            │ Alto (Portabilidad, SoD, Startup)  │
│ 2. Bugs Menores         │ 5            │ Medio-Bajo (Validaciones, Asimetría)│
│ 3. Code Smells          │ 7            │ Medio (SOLID, DRY, Primitive Obs.) │
├─────────────────────────┼──────────────┼────────────────────────────────────┤
│ TOTAL DE OBSERVACIONES  │ 15           │ Dictamen: Aprobado con Observaciones│
└─────────────────────────┴──────────────┴────────────────────────────────────┘
```

---

## 2. Sección I: Bugs Críticos y Defectos de Seguridad

### BUG-CRIT-01: Bloqueo de compilación por rigidez de versión de SDK en `global.json`
- **Ubicación:** `4-Codigo/global.json` (Líneas 1-6)
- **Código observado:**
  ```json
  {
    "sdk": {
      "rollForward": "latestPatch",
      "version": "10.0.403"
    }
  }
  ```
- **Justificación técnica:** La política `rollForward: "latestPatch"` limita la ejecución estrictamente a la banda `10.0.4xx`. En máquinas de desarrollo, servidores de CI o entornos de corrección docente con SDKs de .NET 10 de bandas previas (por ejemplo `10.0.103`), el CLI de .NET aborta inmediatamente antes del restore o compilación con `ERROR al compilar. 0 Advertencias, 0 Errores`.
- **Propuesta de corrección:**
  ```json
  {
    "sdk": {
      "rollForward": "minor",
      "version": "10.0.100"
    }
  }
  ```

---

### BUG-CRIT-02: Destrucción del Host al arranque en entornos sin User Secrets configurados
- **Ubicación:** `4-Codigo/API/Program.cs` (Línea 88) y `4-Codigo/API/Security/JwtOptions.cs` (Líneas 23-41)
- **Código observado:**
  ```csharp
  // Program.cs
  _ = app.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
  ```
- **Justificación técnica:** El método `ValidateOnStart()` ejecuta `JwtOptionsValidator`. Si un desarrollador clona el repositorio e intenta ejecutar `dotnet run` sin haber configurado previamente `dotnet user-secrets` o la variable de entorno `Jwt__Key`, la aplicación arroja `OptionsValidationException` sin un mensaje amigable en consola y crashea el proceso.
- **Propuesta de corrección:** Capturar la excepción en `Program.cs` y emitir una guía operativa clara por consola antes de finalizar con un código de salida controlado, o proveer un script de inicialización local automatizado (`init-dev.ps1` / `init-dev.sh`).

---

### BUG-CRIT-03: Falla de Segregación de Funciones (SoD) en Auditoría de Cursos
- **Ubicación:** `4-Codigo/BusinessLogic/Services/CourseService.cs` (Método `ResolveReviewAsync`, Líneas 208-256)
- **Justificación técnica:** Un usuario con rol `Administrador` posee permisos para crear un curso (`CreateAsync`), enviarlo a revisión (`SubmitForReviewAsync`) y, posteriormente, **aprobarse a sí mismo la solicitud de auditoría** en `ResolveReviewAsync`. El sistema no valida `if (review.RequestedByUserId == currentUser.UserId) throw new ForbiddenOperationException(...)`.
- **Impacto:** Vulnerabilidad de control de negocio que rompe el principio de cuatro ojos (*Four-Eyes Principle*) en la moderación y publicación de contenidos.
- **Propuesta de corrección:**
  ```csharp
  if (review.RequestedByUserId == currentUser.UserId)
  {
      throw new ForbiddenOperationException("Un administrador no puede auditar ni resolver solicitudes de cursos creados por él mismo.");
  }
  ```

---

## 3. Sección II: Bugs Menores y Validaciones Faltantes

### BUG-MEN-01: Mensaje de validación equívoco ante fechas mal formadas en Reportes
- **Ubicación:** `4-Codigo/API/Controllers/ReportsController.cs` (Método `GetSummary`, Líneas 21-29)
- **Justificación técnica:** Cuando se envían parámetros inválidos (ej. `?from=abc&to=2026-11-01`), el binder de ASP.NET Core no puede parsear la fecha y deja la variable `from` en `null`. Al no comprobar `ModelState.IsValid`, el controlador ingresa en `if (!from.HasValue)` y arroja *"Falta el parámetro obligatorio: from."*, induciendo a error al cliente al sugerir ausencia en lugar de error de formato.
- **Propuesta de corrección:** Validar `ModelState.IsValid` antes de comprobar la nulidad de las variables.

---

### BUG-MEN-02: Falta de cota superior de dominio para el Cupo Máximo (`MaxCapacity`)
- **Ubicación:** `4-Codigo/BusinessLogic/Services/CourseService.cs` (Método `BuildRevision`, Línea 313)
- **Justificación técnica:** La validación actual solo comprueba `dto.MaxCapacity <= 0`. No existe un límite superior razonable (ej. máximo 5.000 alumnos). Un usuario puede cargar un cupo de `2.000.000.000`, lo cual es inverosímil para un LMS de estas características y distorsiona métricas y vistas.
- **Propuesta de corrección:** Imponer `if (dto.MaxCapacity is <= 0 or > 10_000) throw new LumenValidationException("El cupo debe estar entre 1 y 10.000 alumnos.");`.

---

### BUG-MEN-03: Asimetría en el campo `ToUtc` de la respuesta de Reportes
- **Ubicación:** `4-Codigo/BusinessLogic/Services/ReportService.cs` (Líneas 31-32 vs. 39)
- **Justificación técnica:** Al solicitar reportes con `timeZone`, la respuesta `ReportSummaryResponseDTO` devuelve en `ToUtc` el inicio del último día (`lastDayStart` a las `00:00:00 UTC`), mientras que sin `timeZone` devuelve el `toUtc` exacto que envió el usuario. Esto genera ambigüedad en el contrato de la API sobre qué instante exacto representa dicho límite.
- **Propuesta de corrección:** Homogeneizar el contrato documentando explícitamente si el campo representa el inicio o el final del rango civil evaluado.

---

### BUG-MEN-04: Desplazamiento artificial de fechas hacia el futuro (`NextTimestamp`)
- **Ubicación:** `4-Codigo/BusinessLogic/Services/CourseService.cs` (Método `NextTimestamp`, Líneas 401-410)
- **Justificación técnica:** Si dos escrituras ocurren dentro del mismo tick del reloj del sistema, `NextTimestamp` suma artificialmente un tick (`previous.AddTicks(1)`). Si se acumulan actualizaciones sucesivas, `UpdatedAtUtc` queda fechado en el futuro respecto a la hora UTC real, un parche de concurrencia que puede provocar inconsistencias con ordenamientos basados en `DateTime.UtcNow`.
- **Propuesta de corrección:** Emplear una columna dedicada de versión numérica (`RowVersion` / `int Version`) como token de concurrencia en lugar de manipular el reloj del sistema.

---

### BUG-MEN-05: Coincidencia frágil de cadenas en la captura de excepciones SQLite
- **Ubicación:** `4-Codigo/DataAccess/Repositories/UserRepository.cs` (Línea 21)
- **Justificación técnica:** Se utiliza `sqlite.Message.Contains("UNIQUE constraint failed: Users.NormalizedEmail'", StringComparison.Ordinal)`. La presencia de la comilla simple final (`'`) al cierre de `NormalizedEmail` acopla la lógica a la puntuación exacta de una versión particular del driver de SQLite, mientras que en otros repositorios se busca sin comillas adyacentes.

---

## 4. Sección III: Code Smells y Deuda Técnica

### SMELL-01: God Class / Violación del Principio de Responsabilidad Única (SRP) en `CourseService`
- **Ubicación:** `4-Codigo/BusinessLogic/Services/CourseService.cs` (533 líneas)
- **Problema:** La clase concentra responsabilidades divergentes:
  1. Catálogo público de cursos.
  2. CRUD y mantenimiento de borradores.
  3. Gestión de auditorías y decisiones (`CourseReview`).
  4. Serialización y deserialización de snapshots.
  5. Ciclo de vida operacional (`Pausar`, `Reanudar`, `Finalizar`).
  6. Normalización, cálculo de duración y validación profunda de entradas.
- **Refactorización recomendada:** Descomponer en:
  - `CourseService`: Gestión del curso y catálogo.
  - `CourseReviewService`: Auditoría, envíos y decisiones administrativas.
  - `CourseValidator`: Métodos puros de validación y normalización de textos.

---

### SMELL-02: Violación flagrante de DRY (Don't Repeat Yourself) en creación de usuarios
- **Ubicación:** `4-Codigo/BusinessLogic/Services/AuthService.cs` (Líneas 34-50) y `4-Codigo/BusinessLogic/Services/UserService.cs` (Líneas 41-57)
- **Problema:** Ambos servicios duplican de forma idéntica 15 líneas de código para instanciar la entidad `User`, normalizar campos, hashear la contraseña y llamar al repositorio.
- **Refactorización recomendada:** Centralizar la creación en una factoría o método de extensión de dominio `User.Create(...)`.

---

### SMELL-03: Violación de Cohesión y Enrutamiento REST en `EnrollmentsController`
- **Ubicación:** `4-Codigo/API/Controllers/EnrollmentsController.cs` (Línea 98)
- **Problema:** El endpoint `GET /api/courses/{courseId:guid}/students/progress` está ubicado en `EnrollmentsController` a pesar de que su raíz canónica de recurso es `/courses`.
- **Refactorización recomendada:** Mover la acción a `CoursesController` inyectando `IProgressService`.

---

### SMELL-04: Antipatrón de Obsesión por Tipos Primitivos (Primitive Obsession)
- **Ubicación:** `4-Codigo/BusinessLogic/Services/ServiceGuard.cs` (Líneas 63-84) y `4-Codigo/Shared/DTOs/UserDTOs.cs`
- **Problema:** Conceptos de negocio con reglas complejas (`Dni`, `Telefono`, `CodigoPostal`, `Email`) son modelados como `string`. El método `ValidatePersonalData` recibe 7 parámetros de tipo `string?` contiguos (*Long Parameter List*), favoreciendo errores humanos al invocarlo.
- **Refactorización recomendada:** Encapsular en *Value Objects* inmutables (`Dni`, `PhoneNumber`, `PostalCode`, `EmailAddress`).

---

### SMELL-05: Expresión Regular no compilada en validación de código postal
- **Ubicación:** `4-Codigo/BusinessLogic/Services/ServiceGuard.cs` (Línea 80)
- **Problema:** Se invoca `Regex.IsMatch(...)` estático sin timeout y sin compilar en cada registro de usuario.
- **Refactorización recomendada:** Aprovechar el generador de código fuente de .NET 10:
  ```csharp
  [GeneratedRegex(@"^(?:[0-9]{4}|[A-Za-z][0-9]{4}[A-Za-z]{3})$", RegexOptions.None, matchTimeoutMilliseconds: 250)]
  private static partial Regex PostalCodeRegex();
  ```

---

### SMELL-06: Serialización de Snapshots acoplada a DTOs de presentación sin versionado
- **Ubicación:** `4-Codigo/BusinessLogic/Services/CourseService.cs` (Líneas 176 y 205)
- **Problema:** Al enviar a revisión, se guarda en SQLite el JSON de `CourseResponseDTO`. Si dicho DTO sufre cambios en el futuro (propiedades renombradas o nuevos campos obligatorios), la deserialización de revisiones históricas congeladas fallará.
- **Refactorización recomendada:** Modelar un esquema de snapshot propio e inmutable (`CourseReviewSnapshotPayload`) con número de versión de esquema.

---

### SMELL-07: Múltiples viajes de ida y vuelta (Roundtrips) síncronos en una sola transacción
- **Ubicación:** `4-Codigo/DataAccess/Repositories/CourseRepository.cs` (Método `CreateAsync`, Líneas 24-29)
- **Problema:** Se invoca `SaveAsync` tres veces consecutivas dentro de la misma transacción para sortear la validación del disparador relacional de SQLite, generando 3 escrituras separadas a disco.
- **Refactorización recomendada:** Reorganizar la inserción para persistir la entidad y su revisión en un único lote (*batch*) compatible con las invariantes de clave foránea.

---

## 5. Guía de Refactorización y Buenas Prácticas para los Alumnos

Para los alumnos **Rodrigo Astore y Nahuel Ferrino**, se sugieren las siguientes acciones de mejora prioritarias:

1. **Flexibilizar `global.json`:** Cambiar `"rollForward": "minor"` para que cualquier docente o evaluador pueda compilar el proyecto sin exigir la instalación de un parche de SDK específico.
2. **Prevenir la Auto-Aprobación de Cursos:** Agregar la validación en `CourseService.ResolveReviewAsync` para impedir que un Administrador apruebe auditorías de cursos que él mismo creó.
3. **Descomponer `CourseService`:** Extraer la lógica de auditorías (`SubmitForReview`, `ResolveReview`, `GetPendingReviews`) a una nueva clase `CourseReviewService`.
4. **Unificar la Creación de Usuarios:** Reutilizar el bloque de instanciación y hashing entre `AuthService` y `UserService`.

---

## 6. Dictamen Final

El proyecto **LUMEN (Backend .NET 10)** demuestra un **nivel de ingeniería de software sobresaliente**, con una arquitectura sólida y mecanismos de defensa en profundidad poco habituales en instancias académicas. Atendiendo los puntos de segregación de funciones (SoD) y modularización de servicios, el código alcanzará un estándar plenamente profesional.

