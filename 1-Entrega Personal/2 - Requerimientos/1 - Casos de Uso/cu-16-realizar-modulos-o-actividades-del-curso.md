# Caso de Uso: Realizar Módulos o Actividades del Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP y los nombres de tests constituyen una propuesta de trazabilidad
> técnica; el documento fuente define actualmente un prototipo frontend académico.

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
4. El sistema verifica que la inscripción propia continúe APROBADA y que el módulo o actividad pertenezca a ese curso. **Reglas aplicables:** **RN-11**, **RN-15**.
5. El Alumno realiza el módulo o actividad. Al completarlo, el sistema refleja el avance correspondiente dentro de su progreso.
6. El sistema actualiza el porcentaje asociado a los módulos o actividades completadas y el estado definido en RN-13. **Reglas aplicables:** **RN-13**.
7. El Alumno consulta el avance resultante y puede continuar con el curso.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **5a. El Alumno todavía no completa el módulo o actividad — A1 (HTTP 200 OK):**
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
| `200` | OK | resultado satisfactorio de Realizar Módulos o Actividades del Curso; A1: El Alumno todavía no completa el módulo o actividad; A4: Se alcanza el total del progreso definido para el curso. |
| `409` | Conflict | A2: Inscripción sin acceso activo; A3: Módulo o actividad fuera del curso autorizado. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Matriz de trazabilidad CU-16 → Test

| Paso del CU | Excepción / Código | Test unitario propuesto (Negocio) | Test de integración propuesto (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Flujo principal | `200 OK` | `CU16_Step01_WhenValidState_ContinuesUseCase` | `CU16_Step01_WhenValidRequest_Returns200OK` |
| Paso 2. Flujo principal | `200 OK` | `CU16_Step02_WhenValidState_ContinuesUseCase` | `CU16_Step02_WhenValidRequest_Returns200OK` |
| Paso 3. Flujo principal | `200 OK` | `CU16_Step03_WhenValidState_ContinuesUseCase` | `CU16_Step03_WhenValidRequest_Returns200OK` |
| Paso 4. Flujo principal | `200 OK` | `CU16_Step04_WhenValidState_ContinuesUseCase` | `CU16_Step04_WhenValidRequest_Returns200OK` |
| Paso 5. Flujo principal | `200 OK` | `CU16_Step05_WhenValidState_ContinuesUseCase` | `CU16_Step05_WhenValidRequest_Returns200OK` |
| Paso 6. Flujo principal | `200 OK` | `CU16_Step06_WhenValidState_ContinuesUseCase` | `CU16_Step06_WhenValidRequest_Returns200OK` |
| Paso 7. Flujo principal | `200 OK` | `CU16_Step07_WhenValidState_ContinuesUseCase` | `CU16_Step07_WhenValidRequest_Returns200OK` |
| 5a. El Alumno todavía no completa el módulo o actividad (A1) | `200 OK` | `CU16_Alt01_WhenConditionOccurs_HandlesExpectedBranch` | `CU16_Alt01_WhenConditionOccurs_Returns200OK` |
| 4a. Inscripción sin acceso activo (A2) | `409 Conflict` | `CU16_Alt02_WhenConditionOccurs_HandlesExpectedBranch` | `CU16_Alt02_WhenConditionOccurs_Returns409Conflict` |
| 4b. Módulo o actividad fuera del curso autorizado (A3) | `409 Conflict` | `CU16_Alt03_WhenConditionOccurs_HandlesExpectedBranch` | `CU16_Alt03_WhenConditionOccurs_Returns409Conflict` |
| 6a. Se alcanza el total del progreso definido para el curso (A4) | `200 OK` | `CU16_Alt04_WhenConditionOccurs_HandlesExpectedBranch` | `CU16_Alt04_WhenConditionOccurs_Returns200OK` |

> Los nombres de tests documentan el contrato esperado y deberán vincularse con la
> suite automatizada cuando exista una implementación backend.
