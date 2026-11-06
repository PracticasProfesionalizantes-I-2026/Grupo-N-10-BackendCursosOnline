# Caso de Uso: Consultar Cursos Publicados

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP describen la API vigente. La sección de evidencia vincula las pruebas
> ejecutables y distingue las diferencias del prototipo local.

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
2. El sistema muestra el listado de cursos en estado PUBLICADO. La API devuelve la publicación vigente mientras el curso esté operativo, incluso durante la revisión de cambios. La API exige JWT. El prototipo ofrece también un catálogo público de demostración. El listado admite limit de 1 a 100 y offset desde cero. Sin esos parámetros devuelve el arreglo completo. El detalle de un curso que ya no está disponible devuelve 404.
3. El usuario selecciona un curso.
4. El sistema muestra su detalle y la información documentada del curso, incluidos sus módulos y duración total.
5. El usuario revisa la información y puede volver al listado. Si es Alumno y desea inscribirse, inicia CU-06.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. No hay cursos PUBLICADOS — A1 (HTTP 200 OK):**
  1. El sistema informa que no hay cursos disponibles para consultar.
  2. El usuario puede volver a su panel.

* **4a. El curso dejó de estar PUBLICADO — A2 (HTTP 404 Not Found):**
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
| `400` | Bad Request | Parámetros de paginación inválidos. |
| `404` | Not Found | A2: El curso dejó de estar PUBLICADO. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `404 Not Found`, según corresponda.

### Evidencia ejecutable de CU-11

| Fuente | Recorrido o contrato |
|---|---|
| API | `GET /api/catalog/courses; GET /api/catalog/courses/{id}` |
| Pruebas automatizadas | ApiIntegrationTests; TraceabilityContractIntegrationTests; demo.test.mjs |
| Bruno | Catalog/01–02 |

200 arreglo o detalle, 404 si ya no está disponible y 400 por paginación inválida. Solo Alumno solicita inscripción.
