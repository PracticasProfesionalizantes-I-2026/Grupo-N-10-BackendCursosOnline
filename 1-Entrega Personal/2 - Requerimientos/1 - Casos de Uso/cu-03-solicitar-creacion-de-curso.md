# Caso de Uso: Solicitar Creación de Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP y los nombres de tests constituyen una propuesta de trazabilidad
> técnica; el documento fuente define actualmente un prototipo frontend académico.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-03 |
| **Nombre** | Solicitar Creación de Curso |
| **Actor Principal** | Profesor / Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Profesor y Administrador → preparar cursos completos; Administrador auditor → recibir solicitudes revisables; Alumno → consultar únicamente cursos publicados |
| **Disparador (Trigger)** | El Profesor o Administrador selecciona "Crear Curso" desde su panel. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-03 a RN-06 y RN-15 |
| **Referencias funcionales** | RF-08 a RF-13; RN-03 a RN-06 y RN-15; permisos de creación, borrador y envío del Administrador. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Profesor / Administrador
- **Secundarios:** Administrador, en la auditoría posterior del CU-04.

---

### 1. BREVE DESCRIPCIÓN

Permite que un Profesor o Administrador prepare un curso, lo guarde como borrador o lo envíe a revisión administrativa una vez completada la información requerida y sus módulos.

### 2. PRECONDICIONES

- El Profesor o Administrador debe haber iniciado sesión.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)

1. El Profesor o Administrador selecciona "Crear Curso".
2. El sistema muestra el formulario del curso.
3. El Profesor o Administrador completa título, descripción, categoría, nivel, modalidad, cupo máximo, objetivos de aprendizaje y requisitos previos sugeridos.
4. El Profesor o Administrador agrega al menos un módulo. **Reglas aplicables:** **RN-04**.
5. Para cada módulo, completa nombre, descripción y duración y agrega el contenido o recursos previstos. **Reglas aplicables:** **RN-15**.
6. El sistema calcula la duración total como suma de las duraciones de los módulos. **Reglas aplicables:** **RN-05**.
7. El Profesor o Administrador selecciona "Enviar a revisión".
8. El sistema valida la información obligatoria y la existencia de al menos un módulo. **Reglas aplicables:** **RN-04**.
9. El sistema cambia el curso a EN REVISIÓN y registra la solicitud de auditoría. **Reglas aplicables:** **RN-06**.
10. El sistema informa que el curso fue enviado a revisión.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **7a. Guardar curso como borrador — A1 (HTTP 201 Created):**
  1. El Profesor o Administrador decide no enviar el curso.
  2. Selecciona "Guardar como borrador".
  3. El sistema conserva la información y deja el curso en BORRADOR.
  4. El usuario puede retomar posteriormente el BORRADOR mediante CU-05; guardarlo no publica el curso ni inicia una auditoría.

* **8a. Curso sin módulos — A2 (HTTP 400 Bad Request):**
  1. El sistema detecta que el curso no posee módulos. **Reglas aplicables:** **RN-04**.
  2. El sistema informa que debe existir al menos un módulo y mantiene el curso sin enviar.

* **8b. Información obligatoria incompleta — A3 (HTTP 400 Bad Request):**
  1. El sistema detecta información obligatoria faltante.
  2. El sistema identifica los datos pendientes.
  3. El Profesor o Administrador corrige la información antes de volver a enviar.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- Si guarda, el curso queda en BORRADOR.
- Si envía correctamente, el curso queda EN REVISIÓN y se genera una solicitud de auditoría.
- La duración total queda calculada a partir de los módulos.
- Si el Profesor crea el curso, queda identificado como su propietario. El Administrador actúa desde la gestión administrativa prevista en la matriz de permisos.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | resultado satisfactorio de Solicitar Creación de Curso; A1: Guardar curso como borrador. |
| `400` | Bad Request | A2: Curso sin módulos; A3: Información obligatoria incompleta. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Matriz de trazabilidad CU-03 → Test

| Paso del CU | Excepción / Código | Test unitario propuesto (Negocio) | Test de integración propuesto (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Flujo principal | `201 Created` | `CU03_Step01_WhenValidState_ContinuesUseCase` | `CU03_Step01_WhenValidRequest_Returns201Created` |
| Paso 2. Flujo principal | `201 Created` | `CU03_Step02_WhenValidState_ContinuesUseCase` | `CU03_Step02_WhenValidRequest_Returns201Created` |
| Paso 3. Flujo principal | `201 Created` | `CU03_Step03_WhenValidState_ContinuesUseCase` | `CU03_Step03_WhenValidRequest_Returns201Created` |
| Paso 4. Flujo principal | `201 Created` | `CU03_Step04_WhenValidState_ContinuesUseCase` | `CU03_Step04_WhenValidRequest_Returns201Created` |
| Paso 5. Flujo principal | `201 Created` | `CU03_Step05_WhenValidState_ContinuesUseCase` | `CU03_Step05_WhenValidRequest_Returns201Created` |
| Paso 6. Flujo principal | `201 Created` | `CU03_Step06_WhenValidState_ContinuesUseCase` | `CU03_Step06_WhenValidRequest_Returns201Created` |
| Paso 7. Flujo principal | `201 Created` | `CU03_Step07_WhenValidState_ContinuesUseCase` | `CU03_Step07_WhenValidRequest_Returns201Created` |
| Paso 8. Flujo principal | `201 Created` | `CU03_Step08_WhenValidState_ContinuesUseCase` | `CU03_Step08_WhenValidRequest_Returns201Created` |
| Paso 9. Flujo principal | `201 Created` | `CU03_Step09_WhenValidState_ContinuesUseCase` | `CU03_Step09_WhenValidRequest_Returns201Created` |
| Paso 10. Flujo principal | `201 Created` | `CU03_Step10_WhenValidState_ContinuesUseCase` | `CU03_Step10_WhenValidRequest_Returns201Created` |
| 7a. Guardar curso como borrador (A1) | `201 Created` | `CU03_Alt01_WhenConditionOccurs_HandlesExpectedBranch` | `CU03_Alt01_WhenConditionOccurs_Returns201Created` |
| 8a. Curso sin módulos (A2) | `400 Bad Request` | `CU03_Alt02_WhenConditionOccurs_HandlesExpectedBranch` | `CU03_Alt02_WhenConditionOccurs_Returns400BadRequest` |
| 8b. Información obligatoria incompleta (A3) | `400 Bad Request` | `CU03_Alt03_WhenConditionOccurs_HandlesExpectedBranch` | `CU03_Alt03_WhenConditionOccurs_Returns400BadRequest` |

> Los nombres de tests documentan el contrato esperado y deberán vincularse con la
> suite automatizada cuando exista una implementación backend.
