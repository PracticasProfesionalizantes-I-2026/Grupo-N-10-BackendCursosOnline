# Caso de Uso: Finalizar Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP describen la API vigente. La sección de evidencia vincula las pruebas
> ejecutables y distingue las diferencias del prototipo local.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-15 |
| **Nombre** | Finalizar Curso |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Administrador → cerrar el ciclo de un curso; Profesor y Alumno → conservar un estado final claro; sistema → impedir nuevas solicitudes |
| **Disparador (Trigger)** | El Administrador selecciona "Finalizar" desde la gestión de cursos. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-12 |
| **Referencias funcionales** | RF-18; RN-12; permiso de finalización y estados de cursos. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Administrador

---

### 1. BREVE DESCRIPCIÓN

Permite que el Administrador cierre el ciclo de un curso PUBLICADO o PAUSADO.

### 2. PRECONDICIONES

- El usuario debe haber iniciado sesión como Administrador.
- El curso debe estar PUBLICADO o PAUSADO.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Administrador accede a la gestión de cursos.
2. Selecciona un curso PUBLICADO o PAUSADO. **Reglas aplicables:** **RN-12**.
3. Solicita finalizar el curso.
4. El sistema muestra la operación y solicita confirmación.
5. El Administrador confirma la finalización.
6. El sistema verifica el permiso y el estado, cambia el curso a FINALIZADO e informa el cierre del ciclo y que no admite nuevas solicitudes de inscripción. **Reglas aplicables:** **RN-12**. La operación cierra el curso para nuevas solicitudes, conserva el acceso aprobado y no altera el avance ni cambia por sí misma las solicitudes pendientes.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **5a. El Administrador decide no finalizar — A1 (Sin solicitud HTTP):**
  1. Abandona la confirmación y el sistema conserva el estado del curso.

* **6a. Estado incompatible — A2 (HTTP 409 Conflict):**
  1. El sistema detecta que el curso no está PUBLICADO ni PAUSADO. **Reglas aplicables:** **RN-12**.
  2. Muestra el estado actual y no aplica la finalización.

* **3a. Actor sin permiso — A3 (HTTP 403 Forbidden):**
  1. El sistema detecta que el usuario no es Administrador.
  2. Restringe la operación sin cambiar el curso.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- El curso pasa a FINALIZADO y deja de admitir nuevas solicitudes de inscripción.
- La finalización del curso no equivale a marcar COMPLETADO el progreso del Alumno ni a cancelar su inscripción.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Finalizar Curso. |
| `409` | Conflict | A2: Estado incompatible. |
| `403` | Forbidden | A3: Actor sin permiso. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Evidencia ejecutable de CU-15

| Fuente | Recorrido o contrato |
|---|---|
| API | `POST /api/courses/{id}/finish` |
| Pruebas automatizadas | EnrollmentCreationIntegrationTests; OpenApiContractTests; demo.test.mjs |
| Bruno | Courses/08 |

200 finaliza una publicación activa o pausada. No revoca inscripciones aprobadas ni cambia el progreso. 403/409 ante rechazo.
