# Caso de Uso: Consultar Inscripciones y Acceder a Cursos

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP describen la API vigente. La sección de evidencia vincula las pruebas
> ejecutables y distingue las diferencias del prototipo local.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-12 |
| **Nombre** | Consultar Inscripciones y Acceder a Cursos |
| **Actor Principal** | Alumno |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Alumno → conocer el estado de sus inscripciones y acceder a cursos aprobados; responsables del sistema → impedir accesos sin aprobación |
| **Disparador (Trigger)** | El Alumno selecciona solicitudes e inscripciones o sus cursos desde el panel. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-11 |
| **Referencias funcionales** | RF-24, RF-26 y RF-30; RN-11; estados de inscripción. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Alumno

---

### 1. BREVE DESCRIPCIÓN

Permite que el Alumno consulte sus solicitudes, identifique sus cursos con inscripción APROBADA y acceda al curso cuando corresponda.

### 2. PRECONDICIONES

- El usuario debe haber iniciado sesión como Alumno.
- Solo puede consultar sus propias inscripciones.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Alumno accede a sus solicitudes e inscripciones.
2. El sistema muestra sus solicitudes y los cursos asociados, con el estado de cada inscripción.
3. El Alumno consulta el estado y selecciona un curso con inscripción APROBADA. **Reglas aplicables:** **RN-11**.
4. El sistema verifica que la inscripción pertenezca al Alumno y permanezca APROBADA. **Reglas aplicables:** **RN-11**.
5. El sistema permite acceder al curso y a sus módulos o actividades. **Reglas aplicables:** **RN-11**. Los nombres, el contenido y los módulos del alumno provienen de la publicación. La API admite paginación de las inscripciones propias. La pausa o finalización no revoca el acceso de una inscripción aprobada.
6. El Alumno puede consultar su progreso (CU-08), realizar módulos o actividades (CU-16) o solicitar cancelar la inscripción (CU-13).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. No existen solicitudes o inscripciones propias — A1 (HTTP 200 OK):**
  1. El sistema informa que no hay inscripciones para mostrar.
  2. El Alumno puede consultar cursos PUBLICADOS mediante CU-11.

* **4a. Inscripción PENDIENTE, RECHAZADA o CANCELADA — A2 (HTTP 409 Conflict):**
  1. El sistema muestra el estado correspondiente y no permite acceso al curso. **Reglas aplicables:** **RN-11**.
  2. Si la inscripción está PENDIENTE, permanece a la espera de resolución en CU-07.

* **4b. La inscripción pertenece a otro Alumno — A3 (HTTP 403 Forbidden):**
  1. El sistema restringe la consulta y el acceso.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- El Alumno visualiza el estado PENDIENTE, APROBADA, RECHAZADA o CANCELADA de sus inscripciones, según corresponda.
- El acceso al curso se habilita únicamente cuando la inscripción está APROBADA. La consulta no modifica estados.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Consultar Inscripciones y Acceder a Cursos; A1: No existen solicitudes o inscripciones propias. |
| `409` | Conflict | A2: Inscripción PENDIENTE, RECHAZADA o CANCELADA. |
| `403` | Forbidden | A3: La inscripción pertenece a otro Alumno. |
| `400` | Bad Request | Parámetros de paginación inválidos. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

La paginación de la API es optativa. Acepta limit de 1 a 100 y offset desde cero con limit. Sin parámetros mantiene el arreglo completo.

### Evidencia ejecutable de CU-12

| Fuente | Recorrido o contrato |
|---|---|
| API | `GET /api/enrollments/mine; GET /api/enrollments/{id}/course-content` |
| Pruebas automatizadas | PagingIntegrationTests; EnrollmentCreationIntegrationTests; ProgressIntegrationTests; demo.test.mjs |
| Bruno | Enrollments/02,04–05; Exposición/11 |

200 consulta o acceso a publicación, 403 ajena y 409 si no está aprobada. Cerrar el catálogo no revoca acceso.
