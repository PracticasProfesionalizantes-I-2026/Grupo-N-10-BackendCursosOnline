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
| **Reglas de negocio relacionadas** | CU-12A: estados de inscripción; CU-12B: RN-11. |
| **Referencias funcionales** | CU-12A: RF-26 y RF-30; CU-12B: RF-24 y RN-11. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Alumno

---

### 1. BREVE DESCRIPCIÓN

CU-12 conserva el identificador histórico de la consulta de inscripciones y el acceso a cursos. Sus dos metas se especifican por separado en CU-12A y CU-12B; no se crean endpoints nuevos.

#### CU-12A — Consultar Estado de Solicitudes e Inscripciones

- **Actor y objetivo:** Alumno autenticado; conocer el estado de sus solicitudes e inscripciones propias (RF-26 y RF-30).
- **Disparador:** el Alumno abre «Mis solicitudes e inscripciones».
- **Precondiciones:** sesión de Alumno; no se exige tener inscripciones ni una inscripción aprobada.
- **Flujo principal:** 1. El Alumno solicita `GET /api/enrollments/mine`. 2. El sistema devuelve HTTP 200 con sus inscripciones y estados PENDIENTE, APROBADA, RECHAZADA o CANCELADA; admite `limit` de 1 a 100 y `offset` no negativo junto con `limit`. 3. El Alumno consulta el estado; si está APROBADA, puede iniciar CU-12B. La consulta no modifica datos.
- **Excepciones:** sin inscripciones, HTTP 200 con lista vacía; paginación inválida, HTTP 400; sin autenticación, HTTP 401; otro rol, HTTP 403. No se exponen inscripciones ajenas.
- **Postcondición:** el Alumno conoce el estado de sus solicitudes; no cambia la inscripción.

#### CU-12B — Acceder al Contenido del Curso Aprobado

- **Actor y objetivo:** Alumno autenticado titular de una inscripción APROBADA; acceder a la versión publicada del curso (RF-24; RN-11).
- **Disparador:** el Alumno elige una inscripción aprobada desde CU-12A.
- **Precondiciones:** la inscripción existe, pertenece al Alumno y permanece APROBADA; existe una publicación válida del curso.
- **Flujo principal:** 1. El Alumno solicita `GET /api/enrollments/{id}/course-content`. 2. El sistema comprueba titularidad y estado APROBADA. 3. Devuelve HTTP 200 con el contenido y los módulos de la publicación, incluso si el curso está pausado o finalizado. 4. El Alumno puede consultar progreso (CU-08), completar módulos (CU-16) o cancelar la inscripción (CU-13). La lectura no modifica estados.
- **Excepciones:** sin autenticación, HTTP 401; rol ajeno o inscripción de otro Alumno, HTTP 403; inscripción inexistente, HTTP 404; inscripción PENDIENTE, RECHAZADA o CANCELADA, HTTP 409; publicación inválida, HTTP 409.
- **Postcondición:** el contenido se muestra solo al titular aprobado; la consulta conserva inscripción, progreso y publicación.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | CU-12A devuelve inscripciones propias o lista vacía; CU-12B devuelve contenido publicado al titular aprobado. |
| `409` | Conflict | CU-12B: inscripción no aprobada o publicación inválida. |
| `403` | Forbidden | Rol ajeno o inscripción de otro Alumno. |
| `401` | Unauthorized | Falta autenticación. |
| `404` | Not Found | CU-12B: inscripción inexistente. |
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

CU-12A: 200 con lista propia, 400 por paginación. CU-12B: 200 con publicación, 403 ajena, 404 inexistente y 409 si no está aprobada. Pausar o finalizar no revoca acceso.
