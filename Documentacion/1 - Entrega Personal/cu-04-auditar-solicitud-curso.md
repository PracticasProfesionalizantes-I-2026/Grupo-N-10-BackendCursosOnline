# Caso de Uso: Auditar Solicitud de Curso

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-08 (solo administrador puede auditar), RN-09 (curso debe estar EN_REVISION) **implementadas** en el código; cada caso borde cuenta con su test unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-04 |
| **Nombre** | Auditar Solicitud de Curso |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Administrador → garantizar calidad de cursos publicados; Profesor → obtener resolución de su solicitud; Alumno → acceder a cursos aprobados y de calidad; Sistema → trazabilidad de decisiones de auditoría |
| **Disparador (Trigger)** | El administrador selecciona una solicitud pendiente de revisión desde el panel de administración |
| **Prioridad / Frecuencia** | Alta; frecuencia media (según volumen de solicitudes de profesores) |
| **Reglas de negocio relacionadas** | RN-08 (solo usuarios con rol Administrador pueden auditar); RN-09 (solo cursos en estado EN_REVISION pueden ser auditados); RN-10 (registro de decisión con observaciones obligatorias al rechazar) |

---

### 1. BREVE DESCRIPCIÓN
Permite que un administrador autenticado revise una solicitud de creación de curso realizada por un profesor (en estado EN_REVISION), visualice la información completa del curso (datos básicos, módulos, contenidos), y determine si la misma es aprobada (curso pasa a PUBLICADO) o rechazada (curso pasa a RECHAZADO con observaciones), registrando la decisión en el sistema.

### 2. PRECONDICIONES
- El administrador debe haber iniciado sesión y poseer un Token JWT válido con rol "Administrador".
- Debe existir al menos una solicitud de curso en estado `EN_REVISION` en el sistema.
- El sistema debe encontrarse disponible y con la Capa de Persistencia accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor (Administrador autenticado) envía una petición al endpoint `GET /api/admin/cursos/pendientes` para listar solicitudes pendientes.
2. La **Capa de Presentación** (`AdminController.ListarPendientes`) valida el token y rol (policy `RequireAdminRole`).
3. La **Capa de Negocio** (`CursoService.ObtenerPendientesAsync`) recupera los cursos con estado `EN_REVISION` y devuelve lista resumida (ID, título, profesor, fecha solicitud).
4. El Actor selecciona un curso y envía una petición al endpoint `GET /api/admin/cursos/{id}` para ver detalle completo.
5. La **Capa de Negocio** (`CursoService.ObtenerPorIdAsync`) verifica la regla **RN-09**: el curso debe estar en estado `EN_REVISION`; si no, lanza `InvalidStateException`.
6. La **Capa de Persistencia** recupera el curso con sus módulos, contenidos y datos del profesor propietario.
7. El Sistema devuelve un código **200 OK** con la información completa del curso.
8. El Actor revisa la información y envía una petición al endpoint `PUT /api/admin/cursos/{id}/aprobar` con un JSON opcional de observaciones (`observaciones`).
9. La **Capa de Negocio** (`CursoService.AprobarAsync`) verifica **RN-08** (rol Admin en token) y **RN-09** (estado actual `EN_REVISION`).
10. La **Capa de Negocio** actualiza el estado del curso a `PUBLICADO`, registra la fecha de aprobación, el `adminId` auditor y observaciones (si las hay).
11. La **Capa de Persistencia** persiste los cambios en la tabla `Cursos` y registra en tabla de auditoría `AuditoriaCursos` (cursoId, adminId, accion: "APROBADO", fecha, observaciones).
12. El Sistema devuelve un código **200 OK** con el curso actualizado (estado `PUBLICADO`).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido en aprobación (HTTP 400 Bad Request):**
  1. Si en el Paso 8 el cuerpo de la petición no es un JSON válido.
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición por error de esquema.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **3a. Sin solicitudes pendientes (HTTP 200 OK - lista vacía):**
  1. Si en el Paso 3 no existen cursos en estado `EN_REVISION`.
  2. La **Capa de Negocio** devuelve lista vacía.
  3. El Sistema devuelve **200 OK** con array vacío `[]`. Fin del caso de uso (no hay curso para auditar).

* **5a. Curso no está en estado EN_REVISION (HTTP 409 Conflict):**
  1. Si en el Paso 5 el curso consultado no tiene estado `EN_REVISION` (ej. ya fue auditado, está en BORRADOR, PUBLICADO, RECHAZADO), violando **RN-09**.
  2. La **Capa de Negocio** lanza una `InvalidStateException`.
  3. El Sistema devuelve un código **409 Conflict** con mensaje: `"El curso no se encuentra en estado pendiente de revisión."`. Fin del caso de uso.

* **8a. Curso no encontrado (HTTP 404 Not Found):**
  1. Si en el Paso 4 o 8 el `id` del curso no existe en la base de datos.
  2. La **Capa de Negocio** no encuentra la entidad.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **9a. Usuario sin rol Administrador (HTTP 403 Forbidden):**
  1. Si en el Paso 9 el token JWT no contiene el claim de rol "Administrador", violando **RN-08**.
  2. La **Capa de Presentación** (policy de autorización) rechaza la petición antes de llegar al servicio.
  3. El Sistema devuelve un código **403 Forbidden**. Fin del caso de uso.

* **10a. Observaciones obligatorias al rechazar (ver flujo A1):**
  - Ver flujo alternativo A1 "Rechazar solicitud".

* **11a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 11 la **Capa de Persistencia** no puede guardar los cambios.
  2. El Sistema interrumpe la operación y registra el error.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

