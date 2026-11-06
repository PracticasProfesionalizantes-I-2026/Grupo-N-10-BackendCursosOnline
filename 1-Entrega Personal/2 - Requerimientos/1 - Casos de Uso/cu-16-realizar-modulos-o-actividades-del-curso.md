# Caso de Uso: Realizar Módulos o Actividades del Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP describen la API vigente. La sección de evidencia vincula las pruebas
> ejecutables y distingue las diferencias del prototipo local.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-16 |
| **Nombre** | Realizar Módulos o Actividades del Curso |
| **Actor Principal** | Alumno |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Alumno → realizar contenido y conocer su avance; Profesor → consultar progreso consistente; sistema → vincular actividades, porcentaje y estado |
| **Disparador (Trigger)** | El Alumno accede a uno de sus cursos con inscripción APROBADA y selecciona un módulo o actividad. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-11, RN-13 y RN-15 |
| **Referencias funcionales** | RF-24 y RF-27; RN-11, RN-13 y RN-15; estados de progreso. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Alumno

---

### 1. BREVE DESCRIPCIÓN

Permite que el Alumno avance en un curso realizando sus módulos o actividades y que el sistema represente ese avance como porcentaje y estado de progreso.

### 2. PRECONDICIONES

- El usuario debe haber iniciado sesión como Alumno y actuar sobre su propia inscripción APROBADA.
- El módulo o actividad debe corresponder al curso al que el Alumno tiene acceso.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Alumno accede a un curso con inscripción APROBADA mediante CU-12. **Reglas aplicables:** **RN-11**.
2. El sistema muestra los módulos o actividades y su contenido o recursos.
3. El Alumno selecciona un módulo o actividad del curso. **Reglas aplicables:** **RN-15**.
4. El sistema verifica que la inscripción propia continúe APROBADA y que el módulo o actividad pertenezca a ese curso. **Reglas aplicables:** **RN-11**, **RN-15**. La API registra únicamente módulos de la publicación vigente. Repetir la confirmación del mismo módulo conserva un solo registro y el mismo avance. Si se republica, se conserva el avance de módulos idénticos, aunque se reordenen. Los cambios de contenido, nombre, descripción, duración o recursos generan otro identificador. Los completados anteriores se preservan en el historial. Finalizar el curso conserva la finalización de módulos por alumnos con inscripción aprobada.
5. El Alumno realiza el módulo o actividad. Al completarlo, el sistema refleja el avance correspondiente dentro de su progreso. En el prototipo, el alumno puede volver a marcar el módulo como pendiente en sus datos locales. La API ofrece la finalización idempotente.
6. El sistema actualiza el porcentaje asociado a los módulos o actividades completadas y el estado definido en RN-13. **Reglas aplicables:** **RN-13**.
7. El Alumno consulta el avance resultante y puede continuar con el curso.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **5a. El Alumno todavía no completa el módulo o actividad — A1 (Sin escritura HTTP):**
  1. No se incorpora ese módulo o actividad como completado al porcentaje. **Reglas aplicables:** **RN-13**.
  2. Se mantiene el progreso correspondiente a lo completado; si no existe avance, se muestra 0 % y NO INICIADO.

* **4a. Inscripción sin acceso activo — A2 (HTTP 409 Conflict):**
  1. El sistema detecta que la inscripción ya no está APROBADA. **Reglas aplicables:** **RN-11**.
  2. Restringe el acceso y no registra nuevo avance.

* **4b. Módulo o actividad fuera del curso autorizado — A3 (HTTP 409 Conflict):**
  1. El sistema restringe la operación y no modifica el progreso. **Reglas aplicables:** **RN-15**.

* **6a. Se alcanza el total del progreso definido para el curso — A4 (HTTP 200 OK):**
  1. El sistema muestra 100 % y COMPLETADO. **Reglas aplicables:** **RN-13**.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- El avance asociado a los módulos o actividades completadas queda representado en el progreso propio del Alumno.
- El porcentaje se corresponde con NO INICIADO (0 %), EN PROGRESO (1 % a 99 %) o COMPLETADO (100 %), conforme a RN-13.
- El progreso queda disponible para su consulta según permisos mediante CU-08, sin cambiar por sí mismo el estado del curso o de la inscripción.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Realizar Módulos o Actividades del Curso; A4: Se alcanza el total del progreso definido para el curso. |
| `409` | Conflict | A2: Inscripción sin acceso activo; A3: Módulo o actividad fuera del curso autorizado. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Evidencia ejecutable de CU-16

| Fuente | Recorrido o contrato |
|---|---|
| API | `PUT /api/enrollments/{id}/modules/{moduleId}/completion` |
| Pruebas automatizadas | CourseReviewHistoryIntegrationTests; ModuleCompletionServiceTests; ModuleCompletionIntegrationTests; EnrollmentConcurrencyRegressionTests |
| Bruno | Enrollments/09; Exposición/13,15–17 |

200 registra una vez; 403 por inscripción ajena y 409 por estado o módulo incorrecto. El prototipo representa avance local.
