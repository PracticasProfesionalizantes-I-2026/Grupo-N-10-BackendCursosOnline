# Caso de Uso: Consultar Progreso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y adaptada a la
> sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-08 |
| **Nombre** | Consultar Progreso |
| **Actor Principal** | Alumno / Profesor |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Alumno → conocer su propio avance; Profesor → seguir a los alumnos de sus cursos; Administración académica → conservar una medición coherente |
| **Disparador (Trigger)** | El usuario accede a la sección de progreso correspondiente |
| **Prioridad / Frecuencia** | Alta; frecuencia muy alta |
| **Reglas de negocio relacionadas** | RN-23 (el Alumno requiere inscripción APROBADA); RN-24 (el Profesor solo consulta cursos propios); RN-25 (0 % NO INICIADO, 1-99 % EN PROGRESO y 100 % COMPLETADO) |

---

### 1. BREVE DESCRIPCIÓN

Permite consultar el avance de un Alumno en un curso mediante un porcentaje y un
estado derivado de los módulos o actividades completadas.

### 2. PRECONDICIONES

- El usuario debe haber iniciado sesión con un Token JWT válido.
- El Alumno debe poseer una inscripción APROBADA para el curso, conforme a
  **RN-23**.
- El Profesor debe ser propietario del curso consultado, conforme a **RN-24**.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El actor envía `GET /api/progresos?cursoId={cursoId}&alumnoId={alumnoId}`;
   cuando consulta su propio avance, el Alumno se identifica por la cuenta
   autenticada.
2. La **Capa de Presentación** valida el formato de los identificadores y los
   parámetros.
3. La **Capa de Negocio** verifica, según el rol, la inscripción APROBADA del
   Alumno o la propiedad del curso por el Profesor, aplicando **RN-23** o
   **RN-24**.
4. La **Capa de Persistencia** obtiene los módulos o actividades completadas y el
   total correspondiente.
5. La **Capa de Negocio** calcula el porcentaje y determina el estado mediante
   **RN-25**.
6. El Sistema devuelve **200 OK** con el porcentaje y el estado NO INICIADO, EN
   PROGRESO o COMPLETADO.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Parámetros inválidos (HTTP 400 Bad Request):**
  1. Si en el Paso 2 los identificadores están ausentes o tienen formato
     inválido, la Capa de Presentación rechaza la consulta.
  2. El Sistema devuelve **400 Bad Request**. Fin del caso de uso.

* **3a. Alumno sin inscripción aprobada (HTTP 403 Forbidden):**
  1. Si en el Paso 3 el Alumno no posee una inscripción APROBADA, se incumple
     **RN-23**.
  2. La Capa de Negocio impide consultar el progreso.
  3. El Sistema devuelve **403 Forbidden**. Fin del caso de uso.

* **3b. Profesor consulta un curso ajeno (HTTP 403 Forbidden):**
  1. Si en el Paso 3 el curso no pertenece al Profesor, se incumple **RN-24**.
  2. La Capa de Negocio restringe la consulta.
  3. El Sistema devuelve **403 Forbidden**. Fin del caso de uso.

* **4a. Curso o Alumno inexistente (HTTP 404 Not Found):**
  1. Si en el Paso 4 no existe alguno de los recursos indicados, la Capa de
     Negocio no puede obtener el progreso.
  2. El Sistema devuelve **404 Not Found**. Fin del caso de uso.

* **4b. Progreso aún no iniciado (HTTP 200 OK):**
  1. Si en el Paso 4 no existen módulos o actividades completadas, el porcentaje
     calculado es 0 %.
  2. La Capa de Negocio aplica **RN-25** y asigna el estado NO INICIADO.
  3. El Sistema devuelve **200 OK** con el progreso inicial. Fin del caso de uso.

* **1a. Usuario no autenticado (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 no se presenta un Token JWT válido, el Sistema devuelve
     **401 Unauthorized**. Fin del caso de uso.

### 5. SUB-VARIACIONES

1. El Alumno consulta su propio progreso sin seleccionar otro usuario.
2. El Profesor selecciona un Alumno dentro de uno de sus cursos y consulta el
   mismo modelo de porcentaje y estado.

### 6. POSTCONDICIONES

- El Sistema muestra el porcentaje de avance calculado.
- El Sistema muestra el estado definido por **RN-25**.
- La consulta no modifica el progreso ni el estado persistente del curso.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | El progreso fue calculado y devuelto, incluso si todavía es 0 %. |
| `400` | Bad Request | Los parámetros de consulta son incompletos o inválidos. |
| `401` | Unauthorized | No existe una autenticación válida. |
| `403` | Forbidden | El actor no posee acceso al progreso solicitado. |
| `404` | Not Found | El curso o el Alumno indicado no existe. |

### Matriz de trazabilidad CU-08 → Test

| Paso del CU | Excepción / Código | Test unitario (Negocio) | Test de integración (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Solicitar progreso | `200 OK` | — (entrada HTTP) | `GetProgress_WithValidParameters_AcceptsRequest` |
| Paso 2. Validar parámetros | `200 OK` | `GetProgressAsync_WithValidIds_ContinuesQuery` | `GetProgress_WithValidIds_Returns200OK` |
| Paso 3. Verificar acceso | `200 OK` | `GetProgressAsync_WithAuthorizedActor_AllowsQuery` | `GetProgress_WithAuthorizedActor_Returns200OK` |
| Paso 4. Obtener avances | `200 OK` | `GetProgressAsync_LoadsCompletedAndTotalItems` | `GetProgress_ReturnsPersistedCompletionData` |
| Paso 5. Calcular estado | `200 OK` | `GetProgressAsync_CalculatesPercentageAndStatus` | `GetProgress_ReturnsExpectedPercentageAndStatus` |
| Paso 6. Responder consulta | `200 OK` | `GetProgressAsync_WithValidData_ReturnsProgress` | `GetProgress_WithValidData_Returns200OK` |
| 2a. Parámetros inválidos | `400 Bad Request` | `GetProgressAsync_WithInvalidIds_ThrowsValidationException` | `GetProgress_WithInvalidParameters_Returns400BadRequest` |
| 3a. Sin inscripción aprobada | `403 Forbidden` | `GetProgressAsync_WithoutApprovedEnrollment_ThrowsForbiddenException` | `GetProgress_WithoutApprovedEnrollment_Returns403Forbidden` |
| 3b. Curso ajeno | `403 Forbidden` | `GetProgressAsync_ForForeignCourse_ThrowsForbiddenException` | `GetProgress_AsForeignTeacher_Returns403Forbidden` |
| 4a. Recurso inexistente | `404 Not Found` | `GetProgressAsync_WithUnknownResource_ThrowsNotFoundException` | `GetProgress_WithUnknownResource_Returns404NotFound` |
| 4b. Sin progreso | `200 OK` | `GetProgressAsync_WithoutCompletedItems_ReturnsNotStarted` | `GetProgress_WithoutCompletedItems_Returns200NotStarted` |
| 1a. Sin autenticación | `401 Unauthorized` | — (autenticación HTTP) | `GetProgress_WithoutToken_Returns401Unauthorized` |

> Los nombres de tests establecen el contrato de trazabilidad del caso de uso y
> deberán coincidir con la suite automatizada cuando se implemente.
