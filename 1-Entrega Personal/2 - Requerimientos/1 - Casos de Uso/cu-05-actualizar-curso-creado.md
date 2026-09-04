# Caso de Uso: Actualizar Curso Creado

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y adaptada a la
> sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-05 |
| **Nombre** | Actualizar Curso Creado |
| **Actor Principal** | Profesor |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Profesor → mantener actualizados sus cursos; Administrador → auditar cada cambio antes de publicarlo; Alumnos → recibir contenido revisado sin perder la versión vigente |
| **Disparador (Trigger)** | El Profesor selecciona “Editar Curso” sobre uno de sus cursos |
| **Prioridad / Frecuencia** | Alta; frecuencia media |
| **Reglas de negocio relacionadas** | RN-07 (datos obligatorios del curso); RN-08 (al menos un módulo); RN-09 (duración igual a la suma de módulos); RN-10 (envío genera auditoría); RN-14 (solo el Profesor propietario puede editar); RN-15 (un curso FINALIZADO no se edita); RN-16 (los cambios no se publican sin auditoría) |

---

### 1. BREVE DESCRIPCIÓN

Permite que un Profesor modifique un curso propio y envíe la nueva versión a
auditoría administrativa antes de su publicación.

### 2. PRECONDICIONES

- El Profesor debe haber iniciado sesión con un Token JWT válido.
- El curso debe existir y pertenecer al Profesor, conforme a **RN-14**.
- El curso no debe encontrarse FINALIZADO, conforme a **RN-15**.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El actor consulta uno de sus cursos mediante `GET /api/cursos/{cursoId}`.
2. El Sistema devuelve la información actual, sus módulos y duraciones.
3. El actor envía una petición `PUT /api/cursos/{cursoId}` con los datos,
   módulos o duraciones modificados y confirma el envío a revisión.
4. La **Capa de Presentación** valida el esquema y los campos obligatorios de
   **RN-07**.
5. La **Capa de Negocio** verifica la propiedad del curso y que no esté
   FINALIZADO, aplicando **RN-14** y **RN-15**.
6. La **Capa de Negocio** valida los módulos y recalcula la duración total,
   aplicando **RN-08** y **RN-09**.
7. La **Capa de Persistencia** registra los cambios como una nueva versión no
   publicada, conforme a **RN-16**.
8. El Sistema cambia el curso a **EN REVISIÓN** y genera una nueva solicitud de
   auditoría, aplicando **RN-10**.
9. El Sistema devuelve **200 OK** con la versión actualizada y la confirmación de
   envío a revisión.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Curso con cambios solicitados (HTTP 200 OK):**
  1. Si en el Paso 3 el curso está en CAMBIOS SOLICITADOS, el Profesor corrige la
     información indicada por la auditoría.
  2. Al confirmar, el Sistema vuelve a aplicar las validaciones de los Pasos 4 a
     6.
  3. El curso vuelve a EN REVISIÓN y el Sistema devuelve **200 OK**. Fin del caso
     de uso.

* **4a. Información obligatoria incompleta (HTTP 400 Bad Request):**
  1. Si en el Paso 4 faltan datos exigidos por **RN-07**, la Capa de Presentación
     identifica los campos pendientes.
  2. El Sistema devuelve **400 Bad Request** y no registra la versión. Fin del
     caso de uso.

* **6a. Curso sin módulos válidos (HTTP 409 Conflict):**
  1. Si en el Paso 6 el curso queda sin módulos válidos, se incumple **RN-08**.
  2. La Capa de Negocio rechaza el envío a revisión.
  3. El Sistema devuelve **409 Conflict**. Fin del caso de uso.

