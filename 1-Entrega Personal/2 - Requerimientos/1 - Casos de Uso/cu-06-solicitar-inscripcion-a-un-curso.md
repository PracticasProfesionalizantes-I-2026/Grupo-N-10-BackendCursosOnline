# Caso de Uso: Solicitar Inscripción a un Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y adaptada a la
> sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-06 |
| **Nombre** | Solicitar Inscripción a un Curso |
| **Actor Principal** | Alumno |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Alumno → solicitar acceso a un curso; Administrador → revisar solicitudes válidas; Profesor → recibir alumnos mediante el proceso definido |
| **Disparador (Trigger)** | El Alumno selecciona “Inscribirse” en un curso PUBLICADO |
| **Prioridad / Frecuencia** | Alta; frecuencia alta |
| **Reglas de negocio relacionadas** | RN-17 (el curso debe estar PUBLICADO y admitir inscripciones); RN-18 (no se duplican inscripciones PENDIENTES o APROBADAS); RN-19 (la solicitud inicia PENDIENTE y no concede acceso) |

---

### 1. BREVE DESCRIPCIÓN

Permite que un Alumno solicite su inscripción a un curso publicado. La solicitud
queda pendiente de resolución administrativa antes de otorgar acceso.

### 2. PRECONDICIONES

- El Alumno debe haber iniciado sesión con un Token JWT válido.
- El curso debe existir, estar PUBLICADO y admitir inscripciones, conforme a
  **RN-17**.
- No debe existir una inscripción PENDIENTE o APROBADA del Alumno para el mismo
  curso, conforme a **RN-18**.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)

1. El actor consulta el catálogo mediante `GET /api/cursos?estado=PUBLICADO`.
2. El Sistema devuelve los cursos publicados que pueden consultarse.
3. El actor selecciona un curso y consulta su detalle mediante
   `GET /api/cursos/{cursoId}`.
4. El Sistema devuelve la información del curso y su disponibilidad.
5. El actor confirma la inscripción mediante `POST /api/inscripciones` con el
   identificador del curso.
6. La **Capa de Negocio** verifica la disponibilidad del curso y la inexistencia
   de una inscripción duplicada, aplicando **RN-17** y **RN-18**.
7. La **Capa de Persistencia** registra la solicitud con estado **PENDIENTE**, sin
   conceder acceso al curso, conforme a **RN-19**.
8. El Sistema devuelve **201 Created** e informa que la solicitud queda a la
   espera de revisión.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **5a. El Alumno decide no continuar (sin petición HTTP):**
  1. Si antes del Paso 5 el Alumno vuelve al catálogo o al detalle, no envía la
     solicitud.
  2. El Sistema no registra ninguna inscripción. Fin del caso de uso.

* **6a. Inscripción duplicada (HTTP 409 Conflict):**
  1. Si en el Paso 6 ya existe una inscripción PENDIENTE o APROBADA para el mismo
     Alumno y curso, se incumple **RN-18**.
  2. La Capa de Negocio rechaza la creación.
  3. El Sistema devuelve **409 Conflict** e informa que no puede generarse una
     nueva solicitud. Fin del caso de uso.

* **6b. Curso no disponible para inscripción (HTTP 409 Conflict):**
  1. Si en el Paso 6 el curso dejó de estar PUBLICADO o no admite nuevas
     inscripciones, se incumple **RN-17**.
  2. La Capa de Negocio rechaza la solicitud.
  3. El Sistema devuelve **409 Conflict**. Fin del caso de uso.

