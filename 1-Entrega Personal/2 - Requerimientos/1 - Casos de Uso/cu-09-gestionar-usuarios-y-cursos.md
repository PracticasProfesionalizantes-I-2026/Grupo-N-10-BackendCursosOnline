# Caso de Uso: Gestionar Usuarios y Cursos

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y adaptada a la
> sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-09 |
| **Nombre** | Gestionar Usuarios y Cursos |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Administrador → mantener usuarios y cursos dentro de estados válidos; Profesores → conservar cursos correctamente administrados; Alumnos → visualizar únicamente cursos disponibles |
| **Disparador (Trigger)** | El Administrador accede a la sección de gestión de usuarios o cursos |
| **Prioridad / Frecuencia** | Alta; frecuencia media |
| **Reglas de negocio relacionadas** | RN-26 (solo el Administrador ejecuta acciones de gestión general); RN-27 (solo un curso PUBLICADO puede pausarse y deja de admitir inscripciones); RN-28 (un curso FINALIZADO no admite nuevas inscripciones); RN-29 (las cuentas administrativas se gestionan internamente) |

---

### 1. BREVE DESCRIPCIÓN

Permite que un Administrador consulte usuarios y cursos y ejecute las acciones de
gestión previstas para cada registro dentro de las reglas del sistema.

### 2. PRECONDICIONES

- El Administrador debe haber iniciado sesión con un Token JWT válido.
- La cuenta autenticada debe poseer rol Administrador conforme a **RN-26**.
- Debe existir el usuario o curso sobre el que se realizará la acción.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El actor consulta `GET /api/usuarios` o `GET /api/cursos`, según el recurso
   que desea gestionar.
2. El Sistema devuelve los registros y sus estados actuales.
3. El actor selecciona un usuario o curso.
4. El Sistema devuelve las acciones permitidas para el registro según su tipo y
   estado.
5. El actor envía la acción prevista mediante `PATCH /api/usuarios/{usuarioId}`
   o `PATCH /api/cursos/{cursoId}`.
6. La **Capa de Presentación** valida el formato de la acción y sus datos.
7. La **Capa de Negocio** verifica el rol Administrador y las reglas aplicables al
   estado del registro, incluida **RN-26**.
8. La **Capa de Persistencia** actualiza la información o el estado.
9. El Sistema devuelve **200 OK** con el registro actualizado y la confirmación de
   la acción.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **5a. Pausar curso (HTTP 200 OK):**
  1. Si en el Paso 5 el Administrador selecciona `PAUSAR` sobre un curso
     PUBLICADO, la Capa de Negocio valida **RN-27**.
  2. La Capa de Persistencia cambia el curso a PAUSADO.
  3. El Sistema devuelve **200 OK** y el curso deja de admitir nuevas
     inscripciones. Fin del caso de uso.

* **5b. Finalizar curso (HTTP 200 OK):**
  1. Si en el Paso 5 el Administrador selecciona `FINALIZAR` sobre un curso
     activo, la Capa de Negocio valida **RN-28**.
  2. La Capa de Persistencia cambia el curso a FINALIZADO.
  3. El Sistema devuelve **200 OK** y el curso no admite nuevas inscripciones. Fin
     del caso de uso.

* **6a. Datos o acción inválidos (HTTP 400 Bad Request):**
  1. Si en el Paso 6 la acción no pertenece al conjunto permitido o faltan datos
     obligatorios, la Capa de Presentación rechaza la petición.
  2. El Sistema devuelve **400 Bad Request**. Fin del caso de uso.

