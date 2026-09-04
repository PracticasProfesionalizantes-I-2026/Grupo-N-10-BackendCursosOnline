# Caso de Uso: Gestionar Solicitudes de Inscripción

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y adaptada a la
> sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-07 |
| **Nombre** | Gestionar Solicitudes de Inscripción |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Administrador → resolver solicitudes pendientes; Alumno → conocer la decisión y obtener acceso si es aprobado; Profesor → mantener la nómina válida de su curso |
| **Disparador (Trigger)** | El Administrador accede a las solicitudes de inscripción pendientes |
| **Prioridad / Frecuencia** | Alta; frecuencia alta |
| **Reglas de negocio relacionadas** | RN-20 (solo una solicitud PENDIENTE puede aprobarse o rechazarse); RN-21 (APROBADA concede acceso); RN-22 (CANCELADA retira el acceso activo) |

---

### 1. BREVE DESCRIPCIÓN

Permite que un Administrador revise solicitudes de inscripción pendientes, las
apruebe o rechace y, cuando corresponda, cancele una inscripción aprobada.

### 2. PRECONDICIONES

- El Administrador debe haber iniciado sesión con un Token JWT válido.
- Debe existir al menos una solicitud PENDIENTE para aprobar o rechazar.
- Para cancelar, debe existir una inscripción APROBADA.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El actor consulta `GET /api/inscripciones?estado=PENDIENTE`.
2. El Sistema devuelve las solicitudes PENDIENTES.
3. El actor selecciona una solicitud y consulta su Alumno y curso relacionado.
4. El actor envía `PATCH /api/inscripciones/{inscripcionId}` con el nuevo estado
   `APROBADA`.
5. La **Capa de Presentación** valida el formato de la decisión.
6. La **Capa de Negocio** verifica que la solicitud continúe PENDIENTE, aplicando
   **RN-20**.
7. La **Capa de Persistencia** cambia la inscripción a **APROBADA** y habilita el
   acceso al curso, aplicando **RN-21**.
8. El Sistema devuelve **200 OK** con la inscripción resuelta.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **4a. Rechazar inscripción (HTTP 200 OK):**
  1. Si en el Paso 4 el Administrador selecciona `RECHAZADA`, la Capa de Negocio
     verifica **RN-20**.
  2. La Capa de Persistencia cambia la solicitud a RECHAZADA.
  3. El Sistema devuelve **200 OK** y el Alumno no obtiene acceso al curso. Fin
     del caso de uso.

* **2a. Cancelar inscripción aprobada (HTTP 200 OK):**
  1. Si en el Paso 2 el Administrador consulta una inscripción APROBADA que debe
     anularse, envía el estado `CANCELADA`.
  2. La Capa de Negocio valida la transición y la Capa de Persistencia cambia el
     estado.
  3. El Sistema retira el acceso activo conforme a **RN-22** y devuelve
     **200 OK**. Fin del caso de uso.