* **1a. Usuario no autenticado (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 no se presenta un Token JWT válido, el Sistema devuelve
     **401 Unauthorized**. Fin del caso de uso.

* **5a. Curso ajeno (HTTP 403 Forbidden):**
  1. Si en el Paso 5 el curso pertenece a otro Profesor, se incumple **RN-14**.
  2. La Capa de Negocio impide la modificación.
  3. El Sistema devuelve **403 Forbidden**. Fin del caso de uso.

* **1b. Curso inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 1 no existe el curso indicado, la Capa de Negocio no encuentra
     el recurso.
  2. El Sistema devuelve **404 Not Found**. Fin del caso de uso.

* **5b. Curso finalizado (HTTP 409 Conflict):**
  1. Si en el Paso 5 el curso está FINALIZADO, se incumple **RN-15**.
  2. La Capa de Negocio rechaza la modificación.
  3. El Sistema devuelve **409 Conflict**. Fin del caso de uso.

### 5. SUB-VARIACIONES

1. El Profesor puede modificar datos generales, módulos, recursos o duraciones;
   todas las variantes generan una versión pendiente de auditoría.

### 6. POSTCONDICIONES

- Los cambios quedan registrados como una nueva versión no publicada.
- La duración total queda recalculada según **RN-09**.
- El curso queda EN REVISIÓN y posee una nueva solicitud de auditoría.
- La versión modificada no se considera publicada hasta su aprobación.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | La nueva versión fue registrada y enviada a auditoría. |
| `400` | Bad Request | La actualización contiene información obligatoria incompleta. |
| `401` | Unauthorized | No existe una autenticación válida. |
| `403` | Forbidden | El Profesor intenta modificar un curso ajeno. |
| `404` | Not Found | El curso indicado no existe. |
| `409` | Conflict | El curso está FINALIZADO o no conserva módulos válidos. |

### Matriz de trazabilidad CU-05 → Test

| Paso del CU | Excepción / Código | Test unitario (Negocio) | Test de integración (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Consultar curso | `200 OK` | `GetCourseAsync_WithExistingOwnedId_ReturnsCourse` | `GetCourse_AsOwner_Returns200OK` |
| Paso 2. Obtener detalle | `200 OK` | `GetCourseAsync_IncludesModulesAndDurations` | `GetCourse_ReturnsCompleteEditableDetail` |
| Paso 3. Enviar cambios | `200 OK` | — (entrada HTTP) | `UpdateCourse_WithValidRequest_AcceptsPayload` |
| Paso 4. Validar esquema | `200 OK` | `UpdateCourseAsync_WithCompleteData_ContinuesUpdate` | `UpdateCourse_WithCompleteData_Returns200OK` |
| Paso 5. Verificar propiedad y estado | `200 OK` | `UpdateCourseAsync_AsOwnerAndNotFinalized_AllowsUpdate` | `UpdateCourse_AsOwner_Returns200OK` |
| Paso 6. Validar módulos y duración | `200 OK` | `UpdateCourseAsync_RecalculatesModuleDuration` | `UpdateCourse_WithModules_ReturnsCalculatedDuration` |
| Paso 7. Crear nueva versión | `200 OK` | `UpdateCourseAsync_PersistsUnpublishedVersion` | `UpdateCourse_PersistsPendingVersion` |
| Paso 8. Generar auditoría | `200 OK` | `UpdateCourseAsync_CreatesNewAuditRequest` | `UpdateCourse_CreatesPendingAudit` |
| Paso 9. Responder actualización | `200 OK` | `UpdateCourseAsync_WithValidData_ReturnsCourseInReview` | `UpdateCourse_WithValidData_Returns200OK` |
| 3a. Cambios solicitados | `200 OK` | `UpdateCourseAsync_WhenChangesRequested_ReturnsToReview` | `UpdateCourse_WhenChangesRequested_Returns200OK` |
| 4a. Datos incompletos | `400 Bad Request` | `UpdateCourseAsync_WithMissingData_ThrowsValidationException` | `UpdateCourse_WithMissingData_Returns400BadRequest` |
| 6a. Sin módulos | `409 Conflict` | `UpdateCourseAsync_WithoutModules_ThrowsCourseRuleException` | `UpdateCourse_WithoutModules_Returns409Conflict` |
| 1a. Sin autenticación | `401 Unauthorized` | — (autenticación HTTP) | `UpdateCourse_WithoutToken_Returns401Unauthorized` |
| 5a. Curso ajeno | `403 Forbidden` | `UpdateCourseAsync_ForForeignCourse_ThrowsForbiddenException` | `UpdateCourse_ForForeignCourse_Returns403Forbidden` |
| 1b. Curso inexistente | `404 Not Found` | `GetCourseAsync_WithUnknownId_ThrowsCourseNotFoundException` | `GetCourse_WithUnknownId_Returns404NotFound` |
| 5b. Curso finalizado | `409 Conflict` | `UpdateCourseAsync_WhenFinalized_ThrowsCourseStateException` | `UpdateCourse_WhenFinalized_Returns409Conflict` |

> Los nombres de tests establecen el contrato de trazabilidad del caso de uso y
> deberán coincidir con la suite automatizada cuando se implemente.
