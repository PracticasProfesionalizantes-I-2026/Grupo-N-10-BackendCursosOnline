# Caso de Uso: Pausar y Reanudar Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> CU-14 conserva el identificador histórico; CU-14A y CU-14B describen metas autónomas.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-14A / CU-14B, bajo CU-14 |
| **Nombre** | Pausar Curso / Reanudar Curso |
| **Actor Principal** | Profesor propietario / Administrador |
| **Alcance / Nivel** | Sistema Lumen; metas de usuario separadas |
| **Reglas de negocio relacionadas** | RN-03 y RN-12 |
| **Referencias funcionales** | RF-17; permisos y estados de cursos |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

## CU-14A — Pausar Curso

### 1. BREVE DESCRIPCIÓN

El Profesor propietario o Administrador suspende nuevas solicitudes de inscripción sin borrar la publicación ni las inscripciones ya registradas.

### 2. PRECONDICIONES

- El actor inició sesión como Profesor propietario o Administrador.
- El curso tiene una versión publicada y estado operativo ACTIVO. La revisión de trabajo puede estar en otro estado.

### 3. FLUJO PRINCIPAL (HTTP 200)

1. El actor consulta sus cursos o la gestión general y selecciona «Pausar».
2. El sistema muestra el estado y el actor confirma.
3. El sistema verifica rol, propiedad, publicación y estado activo.
4. `POST /api/courses/{id}/pause` cambia el estado operativo a PAUSADO. Deja de admitir solicitudes nuevas; conserva la publicación, el acceso de inscripciones aprobadas y las solicitudes pendientes. El Administrador puede resolver pendientes si queda cupo. Enviar o decidir una revisión no reanuda el curso.

### 4. FLUJOS ALTERNATIVOS

- Rol sin permiso o curso ajeno: HTTP 403, sin cambio.
- Curso inexistente: HTTP 404.
- Sin publicación o estado incompatible: HTTP 409, sin cambio.
- Si el actor no confirma, no se envía solicitud.

### 5. POSTCONDICIÓN

El curso queda PAUSADO; no admite nuevas solicitudes y conserva su publicación e inscripciones anteriores.

## CU-14B — Reanudar Curso

### 1. BREVE DESCRIPCIÓN

El Profesor propietario o Administrador vuelve a admitir solicitudes de inscripción en un curso pausado.

### 2. PRECONDICIONES

- El actor inició sesión como Profesor propietario o Administrador.
- El curso tiene una versión publicada y estado operativo PAUSADO. La revisión de trabajo puede estar en otro estado.

### 3. FLUJO PRINCIPAL (HTTP 200)

1. El actor consulta sus cursos o la gestión general y selecciona «Reanudar».
2. El sistema muestra el estado y el actor confirma.
3. El sistema verifica rol, propiedad, publicación y estado pausado.
4. `POST /api/courses/{id}/resume` cambia el estado operativo a ACTIVO y vuelve a admitir solicitudes. La operación no aprueba inscripciones ni sustituye la auditoría de modificaciones.

### 4. FLUJOS ALTERNATIVOS

- Rol sin permiso o curso ajeno: HTTP 403, sin cambio.
- Curso inexistente: HTTP 404.
- Estado distinto de PAUSADO o sin publicación: HTTP 409, sin cambio.
- Si el actor no confirma, no se envía solicitud.

### 5. POSTCONDICIÓN

El curso vuelve a estado operativo ACTIVO; conserva la publicación y admite nuevas solicitudes.

---

## Anexo: evidencia ejecutable

| Fuente | Recorrido o contrato |
|---|---|
| API | `POST /api/courses/{id}/pause` y `POST /api/courses/{id}/resume` |
| Pruebas automatizadas | CourseClosureEnrollmentIntegrationTests; EnrollmentCreationIntegrationTests; OpenApiContractTests; demo.test.mjs |
| Bruno | Courses/06–07 |

Las dos transiciones son independientes. Un intento con estado incompatible responde 409 sin alterar el curso.