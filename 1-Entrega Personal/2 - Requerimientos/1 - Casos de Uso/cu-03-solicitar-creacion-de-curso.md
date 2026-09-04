# Caso de Uso: Solicitar Creación de Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y adaptada a la
> sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-03 |
| **Nombre** | Solicitar Creación de Curso |
| **Actor Principal** | Profesor |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Profesor → preparar y presentar un curso propio; Administrador → recibir cursos completos para auditoría; Alumnos → acceder únicamente a cursos revisados |
| **Disparador (Trigger)** | El Profesor selecciona “Crear Curso” desde su panel |
| **Prioridad / Frecuencia** | Alta; frecuencia media |
| **Reglas de negocio relacionadas** | RN-07 (datos obligatorios del curso); RN-08 (al menos un módulo para enviar a revisión); RN-09 (duración total igual a la suma de módulos); RN-10 (el envío genera auditoría y estado EN REVISIÓN) |

---

### 1. BREVE DESCRIPCIÓN

Permite que un Profesor prepare un curso, lo guarde como borrador o lo envíe a
revisión administrativa una vez completada la información requerida y sus
módulos.

### 2. PRECONDICIONES

- El Profesor debe haber iniciado sesión con un Token JWT válido.
- La cuenta autenticada debe poseer el rol Profesor.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)

1. El actor envía una petición `POST /api/cursos` con título, descripción,
   categoría, nivel, modalidad, cupo máximo, objetivos, requisitos, módulos y la
   indicación de enviar a revisión.
2. La **Capa de Presentación** valida el JSON y los campos obligatorios definidos
   por **RN-07**.
3. La **Capa de Negocio** verifica que la cuenta autenticada sea Profesor y asigna
   la autoría del curso.
4. La **Capa de Negocio** verifica que exista al menos un módulo completo,
   aplicando **RN-08**.
5. La **Capa de Negocio** calcula la duración total como suma de las duraciones de
   los módulos, aplicando **RN-09**.
6. La **Capa de Persistencia** registra el curso, sus módulos y recursos con estado
   **EN REVISIÓN**.
