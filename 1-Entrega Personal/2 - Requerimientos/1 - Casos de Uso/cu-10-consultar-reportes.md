# Caso de Uso: Consultar Reportes

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP y los nombres de tests constituyen una propuesta de trazabilidad
> técnica; el documento fuente define actualmente un prototipo frontend académico.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-10 |
| **Nombre** | Consultar Reportes |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Administrador → consultar información básica por período; responsables del proyecto → disponer de datos consistentes sobre usuarios, cursos e inscripciones |
| **Disparador (Trigger)** | El Administrador selecciona la sección "Reportes". |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-16 |
| **Referencias funcionales** | No especificadas en el documento fuente |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Administrador

---

### 1. BREVE DESCRIPCIÓN

Permite que un Administrador consulte información básica del sistema aplicando un rango de fechas mediante los campos Desde y Hasta.

### 2. PRECONDICIONES

- El Administrador debe haber iniciado sesión.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Administrador accede a "Reportes".
2. El sistema muestra los campos "Desde" y "Hasta".
3. El Administrador selecciona una fecha inicial y una fecha final.
4. El Administrador confirma el filtro.
5. El sistema valida que el rango de fechas sea válido. **Reglas aplicables:** **RN-16**.
6. El sistema muestra información básica de usuarios registrados, cursos creados e inscripciones realizadas dentro del rango seleccionado. **Reglas aplicables:** **RN-16**.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **5a. Rango de fechas inválido — A1 (HTTP 400 Bad Request):**
  1. El sistema detecta que la fecha Desde es posterior a la fecha Hasta. **Reglas aplicables:** **RN-16**.
  2. El sistema informa el error y solicita corregir el rango.

* **6a. Sin resultados — A2 (HTTP 200 OK):**
  1. No existen registros para el rango seleccionado.
  2. El sistema informa que no hay datos para mostrar.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- El sistema muestra información básica correspondiente al rango de fechas seleccionado.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Consultar Reportes; A2: Sin resultados. |
| `400` | Bad Request | A1: Rango de fechas inválido. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Matriz de trazabilidad CU-10 → Test

| Paso del CU | Excepción / Código | Test unitario propuesto (Negocio) | Test de integración propuesto (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Flujo principal | `200 OK` | `CU10_Step01_WhenValidState_ContinuesUseCase` | `CU10_Step01_WhenValidRequest_Returns200OK` |
| Paso 2. Flujo principal | `200 OK` | `CU10_Step02_WhenValidState_ContinuesUseCase` | `CU10_Step02_WhenValidRequest_Returns200OK` |
| Paso 3. Flujo principal | `200 OK` | `CU10_Step03_WhenValidState_ContinuesUseCase` | `CU10_Step03_WhenValidRequest_Returns200OK` |
| Paso 4. Flujo principal | `200 OK` | `CU10_Step04_WhenValidState_ContinuesUseCase` | `CU10_Step04_WhenValidRequest_Returns200OK` |
| Paso 5. Flujo principal | `200 OK` | `CU10_Step05_WhenValidState_ContinuesUseCase` | `CU10_Step05_WhenValidRequest_Returns200OK` |
| Paso 6. Flujo principal | `200 OK` | `CU10_Step06_WhenValidState_ContinuesUseCase` | `CU10_Step06_WhenValidRequest_Returns200OK` |
| 5a. Rango de fechas inválido (A1) | `400 Bad Request` | `CU10_Alt01_WhenConditionOccurs_HandlesExpectedBranch` | `CU10_Alt01_WhenConditionOccurs_Returns400BadRequest` |
| 6a. Sin resultados (A2) | `200 OK` | `CU10_Alt02_WhenConditionOccurs_HandlesExpectedBranch` | `CU10_Alt02_WhenConditionOccurs_Returns200OK` |

> Los nombres de tests documentan el contrato esperado y deberán vincularse con la
> suite automatizada cuando exista una implementación backend.