* **1a. Usuario no autenticado (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 no se presenta un Token JWT válido, el Sistema devuelve
     **401 Unauthorized**. Fin del caso de uso.

* **5b. Rol no autorizado (HTTP 403 Forbidden):**
  1. Si en el Paso 5 la cuenta autenticada no posee rol Alumno, la Capa de Negocio
     impide solicitar la inscripción.
  2. El Sistema devuelve **403 Forbidden**. Fin del caso de uso.

* **3a. Curso inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 3 el curso indicado no existe, la Capa de Negocio no encuentra
     el recurso.
  2. El Sistema devuelve **404 Not Found**. Fin del caso de uso.

### 5. SUB-VARIACIONES

1. El Alumno puede iniciar la solicitud desde el catálogo o desde el detalle del
   curso; ambos recorridos envían el mismo identificador y producen el mismo
   estado PENDIENTE.

### 6. POSTCONDICIONES

- La solicitud queda registrada con estado PENDIENTE.
- El Alumno puede consultar el estado de la solicitud.
- El Alumno todavía no obtiene acceso al curso, conforme a **RN-19**.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | El catálogo o el detalle del curso fue consultado correctamente. |
| `201` | Created | La solicitud de inscripción fue creada en estado PENDIENTE. |
| `401` | Unauthorized | No existe una autenticación válida. |
| `403` | Forbidden | La cuenta autenticada no posee rol Alumno. |
| `404` | Not Found | El curso indicado no existe. |
| `409` | Conflict | La inscripción está duplicada o el curso no admite inscripciones. |

### Matriz de trazabilidad CU-06 → Test

| Paso del CU | Excepción / Código | Test unitario (Negocio) | Test de integración (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Consultar catálogo | `200 OK` | `GetPublishedCoursesAsync_ReturnsPublishedCourses` | `GetPublishedCourses_AsStudent_Returns200OK` |
| Paso 2. Mostrar publicados | `200 OK` | `GetPublishedCoursesAsync_ExcludesUnavailableCourses` | `GetPublishedCourses_ReturnsOnlyPublishedCourses` |
| Paso 3. Consultar curso | `200 OK` | `GetCourseAsync_WithExistingId_ReturnsCourse` | `GetCourse_WithExistingId_Returns200OK` |
| Paso 4. Mostrar disponibilidad | `200 OK` | `GetCourseAsync_ReturnsEnrollmentAvailability` | `GetCourse_ReturnsAvailabilityData` |
| Paso 5. Confirmar inscripción | `201 Created` | — (entrada HTTP) | `CreateEnrollment_WithValidRequest_AcceptsCourseId` |
| Paso 6. Verificar reglas | `201 Created` | `CreateEnrollmentAsync_WhenAvailableAndUnique_AllowsRequest` | `CreateEnrollment_WhenAvailableAndUnique_Returns201Created` |
| Paso 7. Persistir pendiente | `201 Created` | `CreateEnrollmentAsync_PersistsPendingWithoutAccess` | `CreateEnrollment_PersistsPendingStatus` |
| Paso 8. Responder creación | `201 Created` | `CreateEnrollmentAsync_WithValidData_ReturnsPendingEnrollment` | `CreateEnrollment_WithValidData_Returns201Created` |
| 5a. No continuar | Sin petición | `EnrollmentRequest_WhenCancelled_DoesNotCreateEnrollment` | — (no se realiza llamada HTTP) |
| 6a. Inscripción duplicada | `409 Conflict` | `CreateEnrollmentAsync_WhenDuplicate_ThrowsEnrollmentConflictException` | `CreateEnrollment_WhenDuplicate_Returns409Conflict` |
| 6b. Curso no disponible | `409 Conflict` | `CreateEnrollmentAsync_WhenCourseUnavailable_ThrowsCourseStateException` | `CreateEnrollment_WhenCourseUnavailable_Returns409Conflict` |
| 1a. Sin autenticación | `401 Unauthorized` | — (autenticación HTTP) | `GetPublishedCourses_WithoutToken_Returns401Unauthorized` |
| 5b. Rol no autorizado | `403 Forbidden` | `CreateEnrollmentAsync_AsNonStudent_ThrowsForbiddenException` | `CreateEnrollment_AsTeacher_Returns403Forbidden` |
| 3a. Curso inexistente | `404 Not Found` | `GetCourseAsync_WithUnknownId_ThrowsCourseNotFoundException` | `GetCourse_WithUnknownId_Returns404NotFound` |

> Los nombres de tests establecen el contrato de trazabilidad del caso de uso y
> deberán coincidir con la suite automatizada cuando se implemente.
