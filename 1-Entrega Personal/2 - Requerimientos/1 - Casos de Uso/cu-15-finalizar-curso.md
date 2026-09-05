# Caso de Uso: Finalizar Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP y los nombres de tests constituyen una propuesta de trazabilidad
> técnica; el documento fuente define actualmente un prototipo frontend académico.

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
6. El sistema verifica el permiso y el estado, cambia el curso a FINALIZADO e informa el cierre del ciclo y que no admite nuevas solicitudes de inscripción. **Reglas aplicables:** **RN-12**.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **5a. El Administrador decide no finalizar — A1 (HTTP 200 OK):**
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
| `200` | OK | resultado satisfactorio de Finalizar Curso; A1: El Administrador decide no finalizar. |
| `409` | Conflict | A2: Estado incompatible. |
| `403` | Forbidden | A3: Actor sin permiso. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Matriz de trazabilidad CU-15 → Test

| Paso del CU | Excepción / Código | Test unitario propuesto (Negocio) | Test de integración propuesto (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Flujo principal | `200 OK` | `CU15_Step01_WhenValidState_ContinuesUseCase` | `CU15_Step01_WhenValidRequest_Returns200OK` |
| Paso 2. Flujo principal | `200 OK` | `CU15_Step02_WhenValidState_ContinuesUseCase` | `CU15_Step02_WhenValidRequest_Returns200OK` |
| Paso 3. Flujo principal | `200 OK` | `CU15_Step03_WhenValidState_ContinuesUseCase` | `CU15_Step03_WhenValidRequest_Returns200OK` |
| Paso 4. Flujo principal | `200 OK` | `CU15_Step04_WhenValidState_ContinuesUseCase` | `CU15_Step04_WhenValidRequest_Returns200OK` |
| Paso 5. Flujo principal | `200 OK` | `CU15_Step05_WhenValidState_ContinuesUseCase` | `CU15_Step05_WhenValidRequest_Returns200OK` |
| Paso 6. Flujo principal | `200 OK` | `CU15_Step06_WhenValidState_ContinuesUseCase` | `CU15_Step06_WhenValidRequest_Returns200OK` |
| 5a. El Administrador decide no finalizar (A1) | `200 OK` | `CU15_Alt01_WhenConditionOccurs_HandlesExpectedBranch` | `CU15_Alt01_WhenConditionOccurs_Returns200OK` |
| 6a. Estado incompatible (A2) | `409 Conflict` | `CU15_Alt02_WhenConditionOccurs_HandlesExpectedBranch` | `CU15_Alt02_WhenConditionOccurs_Returns409Conflict` |
| 3a. Actor sin permiso (A3) | `403 Forbidden` | `CU15_Alt03_WhenConditionOccurs_HandlesExpectedBranch` | `CU15_Alt03_WhenConditionOccurs_Returns403Forbidden` |

> Los nombres de tests documentan el contrato esperado y deberán vincularse con la
> suite automatizada cuando exista una implementación backend.
