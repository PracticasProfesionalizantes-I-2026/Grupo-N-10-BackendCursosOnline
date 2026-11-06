# Caso de Uso: Auditar Solicitud de Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP describen la API vigente. La sección de evidencia vincula las pruebas
> ejecutables y distingue las diferencias del prototipo local.

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
4. El sistema muestra el contenido guardado al enviar la solicitud, incluidos los módulos. En la API el Administrador o Profesor solicitante puede consultar ese envío por su identificador. Las auditorías antiguas sin copia muestran su decisión sin atribuirles contenido. Las listas de auditorías admiten paginación optativa.
5. El Administrador revisa la información.
6. El Administrador selecciona "Aprobar".
7. El sistema registra la decisión y cambia el curso a PUBLICADO. La API exige una decisión explícita; si falta o es inválida, responde HTTP 400 y conserva el estado anterior. La observación es obligatoria al solicitar cambios. **Reglas aplicables:** **RN-06**. La aprobación reemplaza la publicación vigente. Si se rechaza la revisión o se solicitan cambios, se conserva la publicación anterior si existe. La revisión rechazada y su decisión quedan registradas. El autor puede recuperar el contenido como un nuevo borrador mediante CU-05. La revisión rechazada y su decisión se conservan. Si el cuerpo de la decisión supera el límite de transporte, la API responde HTTP 413 sin resolver la auditoría. Al aprobar una nueva publicación, el cupo no puede quedar por debajo de los alumnos aprobados; el conflicto conserva la publicación previa.
8. El sistema informa que la auditoría fue aprobada.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **6a. Rechazar solicitud — A1 (HTTP 200 OK):**
  1. El Administrador selecciona "Rechazar".
  2. El sistema registra la decisión.
  3. La revisión queda RECHAZADA. Si ya había una publicación, sigue disponible según el estado operativo. El solicitante puede iniciar la recuperación de CU-05.

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
| `400` | Bad Request | Decisión ausente o inválida, u observación ausente al solicitar cambios. |
| `409` | Conflict | A3: La solicitud ya no corresponde a un curso EN REVISIÓN. |
| `413` | Content Too Large | Cuerpo mayor que el límite de transporte de la API. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Evidencia ejecutable de CU-04

| Fuente | Recorrido o contrato |
|---|---|
| API | `GET /api/course-reviews y /api/course-reviews/{id}; POST /api/course-reviews/{id}/decision` |
| Pruebas automatizadas | CourseCreationIntegrationTests; CourseConcurrencyIntegrationTests; CourseReviewHistoryIntegrationTests; demo.test.mjs |
| Bruno | Courses/09–11,14–15; Exposición/06–07 |

200 aplica decisión; 400/409 preservan estado. El rechazo conserva la publicación. La recuperación preserva el rechazo y cada nuevo envío tiene una copia inmutable.
