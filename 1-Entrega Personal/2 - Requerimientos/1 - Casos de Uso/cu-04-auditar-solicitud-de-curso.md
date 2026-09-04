# Caso de Uso: Auditar Solicitud de Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y adaptada a la
> sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-04 |
| **Nombre** | Auditar Solicitud de Curso |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Administrador → revisar y resolver solicitudes; Profesor → conocer el resultado y corregir cuando corresponda; Alumnos → acceder únicamente a cursos aprobados |
| **Disparador (Trigger)** | El Administrador selecciona una solicitud EN REVISIÓN |
| **Prioridad / Frecuencia** | Alta; frecuencia media |
| **Reglas de negocio relacionadas** | RN-11 (solo se resuelven solicitudes EN REVISIÓN); RN-12 (la decisión determina el estado del curso); RN-13 (SOLICITAR CAMBIOS requiere observación) |

---

### 1. BREVE DESCRIPCIÓN

Permite que un Administrador revise una solicitud de creación o modificación de
curso y la apruebe, rechace o devuelva al Profesor con cambios solicitados.

### 2. PRECONDICIONES

- El Administrador debe haber iniciado sesión con un Token JWT válido.
- Debe existir una solicitud de curso con estado EN REVISIÓN.
- El curso, sus módulos y sus cambios deben estar disponibles para consulta.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El actor consulta las solicitudes mediante `GET /api/auditorias-cursos?estado=EN_REVISION`.
2. El Sistema devuelve las solicitudes pendientes y el Administrador selecciona
   una.
3. El Sistema obtiene el curso, sus módulos y los cambios asociados a la
   solicitud.
4. El Administrador revisa la información y envía una petición
   `PATCH /api/auditorias-cursos/{solicitudId}` con la decisión `APROBAR`.
5. La **Capa de Presentación** valida el formato de la decisión.
6. La **Capa de Negocio** verifica que la solicitud continúe EN REVISIÓN,
   aplicando **RN-11**.
7. La **Capa de Persistencia** registra la resolución y cambia el curso a
   **PUBLICADO**, aplicando **RN-12**.
8. El Sistema devuelve **200 OK** con la auditoría resuelta y el nuevo estado del
   curso.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **4a. Rechazar solicitud (HTTP 200 OK):**
  1. Si en el Paso 4 el Administrador selecciona `RECHAZAR`, la Capa de Negocio
     valida la decisión.
  2. La Capa de Persistencia registra la resolución y cambia el curso a
     **RECHAZADO**, conforme a **RN-12**.
  3. El Sistema devuelve **200 OK** y el curso no se publica. Fin del caso de uso.

* **4b. Solicitar cambios (HTTP 200 OK):**
  1. Si en el Paso 4 el Administrador selecciona `SOLICITAR_CAMBIOS`, debe
     incluir una observación conforme a **RN-13**.
  2. La Capa de Persistencia registra la resolución y cambia el curso a
     **CAMBIOS SOLICITADOS**, conforme a **RN-12**.
  3. El Sistema devuelve **200 OK**; el Profesor puede corregir el curso y volver
     a enviarlo. Fin del caso de uso.

* **4c. Observación faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 4 se solicitan cambios sin observación, se incumple **RN-13**.
  2. La Capa de Presentación rechaza la petición.
  3. El Sistema devuelve **400 Bad Request**. Fin del caso de uso.