7. El Sistema genera la solicitud de auditoría asociada, aplicando **RN-10**.
8. El Sistema devuelve **201 Created** con el curso, la duración calculada y la
   confirmación de envío a revisión.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. Usuario no autenticado (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 no se presenta un Token JWT válido, la Capa de Presentación
     detiene la operación.
  2. El Sistema devuelve **401 Unauthorized**. Fin del caso de uso.

* **3a. Rol no autorizado (HTTP 403 Forbidden):**
  1. Si en el Paso 3 la cuenta no posee el rol Profesor, no puede crear cursos.
  2. La Capa de Negocio impide la operación.
  3. El Sistema devuelve **403 Forbidden**. Fin del caso de uso.

* **1b. Guardar curso como borrador (HTTP 201 Created):**
  1. Si en el Paso 1 el Profesor indica “Guardar como borrador”, el Sistema valida
     únicamente los datos mínimos necesarios para conservarlo.
  2. La Capa de Persistencia registra el curso con estado **BORRADOR**.
  3. El Sistema devuelve **201 Created** y no genera una solicitud de auditoría.
     Fin del caso de uso.

* **2a. Información obligatoria incompleta (HTTP 400 Bad Request):**
  1. Si en el Paso 2 falta información requerida por **RN-07**, la Capa de
     Presentación identifica los campos pendientes.
  2. El Sistema devuelve **400 Bad Request** y mantiene el curso sin enviar. Fin
     del caso de uso.

* **4a. Curso sin módulos válidos (HTTP 409 Conflict):**
  1. Si en el Paso 4 el curso no posee al menos un módulo completo, se incumple
     **RN-08**.
  2. La Capa de Negocio rechaza el envío a revisión.
  3. El Sistema devuelve **409 Conflict** y conserva la información como borrador.
     Fin del caso de uso.

* **5a. Duración de módulo inválida (HTTP 400 Bad Request):**
  1. Si en el Paso 5 alguna duración no es válida, no puede aplicarse **RN-09**.
  2. La Capa de Negocio informa el módulo que debe corregirse.
  3. El Sistema devuelve **400 Bad Request**. Fin del caso de uso.

### 5. SUB-VARIACIONES

1. Cada módulo puede incluir distintos tipos de contenido o recursos previstos,
   sin modificar el resultado del envío.
2. El Profesor puede guardar avances como BORRADOR antes de ejecutar el flujo
   principal de envío a revisión.

### 6. POSTCONDICIONES

- El curso queda registrado con sus módulos y recursos.
- La duración total queda calculada según **RN-09**.
- Si se envió correctamente, el curso queda EN REVISIÓN y posee una solicitud de
  auditoría.
- Si se guardó como borrador, el curso queda BORRADOR y no genera auditoría.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | El curso fue creado como BORRADOR o EN REVISIÓN. |
| `400` | Bad Request | Faltan datos obligatorios o alguna duración es inválida. |
| `401` | Unauthorized | No existe una autenticación válida. |
| `403` | Forbidden | La cuenta autenticada no posee rol Profesor. |
| `409` | Conflict | Se intenta enviar un curso que no posee módulos válidos. |

### Matriz de trazabilidad CU-03 → Test

| Paso del CU | Excepción / Código | Test unitario (Negocio) | Test de integración (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Enviar datos del curso | `201 Created` | — (entrada HTTP) | `CreateCourse_WithValidRequest_AcceptsPayload` |
| Paso 2. Validar datos obligatorios | `201 Created` | `CreateCourseAsync_WithCompleteData_ContinuesCreation` | `CreateCourse_WithCompleteData_Returns201Created` |
| Paso 3. Verificar rol y autoría | `201 Created` | `CreateCourseAsync_AsTeacher_AssignsOwnership` | `CreateCourse_AsTeacher_AssignsAuthenticatedOwner` |
| Paso 4. Verificar módulos | `201 Created` | `CreateCourseAsync_WithModule_AllowsReviewSubmission` | `CreateCourse_WithModule_Returns201Created` |
| Paso 5. Calcular duración | `201 Created` | `CreateCourseAsync_SumsModuleDurations` | `CreateCourse_WithModules_ReturnsCalculatedDuration` |
| Paso 6. Persistir curso | `201 Created` | `CreateCourseAsync_WithValidData_PersistsCourseAndModules` | `CreateCourse_WithValidData_PersistsCourse` |
| Paso 7. Generar auditoría | `201 Created` | `CreateCourseAsync_WhenSubmitted_CreatesAuditRequest` | `CreateCourse_WhenSubmitted_CreatesPendingAudit` |
| Paso 8. Responder creación | `201 Created` | `CreateCourseAsync_WhenSubmitted_ReturnsCourseInReview` | `CreateCourse_WhenSubmitted_Returns201Created` |
| 1a. Sin autenticación | `401 Unauthorized` | — (autenticación HTTP) | `CreateCourse_WithoutToken_Returns401Unauthorized` |
| 3a. Rol no autorizado | `403 Forbidden` | `CreateCourseAsync_AsNonTeacher_ThrowsForbiddenException` | `CreateCourse_AsStudent_Returns403Forbidden` |
| 1b. Guardar borrador | `201 Created` | `CreateCourseAsync_AsDraft_DoesNotCreateAudit` | `CreateCourse_AsDraft_Returns201Created` |
| 2a. Datos incompletos | `400 Bad Request` | `CreateCourseAsync_WithMissingRequiredData_ThrowsValidationException` | `CreateCourse_WithMissingRequiredData_Returns400BadRequest` |
| 4a. Sin módulos | `409 Conflict` | `CreateCourseAsync_WithoutModules_ThrowsCourseRuleException` | `CreateCourse_WithoutModules_Returns409Conflict` |
| 5a. Duración inválida | `400 Bad Request` | `CreateCourseAsync_WithInvalidModuleDuration_ThrowsValidationException` | `CreateCourse_WithInvalidModuleDuration_Returns400BadRequest` |

> Los nombres de tests establecen el contrato de trazabilidad del caso de uso y
> deberán coincidir con la suite automatizada cuando se implemente.
