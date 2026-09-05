# Caso de Uso: Auditar Solicitud de Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP y los nombres de tests constituyen una propuesta de trazabilidad
> técnica; el documento fuente define actualmente un prototipo frontend académico.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-04 |
| **Nombre** | Auditar Solicitud de Curso |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Administrador → resolver auditorías con estados consistentes; autor del curso → conocer la decisión y las observaciones; Alumno → acceder únicamente a cursos publicados |
| **Disparador (Trigger)** | El Administrador selecciona una solicitud de auditoría de un curso en estado EN REVISIÓN desde su panel. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-06 y RN-07 |
| **Referencias funcionales** | RF-14 y RF-15; RN-06 y RN-07; estados de cursos. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Administrador
- **Secundarios:** Profesor o Administrador que preparó el curso o sus modificaciones.

---

### 1. BREVE DESCRIPCIÓN

Permite que un Administrador revise una solicitud de creación o modificación de curso y determine su resultado.

### 2. PRECONDICIONES

- El Administrador debe haber iniciado sesión.
- Debe existir una solicitud de auditoría cuyo curso esté EN REVISIÓN.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Administrador accede a la sección de auditorías.
2. El sistema muestra las solicitudes de auditoría cuyos cursos están EN REVISIÓN. **Reglas aplicables:** **RN-06**.
3. El Administrador selecciona una solicitud.
4. El sistema muestra la información del curso, sus módulos y, si corresponde, los cambios realizados.
5. El Administrador revisa la información.
6. El Administrador selecciona "Aprobar".
7. El sistema registra la decisión y cambia el curso a PUBLICADO. **Reglas aplicables:** **RN-06**.
8. El sistema informa que la auditoría fue aprobada.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **6a. Rechazar solicitud — A1 (HTTP 200 OK):**
  1. El Administrador selecciona "Rechazar".
  2. El sistema registra la decisión.
  3. El curso queda RECHAZADO y no se publica.

* **6b. Solicitar cambios — A2 (HTTP 200 OK):**
  1. El Administrador detecta información que debe corregirse. **Reglas aplicables:** **RN-07**.
  2. Selecciona "Solicitar cambios" e indica la observación correspondiente.
  3. El sistema cambia el curso a CAMBIOS SOLICITADOS.
  4. El usuario que preparó el curso puede consultar la observación, corregirlo y volver a enviarlo mediante CU-05, respetando los permisos de su rol.

* **6c. La solicitud ya no corresponde a un curso EN REVISIÓN — A3 (HTTP 409 Conflict):**
  1. Antes de registrar la decisión, el sistema detecta que el curso ya no está EN REVISIÓN.
  2. El sistema muestra el estado actual y no aplica una nueva decisión sobre esa solicitud.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- La auditoría queda resuelta.
- El curso cambia a PUBLICADO, CAMBIOS SOLICITADOS o RECHAZADO según la decisión.
- El usuario que preparó el curso o sus modificaciones puede visualizar el estado resultante en sus solicitudes de auditoría.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Auditar Solicitud de Curso; A1: Rechazar solicitud; A2: Solicitar cambios. |
| `409` | Conflict | A3: La solicitud ya no corresponde a un curso EN REVISIÓN. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Matriz de trazabilidad CU-04 → Test

| Paso del CU | Excepción / Código | Test unitario propuesto (Negocio) | Test de integración propuesto (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Flujo principal | `200 OK` | `CU04_Step01_WhenValidState_ContinuesUseCase` | `CU04_Step01_WhenValidRequest_Returns200OK` |
| Paso 2. Flujo principal | `200 OK` | `CU04_Step02_WhenValidState_ContinuesUseCase` | `CU04_Step02_WhenValidRequest_Returns200OK` |
| Paso 3. Flujo principal | `200 OK` | `CU04_Step03_WhenValidState_ContinuesUseCase` | `CU04_Step03_WhenValidRequest_Returns200OK` |
| Paso 4. Flujo principal | `200 OK` | `CU04_Step04_WhenValidState_ContinuesUseCase` | `CU04_Step04_WhenValidRequest_Returns200OK` |
| Paso 5. Flujo principal | `200 OK` | `CU04_Step05_WhenValidState_ContinuesUseCase` | `CU04_Step05_WhenValidRequest_Returns200OK` |
| Paso 6. Flujo principal | `200 OK` | `CU04_Step06_WhenValidState_ContinuesUseCase` | `CU04_Step06_WhenValidRequest_Returns200OK` |
| Paso 7. Flujo principal | `200 OK` | `CU04_Step07_WhenValidState_ContinuesUseCase` | `CU04_Step07_WhenValidRequest_Returns200OK` |
| Paso 8. Flujo principal | `200 OK` | `CU04_Step08_WhenValidState_ContinuesUseCase` | `CU04_Step08_WhenValidRequest_Returns200OK` |
| 6a. Rechazar solicitud (A1) | `200 OK` | `CU04_Alt01_WhenConditionOccurs_HandlesExpectedBranch` | `CU04_Alt01_WhenConditionOccurs_Returns200OK` |
| 6b. Solicitar cambios (A2) | `200 OK` | `CU04_Alt02_WhenConditionOccurs_HandlesExpectedBranch` | `CU04_Alt02_WhenConditionOccurs_Returns200OK` |
| 6c. La solicitud ya no corresponde a un curso EN REVISIÓN (A3) | `409 Conflict` | `CU04_Alt03_WhenConditionOccurs_HandlesExpectedBranch` | `CU04_Alt03_WhenConditionOccurs_Returns409Conflict` |

> Los nombres de tests documentan el contrato esperado y deberán vincularse con la
> suite automatizada cuando exista una implementación backend.