* **1a. Usuario no autenticado (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 no se presenta un Token JWT válido, el Sistema devuelve
     **401 Unauthorized**. Fin del caso de uso.

* **4d. Rol no autorizado (HTTP 403 Forbidden):**
  1. Si en el Paso 4 el usuario autenticado no es Administrador, la Capa de
     Negocio impide resolver la auditoría.
  2. El Sistema devuelve **403 Forbidden**. Fin del caso de uso.

* **3a. Solicitud inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 no existe la solicitud indicada, la Capa de Negocio no
     encuentra el recurso.
  2. El Sistema devuelve **404 Not Found**. Fin del caso de uso.

* **6a. Solicitud ya resuelta (HTTP 409 Conflict):**
  1. Si en el Paso 6 la solicitud ya no está EN REVISIÓN, se incumple **RN-11**.
  2. La Capa de Negocio evita una segunda resolución.
  3. El Sistema devuelve **409 Conflict**. Fin del caso de uso.

### 5. SUB-VARIACIONES

1. La solicitud puede corresponder a la creación de un curso o a cambios sobre un
   curso existente; las tres decisiones disponibles producen los mismos estados
   finales.

### 6. POSTCONDICIONES

- La auditoría queda resuelta con la decisión del Administrador.
- El curso queda PUBLICADO, RECHAZADO o CAMBIOS SOLICITADOS según **RN-12**.
- El Profesor puede consultar el estado y, si corresponde, la observación.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | La auditoría fue resuelta y el curso cambió de estado. |
| `400` | Bad Request | La decisión es inválida o falta una observación obligatoria. |
| `401` | Unauthorized | No existe una autenticación válida. |
| `403` | Forbidden | El usuario autenticado no posee rol Administrador. |
| `404` | Not Found | La solicitud indicada no existe. |
| `409` | Conflict | La solicitud ya fue resuelta y no continúa EN REVISIÓN. |

### Matriz de trazabilidad CU-04 → Test

| Paso del CU | Excepción / Código | Test unitario (Negocio) | Test de integración (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Consultar solicitudes | `200 OK` | `GetPendingAuditsAsync_ReturnsInReviewRequests` | `GetCourseAudits_AsAdmin_ReturnsPendingRequests` |
| Paso 2. Seleccionar solicitud | `200 OK` | `GetAuditAsync_WithExistingId_ReturnsRequest` | `GetCourseAudit_WithExistingId_Returns200OK` |
| Paso 3. Obtener detalle | `200 OK` | `GetAuditAsync_IncludesCourseModulesAndChanges` | `GetCourseAudit_ReturnsCompleteCourseDetail` |
| Paso 4. Enviar aprobación | `200 OK` | `ResolveAuditAsync_WithApproveDecision_ContinuesResolution` | `ResolveCourseAudit_WithApproveDecision_AcceptsRequest` |
| Paso 5. Validar decisión | `200 OK` | `ResolveAuditAsync_WithValidDecision_ContinuesResolution` | `ResolveCourseAudit_WithValidDecision_Returns200OK` |
| Paso 6. Verificar estado | `200 OK` | `ResolveAuditAsync_WhenInReview_AllowsResolution` | `ResolveCourseAudit_WhenInReview_Returns200OK` |
| Paso 7. Publicar curso | `200 OK` | `ResolveAuditAsync_WhenApproved_PublishesCourse` | `ResolveCourseAudit_WhenApproved_PersistsPublishedStatus` |
| Paso 8. Responder resolución | `200 OK` | `ResolveAuditAsync_WhenApproved_ReturnsResolvedAudit` | `ResolveCourseAudit_WhenApproved_Returns200OK` |
| 4a. Rechazar | `200 OK` | `ResolveAuditAsync_WhenRejected_SetsRejectedStatus` | `ResolveCourseAudit_WhenRejected_Returns200OK` |
| 4b. Solicitar cambios | `200 OK` | `ResolveAuditAsync_WhenChangesRequested_SetsChangesRequestedStatus` | `ResolveCourseAudit_WhenChangesRequested_Returns200OK` |
| 4c. Sin observación | `400 Bad Request` | `ResolveAuditAsync_WithoutRequiredObservation_ThrowsValidationException` | `ResolveCourseAudit_WithoutObservation_Returns400BadRequest` |
| 1a. Sin autenticación | `401 Unauthorized` | — (autenticación HTTP) | `GetCourseAudits_WithoutToken_Returns401Unauthorized` |
| 4d. Rol no autorizado | `403 Forbidden` | `ResolveAuditAsync_AsNonAdmin_ThrowsForbiddenException` | `ResolveCourseAudit_AsTeacher_Returns403Forbidden` |
| 3a. Solicitud inexistente | `404 Not Found` | `GetAuditAsync_WithUnknownId_ThrowsAuditNotFoundException` | `GetCourseAudit_WithUnknownId_Returns404NotFound` |
| 6a. Solicitud resuelta | `409 Conflict` | `ResolveAuditAsync_WhenAlreadyResolved_ThrowsAuditStateException` | `ResolveCourseAudit_WhenAlreadyResolved_Returns409Conflict` |

> Los nombres de tests establecen el contrato de trazabilidad del caso de uso y
> deberán coincidir con la suite automatizada cuando se implemente.
