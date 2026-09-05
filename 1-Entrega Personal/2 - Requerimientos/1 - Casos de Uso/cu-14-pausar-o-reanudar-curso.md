# Caso de Uso: Pausar o Reanudar Curso

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP y los nombres de tests constituyen una propuesta de trazabilidad
> técnica; el documento fuente define actualmente un prototipo frontend académico.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-14 |
| **Nombre** | Pausar o Reanudar Curso |
| **Actor Principal** | Profesor / Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Profesor y Administrador → controlar temporalmente la disponibilidad de cursos; Alumno → conocer cuándo un curso no admite nuevas inscripciones |
| **Disparador (Trigger)** | El Profesor o Administrador selecciona "Pausar" o "Reanudar" sobre un curso que puede gestionar. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-03 y RN-12 |
| **Referencias funcionales** | RF-17; RN-03 y RN-12; permisos de pausa/reanudación y estados de cursos. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Profesor / Administrador

---

### 1. BREVE DESCRIPCIÓN

Permite pausar un curso PUBLICADO o reanudar un curso PAUSADO. El Profesor actúa solo sobre cursos propios y el Administrador desde la gestión general.

### 2. PRECONDICIONES

- El usuario debe haber iniciado sesión como Profesor o Administrador.
- El curso debe pertenecer al Profesor cuando ese sea el actor.
- Para pausar, el curso está PUBLICADO; para reanudar, está PAUSADO.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Profesor accede a sus cursos o el Administrador a la gestión de cursos. **Reglas aplicables:** **RN-03**.
2. El sistema muestra los cursos que puede gestionar y sus estados.
3. El actor selecciona un curso PUBLICADO y solicita pausarlo.
4. El sistema verifica los permisos y el estado del curso. **Reglas aplicables:** **RN-03**, **RN-12**.
5. El actor confirma la pausa.
6. El sistema cambia el curso a PAUSADO e informa que no admite nuevas solicitudes de inscripción. **Reglas aplicables:** **RN-12**.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Reanudar un curso PAUSADO — A1 (HTTP 200 OK):**
  1. El actor selecciona un curso PAUSADO y solicita reanudarlo. **Reglas aplicables:** **RN-12**.
  2. El sistema verifica los permisos, la propiedad cuando actúa el Profesor y el estado PAUSADO.
  3. El actor confirma la reanudación.
  4. El sistema cambia el curso a PUBLICADO y vuelve a habilitar nuevas solicitudes de inscripción.

* **4a. Rol sin permiso o curso ajeno al Profesor — A2 (HTTP 403 Forbidden):**
  1. El sistema restringe la operación y conserva el estado del curso. **Reglas aplicables:** **RN-03**.

* **4b. Estado incompatible con la operación — A3 (HTTP 409 Conflict):**
  1. El sistema detecta que el curso no está PUBLICADO para pausar o PAUSADO para reanudar. **Reglas aplicables:** **RN-12**.
  2. Muestra el estado actual y no realiza la transición.

* **5a. No confirmar la operación — A4 (HTTP 200 OK):**
  1. El actor abandona la confirmación y el curso conserva su estado.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- Al pausar, el curso pasa de PUBLICADO a PAUSADO y deja de admitir nuevas solicitudes de inscripción.
- Al reanudar, el curso pasa de PAUSADO a PUBLICADO y vuelve a admitir nuevas solicitudes.
- La operación cambia la disponibilidad del curso; no aprueba inscripciones ni sustituye la auditoría de modificaciones.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Pausar o Reanudar Curso; A1: Reanudar un curso PAUSADO; A4: No confirmar la operación. |
| `403` | Forbidden | A2: Rol sin permiso o curso ajeno al Profesor. |
| `409` | Conflict | A3: Estado incompatible con la operación. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Matriz de trazabilidad CU-14 → Test

| Paso del CU | Excepción / Código | Test unitario propuesto (Negocio) | Test de integración propuesto (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Flujo principal | `200 OK` | `CU14_Step01_WhenValidState_ContinuesUseCase` | `CU14_Step01_WhenValidRequest_Returns200OK` |
| Paso 2. Flujo principal | `200 OK` | `CU14_Step02_WhenValidState_ContinuesUseCase` | `CU14_Step02_WhenValidRequest_Returns200OK` |
| Paso 3. Flujo principal | `200 OK` | `CU14_Step03_WhenValidState_ContinuesUseCase` | `CU14_Step03_WhenValidRequest_Returns200OK` |
| Paso 4. Flujo principal | `200 OK` | `CU14_Step04_WhenValidState_ContinuesUseCase` | `CU14_Step04_WhenValidRequest_Returns200OK` |
| Paso 5. Flujo principal | `200 OK` | `CU14_Step05_WhenValidState_ContinuesUseCase` | `CU14_Step05_WhenValidRequest_Returns200OK` |
| Paso 6. Flujo principal | `200 OK` | `CU14_Step06_WhenValidState_ContinuesUseCase` | `CU14_Step06_WhenValidRequest_Returns200OK` |
| 3a. Reanudar un curso PAUSADO (A1) | `200 OK` | `CU14_Alt01_WhenConditionOccurs_HandlesExpectedBranch` | `CU14_Alt01_WhenConditionOccurs_Returns200OK` |
| 4a. Rol sin permiso o curso ajeno al Profesor (A2) | `403 Forbidden` | `CU14_Alt02_WhenConditionOccurs_HandlesExpectedBranch` | `CU14_Alt02_WhenConditionOccurs_Returns403Forbidden` |
| 4b. Estado incompatible con la operación (A3) | `409 Conflict` | `CU14_Alt03_WhenConditionOccurs_HandlesExpectedBranch` | `CU14_Alt03_WhenConditionOccurs_Returns409Conflict` |
| 5a. No confirmar la operación (A4) | `200 OK` | `CU14_Alt04_WhenConditionOccurs_HandlesExpectedBranch` | `CU14_Alt04_WhenConditionOccurs_Returns200OK` |

> Los nombres de tests documentan el contrato esperado y deberán vincularse con la
> suite automatizada cuando exista una implementación backend.