* **A1. Rechazar solicitud (HTTP 200 OK / 400 Bad Request):**
  1. En el Paso 8, el Actor decide rechazar y envía petición a `PUT /api/admin/cursos/{id}/rechazar` con JSON obligatorio: `observaciones` (motivo del rechazo).
  2. La **Capa de Presentación** valida que `observaciones` esté presente y no vacío (`[Required]` en `RechazarCursoDTO`).
  3. Si `observaciones` falta o está vacío: **Capa de Presentación** devuelve **400 Bad Request** con mensaje: `"Las observaciones son obligatorias al rechazar."`. Fin del caso de uso.
  4. La **Capa de Negocio** (`CursoService.RechazarAsync`) verifica **RN-08**, **RN-09** y **RN-10** (observaciones obligatorias).
  5. La **Capa de Negocio** actualiza el estado del curso a `RECHAZADO`, registra fecha, `adminId` y observaciones.
  6. La **Capa de Persistencia** persiste en `Cursos` y `AuditoriaCursos` (accion: "RECHAZADO").
  7. El Sistema devuelve **200 OK** con el curso actualizado (estado `RECHAZADO`).

* **A2. Información incompleta detectada durante la revisión (HTTP 200 OK con observaciones):**
  1. En el Paso 8, el Actor detecta información insuficiente y envía petición a `PUT /api/admin/cursos/{id}/observaciones` con JSON: `observaciones` (detalle de lo que falta).
  2. La **Capa de Negocio** registra las observaciones en `AuditoriaCursos` sin cambiar el estado (queda `EN_REVISION`).
  3. El profesor es notificado (evento de dominio) y deberá corregir la información antes de volver a solicitar aprobación (ver CU-06).
  4. El Sistema devuelve **200 OK** con confirmación de observaciones registradas.

### 5. SUB-VARIACIONES (opcional)
1. El administrador puede acceder al listado y detalle desde el panel web de administración, desde la colección de Bruno (`GET Pendientes.bru`, `GET CursoDetalle.bru`, `PUT Aprobar.bru`, `PUT Rechazar.bru`) o desde un cliente HTTP.
2. La notificación al profesor tras rechazo u observaciones puede ser por email, notificación in-app, o ambos (implementación de `INotificationService`).

### 6. POSTCONDICIONES
- **Si aprobado:** El curso cambia a estado `PUBLICADO`, queda visible en el catálogo público para alumnos (CU-05), y el profesor puede dictarlo.
- **Si rechazado:** El curso cambia a estado `RECHAZADO`, las observaciones quedan registradas, y el profesor puede visualizar el motivo en su panel.
- **Si observaciones:** El curso permanece en `EN_REVISION` con observaciones registradas; el profesor debe corregir y reenviar (CU-06).
- En todos los casos: queda registro de auditoría en `AuditoriaCursos` (cursoId, adminId, acción, fecha, observaciones) para trazabilidad.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Consulta exitosa de listado/detalle; aprobación/rechazo/observaciones procesados correctamente. |
| `400` | Bad Request | JSON inválido; observaciones faltantes al rechazar (validación de esquema en DTO). |
| `403` | Forbidden | Usuario autenticado sin rol Administrador (policy `RequireAdminRole`). |
| `404` | Not Found | Curso no existe en la base de datos. |
| `409` | Conflict | Curso no está en estado EN_REVISION (violación RN-09). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** campo `observaciones` obligatorio al rechazar (`[Required]` en `RechazarCursoDTO` + `ModelState.IsValid`), formato JSON.
- **Verificación (Negocio, → 403/404/409):** RN-08 autorización por rol Admin (policy en controller + verificación en service); RN-09 validación de estado `EN_REVISION` antes de aprobar/rechazar (`InvalidStateException` → 409); RN-10 verificación de observaciones no vacías al rechazar (doble chequeo en service). El negocio funciona como *defensa en profundidad*.

### Matriz de trazabilidad CU-04 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (aprobar) | `200 OK` | `AprobarAsync_WithValidCurso_ReturnsUpdatedCurso` | `AprobarCurso_WithValidId_Returns200OkAndPublicado` |
| Flujo A1 (rechazar) | `200 OK` | `RechazarAsync_WithValidObservaciones_ReturnsUpdatedCurso` | `RechazarCurso_WithValidObservaciones_Returns200OkAndRechazado` |
| Flujo A2 (observaciones) | `200 OK` | `AgregarObservacionesAsync_WithValidData_ReturnsSuccess` | `AgregarObservaciones_WithValidData_Returns200Ok` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding) | `AprobarCurso_WithInvalidJson_Returns400BadRequest` |
| 3a. Sin pendientes | `200 OK` (lista vacía) | `ObtenerPendientesAsync_WhenNone_ReturnsEmptyList` | `ListarPendientes_WhenNone_Returns200OkEmptyArray` |
| 5a. Estado inválido | `409 Conflict` | `ObtenerPorIdAsync_WhenNotEnRevision_ThrowsInvalidStateException` | `ObtenerCursoParaAuditoria_WhenNotEnRevision_Returns409Conflict` |
| 8a. Curso no encontrado | `404 Not Found` | `ObtenerPorIdAsync_WhenNotExists_ThrowsNotFoundException` | `ObtenerCursoParaAuditoria_WhenNotExists_Returns404NotFound` |
| 9a. Sin rol Admin | `403 Forbidden` | — (policy en middleware) | `AprobarCurso_WithoutAdminRole_Returns403Forbidden` |
| A1 sin observaciones | `400 Bad Request` | — (validación `[Required]`) | `RechazarCurso_WithoutObservaciones_Returns400BadRequest` |
| 11a. Error persistencia | `500 Internal Server Error` | `AprobarAsync_WhenDbFails_ThrowsException` | `AprobarCurso_WhenDbFails_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. Los tests se ejecutan con `dotnet test CursosOnline.slnx`.