* **1a. Usuario no autenticado (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 no se presenta un Token JWT válido, el Sistema devuelve
     **401 Unauthorized**. Fin del caso de uso.

* **4b. Rol no autorizado (HTTP 403 Forbidden):**
  1. Si en el Paso 4 la cuenta autenticada no posee rol Administrador, la Capa de
     Negocio impide resolver la inscripción.
  2. El Sistema devuelve **403 Forbidden**. Fin del caso de uso.

* **3a. Inscripción inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 no existe la inscripción indicada, la Capa de Negocio no
     encuentra el recurso.
  2. El Sistema devuelve **404 Not Found**. Fin del caso de uso.

* **6a. Solicitud ya resuelta (HTTP 409 Conflict):**
  1. Si en el Paso 6 la solicitud ya no está PENDIENTE, se incumple **RN-20**.
  2. La Capa de Negocio rechaza una segunda resolución.
  3. El Sistema devuelve **409 Conflict**. Fin del caso de uso.

* **5a. Estado solicitado inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 5 el estado no es APROBADA, RECHAZADA o CANCELADA, la Capa de
     Presentación rechaza la petición.
  2. El Sistema devuelve **400 Bad Request**. Fin del caso de uso.

### 5. SUB-VARIACIONES

1. La resolución de una solicitud PENDIENTE puede ser APROBADA o RECHAZADA.
2. La cancelación es una transición distinta y solo parte de una inscripción
   APROBADA.

### 6. POSTCONDICIONES

- La solicitud queda APROBADA o RECHAZADA.
- Si queda APROBADA, el Alumno obtiene acceso al curso según **RN-21**.
- Si una inscripción aprobada se cancela, queda CANCELADA y se retira el acceso
  según **RN-22**.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | La inscripción fue consultada o cambió a un estado permitido. |
| `400` | Bad Request | El estado solicitado no pertenece al conjunto admitido. |
| `401` | Unauthorized | No existe una autenticación válida. |
| `403` | Forbidden | El usuario autenticado no posee rol Administrador. |
| `404` | Not Found | La inscripción indicada no existe. |
| `409` | Conflict | La inscripción no se encuentra en el estado requerido para la transición. |

### Matriz de trazabilidad CU-07 → Test

| Paso del CU | Excepción / Código | Test unitario (Negocio) | Test de integración (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Consultar pendientes | `200 OK` | `GetEnrollmentsAsync_WithPendingFilter_ReturnsPendingRequests` | `GetPendingEnrollments_AsAdmin_Returns200OK` |
| Paso 2. Mostrar solicitudes | `200 OK` | `GetEnrollmentsAsync_ExcludesResolvedRequests` | `GetPendingEnrollments_ReturnsOnlyPendingRequests` |
| Paso 3. Consultar detalle | `200 OK` | `GetEnrollmentAsync_IncludesStudentAndCourse` | `GetEnrollment_ReturnsRelatedStudentAndCourse` |
| Paso 4. Enviar aprobación | `200 OK` | — (entrada HTTP) | `ResolveEnrollment_WithApprovedState_AcceptsRequest` |
| Paso 5. Validar decisión | `200 OK` | `ResolveEnrollmentAsync_WithAllowedState_ContinuesResolution` | `ResolveEnrollment_WithAllowedState_Returns200OK` |
| Paso 6. Verificar pendiente | `200 OK` | `ResolveEnrollmentAsync_WhenPending_AllowsResolution` | `ResolveEnrollment_WhenPending_Returns200OK` |
| Paso 7. Aprobar y habilitar | `200 OK` | `ResolveEnrollmentAsync_WhenApproved_GrantsCourseAccess` | `ResolveEnrollment_WhenApproved_PersistsAccess` |
| Paso 8. Responder resolución | `200 OK` | `ResolveEnrollmentAsync_WhenApproved_ReturnsResolvedEnrollment` | `ResolveEnrollment_WhenApproved_Returns200OK` |
| 4a. Rechazar | `200 OK` | `ResolveEnrollmentAsync_WhenRejected_DoesNotGrantAccess` | `ResolveEnrollment_WhenRejected_Returns200OK` |
| 2a. Cancelar aprobada | `200 OK` | `CancelEnrollmentAsync_WhenApproved_RevokesCourseAccess` | `CancelEnrollment_WhenApproved_Returns200OK` |
| 1a. Sin autenticación | `401 Unauthorized` | — (autenticación HTTP) | `GetPendingEnrollments_WithoutToken_Returns401Unauthorized` |
| 4b. Rol no autorizado | `403 Forbidden` | `ResolveEnrollmentAsync_AsNonAdmin_ThrowsForbiddenException` | `ResolveEnrollment_AsTeacher_Returns403Forbidden` |
| 3a. Inscripción inexistente | `404 Not Found` | `GetEnrollmentAsync_WithUnknownId_ThrowsEnrollmentNotFoundException` | `GetEnrollment_WithUnknownId_Returns404NotFound` |
| 6a. Solicitud resuelta | `409 Conflict` | `ResolveEnrollmentAsync_WhenNotPending_ThrowsEnrollmentStateException` | `ResolveEnrollment_WhenNotPending_Returns409Conflict` |
| 5a. Estado inválido | `400 Bad Request` | `ResolveEnrollmentAsync_WithInvalidState_ThrowsValidationException` | `ResolveEnrollment_WithInvalidState_Returns400BadRequest` |

> Los nombres de tests establecen el contrato de trazabilidad del caso de uso y
> deberán coincidir con la suite automatizada cuando se implemente.
