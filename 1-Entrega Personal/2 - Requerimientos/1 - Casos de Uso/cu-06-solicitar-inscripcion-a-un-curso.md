# Caso de Uso: Solicitar Inscripción a un Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP y los nombres de tests constituyen una propuesta de trazabilidad
> técnica; el documento fuente define actualmente un prototipo frontend académico.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-06 |
| **Nombre** | Solicitar Inscripción a un Curso |
| **Actor Principal** | Alumno |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Alumno → solicitar acceso a un curso publicado; Administrador → recibir solicitudes válidas; Profesor → conservar inscripciones consistentes en sus cursos |
| **Disparador (Trigger)** | El Alumno selecciona "Inscribirse" en el detalle de un curso PUBLICADO. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-08 a RN-10 y RN-12 |
| **Referencias funcionales** | RF-19 a RF-22; RN-08 a RN-10 y RN-12. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Alumno
- **Secundarios:** Administrador

---

### 1. BREVE DESCRIPCIÓN

Permite que un Alumno solicite su inscripción a un curso publicado. La solicitud requiere resolución administrativa antes de otorgar acceso.

### 2. PRECONDICIONES

- El Alumno debe haber iniciado sesión.
- El curso debe estar PUBLICADO.
- No debe existir una inscripción PENDIENTE o APROBADA del Alumno para ese curso.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)

1. El Alumno accede al catálogo de cursos.
2. El sistema muestra los cursos PUBLICADOS. **Reglas aplicables:** **RN-08**.
3. El Alumno selecciona un curso.
4. El sistema muestra el detalle del curso.
5. El Alumno selecciona "Inscribirse".
6. El sistema verifica que no exista una inscripción duplicada y que el curso admita inscripciones. **Reglas aplicables:** **RN-09**, **RN-12**.
7. El sistema registra la solicitud en estado PENDIENTE. **Reglas aplicables:** **RN-10**.
8. El sistema informa que la solicitud fue enviada y queda a la espera de revisión.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **6a. Inscripción duplicada — A1 (HTTP 409 Conflict):**
  1. El sistema detecta una solicitud PENDIENTE o inscripción APROBADA para el mismo curso. **Reglas aplicables:** **RN-09**.
  2. El sistema informa que no puede generarse una nueva solicitud.

* **5a. El Alumno decide no continuar — A2 (HTTP 200 OK):**
  1. El Alumno vuelve al catálogo o al detalle sin confirmar la inscripción.
  2. El sistema no registra ninguna solicitud.

* **6b. El curso no admite nuevas inscripciones — A3 (HTTP 409 Conflict):**
  1. El sistema detecta que el curso ya no está PUBLICADO, por ejemplo porque pasó a PAUSADO o FINALIZADO. **Reglas aplicables:** **RN-08**, **RN-12**.
  2. Informa que no admite nuevas solicitudes y no registra la inscripción.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- La solicitud queda registrada en estado PENDIENTE.
- El Alumno puede consultar el estado de la solicitud mediante CU-12.
- Todavía no obtiene acceso al curso hasta su aprobación.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | resultado satisfactorio de Solicitar Inscripción a un Curso. |
| `409` | Conflict | A1: Inscripción duplicada; A3: El curso no admite nuevas inscripciones. |
| `200` | OK | A2: El Alumno decide no continuar. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Matriz de trazabilidad CU-06 → Test

| Paso del CU | Excepción / Código | Test unitario propuesto (Negocio) | Test de integración propuesto (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Flujo principal | `201 Created` | `CU06_Step01_WhenValidState_ContinuesUseCase` | `CU06_Step01_WhenValidRequest_Returns201Created` |
| Paso 2. Flujo principal | `201 Created` | `CU06_Step02_WhenValidState_ContinuesUseCase` | `CU06_Step02_WhenValidRequest_Returns201Created` |
| Paso 3. Flujo principal | `201 Created` | `CU06_Step03_WhenValidState_ContinuesUseCase` | `CU06_Step03_WhenValidRequest_Returns201Created` |
| Paso 4. Flujo principal | `201 Created` | `CU06_Step04_WhenValidState_ContinuesUseCase` | `CU06_Step04_WhenValidRequest_Returns201Created` |
| Paso 5. Flujo principal | `201 Created` | `CU06_Step05_WhenValidState_ContinuesUseCase` | `CU06_Step05_WhenValidRequest_Returns201Created` |
| Paso 6. Flujo principal | `201 Created` | `CU06_Step06_WhenValidState_ContinuesUseCase` | `CU06_Step06_WhenValidRequest_Returns201Created` |
| Paso 7. Flujo principal | `201 Created` | `CU06_Step07_WhenValidState_ContinuesUseCase` | `CU06_Step07_WhenValidRequest_Returns201Created` |
| Paso 8. Flujo principal | `201 Created` | `CU06_Step08_WhenValidState_ContinuesUseCase` | `CU06_Step08_WhenValidRequest_Returns201Created` |
| 6a. Inscripción duplicada (A1) | `409 Conflict` | `CU06_Alt01_WhenConditionOccurs_HandlesExpectedBranch` | `CU06_Alt01_WhenConditionOccurs_Returns409Conflict` |
| 5a. El Alumno decide no continuar (A2) | `200 OK` | `CU06_Alt02_WhenConditionOccurs_HandlesExpectedBranch` | `CU06_Alt02_WhenConditionOccurs_Returns200OK` |
| 6b. El curso no admite nuevas inscripciones (A3) | `409 Conflict` | `CU06_Alt03_WhenConditionOccurs_HandlesExpectedBranch` | `CU06_Alt03_WhenConditionOccurs_Returns409Conflict` |

> Los nombres de tests documentan el contrato esperado y deberán vincularse con la
> suite automatizada cuando exista una implementación backend.