* **1a. Usuario no autenticado (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 no se presenta un Token JWT válido, el Sistema devuelve
     **401 Unauthorized**. Fin del caso de uso.

* **7a. Rol no autorizado (HTTP 403 Forbidden):**
  1. Si en el Paso 7 el usuario no posee rol Administrador, se incumple
     **RN-26**.
  2. La Capa de Negocio impide la acción.
  3. El Sistema devuelve **403 Forbidden**. Fin del caso de uso.

* **3a. Registro inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 no existe el usuario o curso indicado, la Capa de Negocio no
     encuentra el recurso.
  2. El Sistema devuelve **404 Not Found**. Fin del caso de uso.

* **7b. Transición de curso no permitida (HTTP 409 Conflict):**
  1. Si en el Paso 7 se intenta pausar un curso que no está PUBLICADO o ejecutar
     una transición incompatible con su estado, se incumple **RN-27** o **RN-28**.
  2. La Capa de Negocio rechaza la acción.
  3. El Sistema devuelve **409 Conflict**. Fin del caso de uso.

### 5. SUB-VARIACIONES

1. Para usuarios, las acciones modifican la información o el estado de la cuenta
   dentro del alcance permitido.
2. Para cursos, las acciones incluyen revisar información, editar, pausar o
   finalizar según el estado actual.
3. Las cuentas administrativas solo se gestionan por el proceso interno definido
   en **RN-29**.

### 6. POSTCONDICIONES

- La acción administrativa queda registrada.
- El usuario o curso refleja la información o el estado resultante.
- Un curso PAUSADO o FINALIZADO deja de admitir nuevas inscripciones.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | La consulta o la acción administrativa se completó correctamente. |
| `400` | Bad Request | La acción o sus datos no cumplen el contrato de entrada. |
| `401` | Unauthorized | No existe una autenticación válida. |
| `403` | Forbidden | El usuario autenticado no posee rol Administrador. |
| `404` | Not Found | El usuario o curso indicado no existe. |
| `409` | Conflict | La transición solicitada no es compatible con el estado del curso. |

### Matriz de trazabilidad CU-09 → Test

| Paso del CU | Excepción / Código | Test unitario (Negocio) | Test de integración (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Consultar registros | `200 OK` | `GetManagedResourcesAsync_ReturnsRequestedCollection` | `GetManagedResources_AsAdmin_Returns200OK` |
| Paso 2. Mostrar estados | `200 OK` | `GetManagedResourcesAsync_IncludesCurrentStatus` | `GetManagedResources_ReturnsCurrentStatuses` |
| Paso 3. Seleccionar registro | `200 OK` | `GetManagedResourceAsync_WithExistingId_ReturnsResource` | `GetManagedResource_WithExistingId_Returns200OK` |
| Paso 4. Obtener acciones | `200 OK` | `GetAllowedActionsAsync_ReturnsActionsForCurrentState` | `GetManagedResource_ReturnsAllowedActions` |
| Paso 5. Enviar acción | `200 OK` | — (entrada HTTP) | `ManageResource_WithValidAction_AcceptsRequest` |
| Paso 6. Validar acción | `200 OK` | `ManageResourceAsync_WithValidAction_ContinuesUpdate` | `ManageResource_WithValidAction_Returns200OK` |
| Paso 7. Verificar permisos y estado | `200 OK` | `ManageResourceAsync_AsAdminWithValidState_AllowsAction` | `ManageResource_AsAdminWithValidState_Returns200OK` |
| Paso 8. Persistir cambios | `200 OK` | `ManageResourceAsync_WithValidAction_PersistsChanges` | `ManageResource_WithValidAction_PersistsChanges` |
| Paso 9. Responder actualización | `200 OK` | `ManageResourceAsync_WithValidAction_ReturnsUpdatedResource` | `ManageResource_WithValidAction_ReturnsUpdatedResource` |
| 5a. Pausar curso | `200 OK` | `PauseCourseAsync_WhenPublished_SetsPausedAndBlocksEnrollments` | `PauseCourse_WhenPublished_Returns200OK` |
| 5b. Finalizar curso | `200 OK` | `FinalizeCourseAsync_WhenActive_SetsFinalizedAndBlocksEnrollments` | `FinalizeCourse_WhenActive_Returns200OK` |
| 6a. Acción inválida | `400 Bad Request` | `ManageResourceAsync_WithInvalidAction_ThrowsValidationException` | `ManageResource_WithInvalidAction_Returns400BadRequest` |
| 1a. Sin autenticación | `401 Unauthorized` | — (autenticación HTTP) | `GetManagedResources_WithoutToken_Returns401Unauthorized` |
| 7a. Rol no autorizado | `403 Forbidden` | `ManageResourceAsync_AsNonAdmin_ThrowsForbiddenException` | `ManageResource_AsTeacher_Returns403Forbidden` |
| 3a. Registro inexistente | `404 Not Found` | `GetManagedResourceAsync_WithUnknownId_ThrowsNotFoundException` | `GetManagedResource_WithUnknownId_Returns404NotFound` |
| 7b. Transición inválida | `409 Conflict` | `ManageCourseAsync_WithInvalidTransition_ThrowsCourseStateException` | `ManageCourse_WithInvalidTransition_Returns409Conflict` |

> Los nombres de tests establecen el contrato de trazabilidad del caso de uso y
> deberán coincidir con la suite automatizada cuando se implemente.
