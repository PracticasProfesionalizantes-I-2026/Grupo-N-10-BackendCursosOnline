# Caso de Uso: Consultar Progreso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP y los nombres de tests constituyen una propuesta de trazabilidad
> técnica; el documento fuente define actualmente un prototipo frontend académico.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-08 |
| **Nombre** | Consultar Progreso |
| **Actor Principal** | Alumno / Profesor / Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Alumno → consultar su propio avance; Profesor → seguir alumnos de cursos propios; Administrador → consultar el progreso según sus permisos |
| **Disparador (Trigger)** | El usuario accede a la sección de progreso correspondiente. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-13 y RN-14 |
| **Referencias funcionales** | RF-28; RN-13 y RN-14; permiso del Administrador para ver progreso. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Alumno / Profesor / Administrador

---

### 1. BREVE DESCRIPCIÓN

Permite consultar el avance de un Alumno en un curso mediante porcentaje y estado de progreso.

### 2. PRECONDICIONES

- El usuario debe haber iniciado sesión. El Alumno consulta solo su propio progreso; el Administrador puede consultar el progreso desde su gestión general.
- El Alumno debe poseer una inscripción APROBADA para consultar su progreso.
- El Profesor solo puede consultar alumnos de cursos propios.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Alumno accede a sus cursos, el Profesor al seguimiento de alumnos de sus cursos y el Administrador a la consulta de progreso desde su gestión general.
2. El usuario selecciona el curso o Alumno correspondiente. El sistema verifica que el Alumno consulte su propio progreso y que el Profesor consulte únicamente alumnos de cursos propios. **Reglas aplicables:** **RN-14**.
3. El sistema consulta el avance registrado al realizar módulos o actividades del curso (CU-16); esta consulta no modifica el progreso.
4. El sistema muestra el porcentaje de avance.
5. El sistema muestra el estado asociado: 0 % NO INICIADO, 1-99 % EN PROGRESO o 100 % COMPLETADO. **Reglas aplicables:** **RN-13**.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Sin inscripción aprobada — A1 (HTTP 409 Conflict):**
  1. El sistema detecta que el Alumno no posee una inscripción APROBADA. **Reglas aplicables:** **RN-11**.
  2. El sistema no muestra progreso del curso.

* **3b. Profesor intenta consultar un curso ajeno — A2 (HTTP 403 Forbidden):**
  1. El sistema detecta que el curso no pertenece al Profesor. **Reglas aplicables:** **RN-14**.
  2. El sistema restringe la consulta.

* **2a. Alumno intenta consultar progreso ajeno — A3 (HTTP 403 Forbidden):**
  1. El sistema detecta que el progreso solicitado corresponde a otro Alumno.
  2. Restringe la consulta y mantiene disponible únicamente el progreso propio.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- El sistema muestra el porcentaje y el estado NO INICIADO, EN PROGRESO o COMPLETADO según corresponda.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Consultar Progreso. |
| `409` | Conflict | A1: Sin inscripción aprobada. |
| `403` | Forbidden | A2: Profesor intenta consultar un curso ajeno; A3: Alumno intenta consultar progreso ajeno. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Matriz de trazabilidad CU-08 → Test

| Paso del CU | Excepción / Código | Test unitario propuesto (Negocio) | Test de integración propuesto (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Flujo principal | `200 OK` | `CU08_Step01_WhenValidState_ContinuesUseCase` | `CU08_Step01_WhenValidRequest_Returns200OK` |
| Paso 2. Flujo principal | `200 OK` | `CU08_Step02_WhenValidState_ContinuesUseCase` | `CU08_Step02_WhenValidRequest_Returns200OK` |
| Paso 3. Flujo principal | `200 OK` | `CU08_Step03_WhenValidState_ContinuesUseCase` | `CU08_Step03_WhenValidRequest_Returns200OK` |
| Paso 4. Flujo principal | `200 OK` | `CU08_Step04_WhenValidState_ContinuesUseCase` | `CU08_Step04_WhenValidRequest_Returns200OK` |
| Paso 5. Flujo principal | `200 OK` | `CU08_Step05_WhenValidState_ContinuesUseCase` | `CU08_Step05_WhenValidRequest_Returns200OK` |
| 3a. Sin inscripción aprobada (A1) | `409 Conflict` | `CU08_Alt01_WhenConditionOccurs_HandlesExpectedBranch` | `CU08_Alt01_WhenConditionOccurs_Returns409Conflict` |
| 3b. Profesor intenta consultar un curso ajeno (A2) | `403 Forbidden` | `CU08_Alt02_WhenConditionOccurs_HandlesExpectedBranch` | `CU08_Alt02_WhenConditionOccurs_Returns403Forbidden` |
| 2a. Alumno intenta consultar progreso ajeno (A3) | `403 Forbidden` | `CU08_Alt03_WhenConditionOccurs_HandlesExpectedBranch` | `CU08_Alt03_WhenConditionOccurs_Returns403Forbidden` |

> Los nombres de tests documentan el contrato esperado y deberán vincularse con la
> suite automatizada cuando exista una implementación backend.
