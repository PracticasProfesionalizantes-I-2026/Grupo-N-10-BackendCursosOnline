# Caso de Uso: Consultar Cursos Publicados

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP y los nombres de tests constituyen una propuesta de trazabilidad
> técnica; el documento fuente define actualmente un prototipo frontend académico.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-11 |
| **Nombre** | Consultar Cursos Publicados |
| **Actor Principal** | Alumno / Profesor / Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Alumno, Profesor y Administrador → consultar el catálogo publicado; responsables de cursos → mostrar información vigente y aprobada |
| **Disparador (Trigger)** | El usuario selecciona la consulta de cursos publicados desde su panel. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | No se identifica una RN específica; aplican RF-19, RF-30 y la matriz de permisos |
| **Referencias funcionales** | RF-19 y RF-30; matriz de consulta de cursos publicados. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Alumno / Profesor / Administrador

---

### 1. BREVE DESCRIPCIÓN

Permite consultar el listado y el detalle de cursos PUBLICADOS sin tener que iniciar una solicitud de inscripción.

### 2. PRECONDICIONES

- El usuario debe haber iniciado sesión como Alumno, Profesor o Administrador.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El usuario accede a la consulta de cursos publicados.
2. El sistema muestra el listado de cursos en estado PUBLICADO.
3. El usuario selecciona un curso.
4. El sistema muestra su detalle y la información documentada del curso, incluidos sus módulos y duración total.
5. El usuario revisa la información y puede volver al listado. Si es Alumno y desea inscribirse, inicia CU-06.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. No hay cursos PUBLICADOS — A1 (HTTP 200 OK):**
  1. El sistema informa que no hay cursos disponibles para consultar.
  2. El usuario puede volver a su panel.

* **4a. El curso dejó de estar PUBLICADO — A2 (HTTP 409 Conflict):**
  1. El sistema informa que el curso seleccionado ya no está disponible en el catálogo.
  2. Actualiza la consulta sin permitir una nueva solicitud de inscripción a ese curso.

* **5a. Consulta como Profesor o Administrador — A3 (HTTP 200 OK):**
  1. El usuario puede consultar el curso, pero no dispone de la acción de solicitar inscripción.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- El usuario conoce la información de los cursos PUBLICADOS disponibles.
- La consulta no crea una inscripción, no otorga acceso a contenido reservado a inscripciones APROBADAS y no modifica cursos.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Consultar Cursos Publicados; A1: No hay cursos PUBLICADOS; A3: Consulta como Profesor o Administrador. |
| `409` | Conflict | A2: El curso dejó de estar PUBLICADO. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Matriz de trazabilidad CU-11 → Test

| Paso del CU | Excepción / Código | Test unitario propuesto (Negocio) | Test de integración propuesto (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Flujo principal | `200 OK` | `CU11_Step01_WhenValidState_ContinuesUseCase` | `CU11_Step01_WhenValidRequest_Returns200OK` |
| Paso 2. Flujo principal | `200 OK` | `CU11_Step02_WhenValidState_ContinuesUseCase` | `CU11_Step02_WhenValidRequest_Returns200OK` |
| Paso 3. Flujo principal | `200 OK` | `CU11_Step03_WhenValidState_ContinuesUseCase` | `CU11_Step03_WhenValidRequest_Returns200OK` |
| Paso 4. Flujo principal | `200 OK` | `CU11_Step04_WhenValidState_ContinuesUseCase` | `CU11_Step04_WhenValidRequest_Returns200OK` |
| Paso 5. Flujo principal | `200 OK` | `CU11_Step05_WhenValidState_ContinuesUseCase` | `CU11_Step05_WhenValidRequest_Returns200OK` |
| 2a. No hay cursos PUBLICADOS (A1) | `200 OK` | `CU11_Alt01_WhenConditionOccurs_HandlesExpectedBranch` | `CU11_Alt01_WhenConditionOccurs_Returns200OK` |
| 4a. El curso dejó de estar PUBLICADO (A2) | `409 Conflict` | `CU11_Alt02_WhenConditionOccurs_HandlesExpectedBranch` | `CU11_Alt02_WhenConditionOccurs_Returns409Conflict` |
| 5a. Consulta como Profesor o Administrador (A3) | `200 OK` | `CU11_Alt03_WhenConditionOccurs_HandlesExpectedBranch` | `CU11_Alt03_WhenConditionOccurs_Returns200OK` |

> Los nombres de tests documentan el contrato esperado y deberán vincularse con la
> suite automatizada cuando exista una implementación backend.
