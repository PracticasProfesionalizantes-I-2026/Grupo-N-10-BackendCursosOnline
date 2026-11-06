# Caso de Uso: Actualizar Curso Creado

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP describen la API vigente. La sección de evidencia vincula las pruebas
> ejecutables y distingue las diferencias del prototipo local.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-05 |
| **Nombre** | Actualizar Curso Creado |
| **Actor Principal** | Profesor / Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Profesor y Administrador → mantener actualizada la información del curso; Administrador auditor → revisar modificaciones; Alumno → consultar contenido aprobado |
| **Disparador (Trigger)** | El Profesor accede a sus cursos o solicitudes de auditoría, o el Administrador a la gestión de cursos, y selecciona "Editar Curso". |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-03 a RN-07 y RN-15 |
| **Referencias funcionales** | RF-09 a RF-16 y RF-31; RN-03 a RN-07 y RN-15; permiso de edición del Administrador. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Profesor / Administrador
- **Secundarios:** Administrador, en la auditoría posterior del CU-04.

---

### 1. BREVE DESCRIPCIÓN

Permite retomar un BORRADOR o modificar un curso PUBLICADO o con CAMBIOS SOLICITADOS. El Profesor actúa únicamente sobre cursos propios y el Administrador desde la gestión general de cursos. Guardar un BORRADOR y enviar modificaciones a auditoría son resultados diferentes.

### 2. PRECONDICIONES

- El usuario debe haber iniciado sesión como Profesor o Administrador.
- El curso debe existir. Si actúa un Profesor, debe pertenecerle.
- Para este flujo, el curso está PUBLICADO o en CAMBIOS SOLICITADOS; la edición de un BORRADOR se describe en A3.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Profesor accede a sus cursos o a sus solicitudes de auditoría; el Administrador accede a la gestión de cursos.
2. El sistema muestra los cursos accesibles según el rol y, cuando corresponda, el estado de la auditoría y las observaciones recibidas.
3. El usuario selecciona el curso y solicita editarlo. El sistema verifica el rol y la propiedad cuando actúa un Profesor. **Reglas aplicables:** **RN-03**.
4. El sistema muestra los datos actuales y los módulos del curso. En la API la consulta de gestión entrega ETag. El editor puede enviarlo en If-Match al guardar por PUT. Si la etiqueta quedó obsoleta, la API responde 412 sin escribir y el editor debe recargar. Una etiqueta mal formada devuelve 400. Si la versión de trabajo ya está EN REVISIÓN o RECHAZADA, la edición devuelve 409. El Administrador edita la versión de trabajo mediante el mismo flujo de auditoría.
5. El usuario modifica los datos del curso o sus módulos, con nombre, descripción, duración y contenido o recursos. **Reglas aplicables:** **RN-15**.
6. El sistema valida los datos obligatorios y la existencia de al menos un módulo, y recalcula la duración total como suma de sus duraciones. **Reglas aplicables:** **RN-04**, **RN-05**.
7. El usuario confirma el envío de los cambios a revisión.
8. El sistema registra las modificaciones, cambia el curso a EN REVISIÓN y genera la solicitud de auditoría del CU-04. **Reglas aplicables:** **RN-06**, **RN-07**.
9. El sistema informa el resultado y lo muestra en las solicitudes de auditoría del usuario.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **6a. Datos obligatorios incompletos o curso sin módulos — A1 (HTTP 400 Bad Request):**
  1. El sistema identifica los datos pendientes o la ausencia de módulos. **Reglas aplicables:** **RN-04**.
  2. No envía los cambios a revisión; el usuario corrige la información y vuelve al paso 6.

* **4a. Corregir CAMBIOS SOLICITADOS — A2 (HTTP 200 OK):**
  1. El usuario consulta las observaciones de la auditoría y corrige la información indicada. **Reglas aplicables:** **RN-07**.
  2. Continúa desde el paso 6; al confirmar el envío, el curso vuelve a EN REVISIÓN.

* **3a. Retomar un BORRADOR — A3 (HTTP 200 OK):**
  1. El usuario selecciona un curso BORRADOR y actualiza sus datos o módulos.
  2. Si elige "Guardar como borrador", el sistema conserva la información, recalcula la duración y mantiene BORRADOR.
  3. Si elige "Enviar a revisión", continúa desde el paso 6; solo un envío válido cambia BORRADOR a EN REVISIÓN.

* **3b. Profesor intenta editar un curso ajeno — A4 (HTTP 403 Forbidden):**
  1. El sistema detecta que el curso no pertenece al Profesor. **Reglas aplicables:** **RN-03**.
  2. Restringe la edición y conserva la información y el estado del curso.

* **4b. Otro editor guardó antes — A5 (HTTP 412 Precondition Failed):** Si el cliente envió la etiqueta anterior, la API conserva la edición vigente y exige recargar antes de volver a intentar. Si una escritura concurrente se detecta durante la persistencia o el estado no admite edición, puede responder 409.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- Al confirmar modificaciones de un curso PUBLICADO o correcciones de CAMBIOS SOLICITADOS, el curso queda EN REVISIÓN y se genera una solicitud de auditoría.
- Los cambios enviados no se consideran aprobados hasta que se resuelva CU-04.
- Si solo se guarda la edición de un BORRADOR, el curso conserva BORRADOR y no se inicia auditoría.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Actualizar Curso Creado; A2: Corregir CAMBIOS SOLICITADOS; A3: Retomar un BORRADOR. |
| `400` | Bad Request | A1: Datos obligatorios incompletos o curso sin módulos. |
| `403` | Forbidden | A4: Profesor intenta editar un curso ajeno. |
| `412` | Precondition Failed | A5: etiqueta de edición obsoleta. |
| `409` | Conflict | Estado incompatible o conflicto durante la escritura. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Evidencia ejecutable de CU-05

| Fuente | Recorrido o contrato |
|---|---|
| API | `GET y PUT /api/courses/{id}; POST /api/courses/{id}/submit-review` |
| Pruebas automatizadas | CourseUpdateIntegrationTests; TraceabilityContractIntegrationTests; demo.test.mjs |
| Bruno | Courses/02,04,12,05 |

Guardado y envío separados. 403 por curso ajeno, 409 por estado, 412 por etiqueta obsoleta. La publicación anterior sigue vigente.
