# Caso de Uso: Cancelar Inscripción

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP describen la API vigente. La sección de evidencia vincula las pruebas
> ejecutables y distingue las diferencias del prototipo local.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-13 |
| **Nombre** | Cancelar Inscripción |
| **Actor Principal** | Alumno / Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Alumno y Administrador → cancelar inscripciones autorizadas; Profesor → mantener participantes activos consistentes; sistema → retirar el acceso cancelado |
| **Disparador (Trigger)** | El Alumno selecciona cancelar una inscripción propia o el Administrador selecciona cancelar una inscripción desde su gestión. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-11 |
| **Referencias funcionales** | RF-25; RN-11; permisos de cancelación y transición APROBADA -> CANCELADA. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Alumno / Administrador

---

### 1. BREVE DESCRIPCIÓN

Permite cancelar una inscripción APROBADA. El Alumno solo puede cancelar una inscripción propia y el Administrador puede hacerlo desde la gestión de inscripciones.

### 2. PRECONDICIONES

- El usuario debe haber iniciado sesión como Alumno o Administrador.
- La inscripción seleccionada debe estar APROBADA. Si actúa el Alumno, debe pertenecerle.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Alumno accede a sus inscripciones o el Administrador a la gestión de inscripciones.
2. El sistema muestra las inscripciones que el actor puede gestionar y su estado.
3. El actor selecciona una inscripción APROBADA y solicita cancelarla. **Reglas aplicables:** **RN-11**.
4. El sistema verifica el rol, la propiedad cuando actúa el Alumno y el estado APROBADA. **Reglas aplicables:** **RN-11**.
5. El sistema informa que la cancelación retira el acceso activo y solicita confirmación.
6. El actor confirma la cancelación.
7. El sistema cambia la inscripción a CANCELADA, retira el acceso activo y muestra la confirmación. **Reglas aplicables:** **RN-11**. La cancelación conserva el historial de esa inscripción. Una futura inscripción empieza con avance propio al aprobarse. Sin confirmación no se envía una mutación HTTP.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **6a. El actor decide no cancelar — A1 (Sin solicitud HTTP):**
  1. El actor abandona la confirmación.
  2. El sistema mantiene la inscripción APROBADA y el acceso activo.

* **4a. Inscripción que no está APROBADA — A2 (HTTP 409 Conflict):**
  1. El sistema muestra el estado actual y no ejecuta la cancelación. **Reglas aplicables:** **RN-11**.
  2. Este flujo no cambia una solicitud PENDIENTE ni una inscripción RECHAZADA o ya CANCELADA.

* **4b. Rol sin permiso o inscripción ajena al Alumno — A3 (HTTP 403 Forbidden):**
  1. El sistema restringe la operación y no modifica la inscripción.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- La inscripción cambia de APROBADA a CANCELADA y se retira el acceso activo al curso.
- Si la operación no se confirma o no cumple las precondiciones, conserva su estado.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Cancelar Inscripción. |
| `409` | Conflict | A2: Inscripción que no está APROBADA. |
| `403` | Forbidden | A3: Rol sin permiso o inscripción ajena al Alumno. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Evidencia ejecutable de CU-13

| Fuente | Recorrido o contrato |
|---|---|
| API | `POST /api/enrollments/{id}/cancel` |
| Pruebas automatizadas | EnrollmentAndProgressServiceTests; EnrollmentConcurrencyRegressionTests; demo.test.mjs |
| Bruno | Enrollments/07 |

200 cancela y conserva historial; 403 por rol/titularidad y 409 si no estaba aprobada.
