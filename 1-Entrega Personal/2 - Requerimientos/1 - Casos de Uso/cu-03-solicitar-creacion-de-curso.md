# Caso de Uso: Solicitar Creación de Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP describen la API vigente. La sección de evidencia vincula las pruebas
> ejecutables y distingue las diferencias del prototipo local.

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
- **Secundarios:** Administrador distinto de quien solicitó la revisión, en la auditoría posterior del CU-04 (RN-17, issue docente #16).

---

### 1. BREVE DESCRIPCIÓN

Permite que un Profesor o Administrador prepare un curso, lo guarde como borrador o lo envíe a revisión administrativa una vez completada la información requerida y sus módulos.

### 2. PRECONDICIONES

- El Profesor o Administrador debe haber iniciado sesión.

### 3. FLUJO PRINCIPAL (Camino feliz - HTTP 201 para crear y 200 para enviar)

1. El Profesor o Administrador selecciona "Crear Curso".
2. El sistema muestra el formulario del curso.
3. El Profesor o Administrador completa título, descripción, categoría, nivel, modalidad, cupo máximo, objetivos de aprendizaje y requisitos previos sugeridos.
4. El Profesor o Administrador agrega al menos un módulo. **Reglas aplicables:** **RN-04**.
5. Para cada módulo, completa nombre, descripción y duración y agrega el contenido o recursos previstos. **Reglas aplicables:** **RN-15**.
6. El sistema calcula la duración total como suma de las duraciones de los módulos. **Reglas aplicables:** **RN-05**.
7. El Profesor o Administrador selecciona "Enviar a revisión".
8. El sistema valida la información obligatoria y la existencia de al menos un módulo. **Reglas aplicables:** **RN-04**. Si el cuerpo de creación supera el límite de transporte, la API responde HTTP 413 y no crea el borrador. El cupo es positivo. Se admiten como máximo 100 módulos, 30 objetivos, 30 requisitos, 50 recursos por módulo y los máximos de texto aprobados; una entrada excedida recibe HTTP 400 sin guardar el curso.
9. El sistema cambia el curso a EN REVISIÓN y registra la solicitud de auditoría. **Reglas aplicables:** **RN-06**. En la API el guardado crea primero un BORRADOR con POST /api/courses y responde 201. El envío posterior mediante POST /api/courses/{id}/submit-review responde 200 y genera la auditoría. Si el envío falla, la creación ya confirmada se conserva como borrador. El prototipo representa el alta como Profesor. El alta administrativa está disponible en la API.
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
| `201` | Created | Creación del borrador, incluido A1. |
| `400` | Bad Request | A2: Curso sin módulos; A3: Información obligatoria incompleta. |
| `200` | OK | Envío del borrador a revisión. |
| `413` | Content Too Large | Cuerpo mayor que el límite de transporte de la API. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Evidencia ejecutable de CU-03

| Fuente | Recorrido o contrato |
|---|---|
| API | `POST /api/courses; POST /api/courses/{id}/submit-review` |
| Pruebas automatizadas | CourseCreationIntegrationTests; CourseServiceTests; OpenApiContractTests |
| Bruno | Courses/03,05; Exposición/04–05 |

201 crea borrador; 200 crea solicitud. El alta administrativa está en la API; el prototipo crea por Profesor.
