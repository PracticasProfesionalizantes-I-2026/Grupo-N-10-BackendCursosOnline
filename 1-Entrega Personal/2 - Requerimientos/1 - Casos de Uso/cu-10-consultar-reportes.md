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

Permite que un Administrador consulte información básica del sistema aplicando un rango de fechas mediante los campos obligatorios Desde (`from`) y Hasta (`to`).

### 2. PRECONDICIONES

- El Administrador debe haber iniciado sesión.
- Para obtener un reporte, la solicitud debe incluir los parámetros `from` y `to`; si no existen errores de conversión, su presencia se valida en el paso 5 y su ausencia se trata mediante A3.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Administrador accede a "Reportes".
2. El sistema muestra los campos obligatorios "Desde" y "Hasta".
3. El Administrador selecciona una fecha inicial (`from`) y una fecha final (`to`).
4. El Administrador confirma el filtro.
5. El sistema exige los parámetros obligatorios `from` y `to`. Si una fecha recibida tiene formato inválido, ASP.NET Core puede rechazar la solicitud con HTTP `400 Bad Request` y `ValidationProblemDetails` antes de ejecutar el controlador. Si no existen errores de conversión, el controlador valida la presencia de ambas fechas (A3) y el sistema valida el orden del rango (A1). **Reglas aplicables:** **RN-16**.
6. El sistema muestra información básica de usuarios registrados, cursos creados e inscripciones realizadas dentro del rango seleccionado. **Reglas aplicables:** **RN-16**.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **5a. Rango de fechas inválido — A1 (HTTP 400 Bad Request):**
  1. El sistema detecta que la fecha Desde es posterior a la fecha Hasta. **Reglas aplicables:** **RN-16**.
  2. El sistema informa el error y solicita corregir el rango.

* **6a. Sin resultados — A2 (HTTP 200 OK):**
  1. No existen registros para el rango seleccionado.
  2. El sistema informa que no hay datos para mostrar.

* **5b. Fechas obligatorias ausentes — A3 (HTTP 400 Bad Request):**
  1. Si no existen errores de conversión de las fechas recibidas, el sistema detecta que falta `from`, falta `to` o faltan ambos parámetros, antes de validar el orden de las fechas.
  2. El controlador utiliza `LumenValidationException` y el sistema responde con HTTP `400 Bad Request` en el formato JSON existente `{ code, message }`, con un mensaje comprensible que identifica los parámetros ausentes: "Falta el parámetro obligatorio: from.", "Falta el parámetro obligatorio: to." o "Faltan los parámetros obligatorios: from y to.", según corresponda.
  3. El sistema solicita completar las fechas ausentes y no genera el reporte.

> **Prioridad de validación:** si una fecha tiene formato inválido y la otra está ausente, ASP.NET Core puede responder primero con HTTP `400 Bad Request` y `ValidationProblemDetails`, identificando el parámetro cuyo formato es inválido. No se exige informar simultáneamente errores de formato y de ausencia; en ningún caso se genera el reporte ni se consulta el repositorio de reportes.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- En el flujo principal, el sistema muestra información básica correspondiente al rango de fechas seleccionado.
- Si se aplica A3, el sistema responde con HTTP `400 Bad Request`, identifica los parámetros ausentes y no genera el reporte.
- Si existen errores de conversión, el sistema responde con HTTP `400 Bad Request` y `ValidationProblemDetails`, identifica el parámetro inválido y no genera el reporte.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Consultar Reportes; A2: Sin resultados. |
| `400` | Bad Request | A1: Rango de fechas inválido; A3: Fechas obligatorias ausentes sin errores de conversión (`from`, `to` o ambos); errores de conversión, incluidos los escenarios combinados de formato inválido y ausencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Estado de implementación y comportamiento esperado

- **Comportamiento esperado:** `from` y `to` son obligatorios. Si no existen errores de conversión y falta alguno de los parámetros, se aplica A3 con HTTP `400 Bad Request` y un mensaje que identifica los parámetros ausentes. Los errores de formato pueden rechazarse primero mediante `ValidationProblemDetails`, incluso si falta la otra fecha; no se exige acumular errores de formato y de ausencia.
- **Implementación actual:** el controlador recibe fechas anulables para detectar ausencias y utiliza `LumenValidationException` antes de llamar al servicio. ASP.NET Core mantiene el rechazo automático por errores de conversión. El servicio conserva la validación de fechas invertidas (A1) y el filtrado inclusivo del día Hasta. A2 mantiene el resultado HTTP `200 OK` cuando no hay datos.
- **Antecedente de la revisión técnica previa a la mejora:** si faltaba `from`, la API podía devolver HTTP `200` utilizando `DateTime.MinValue`; si faltaba `to`, devolvía HTTP `400` con un mensaje que no representaba correctamente su ausencia; si faltaban ambos, podía devolver HTTP `200`. La validación actual de presencia evita estos comportamientos.

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
| 5b. Falta `from`, con `to` presente y válido (A3) | `400 Bad Request` | No aplica: validación de presencia (Presentación) | `Summary_WithMissingDates_ReturnsValidationError_WithoutQueryingReports` (`query: "?to=2026-10-05"`): verifica HTTP `400`, código `LumenValidationException`, mensaje "Falta el parámetro obligatorio: from." y cero llamadas al repositorio. |
| 5b. Falta `to`, con `from` presente y válido (A3) | `400 Bad Request` | No aplica: validación de presencia (Presentación) | `Summary_WithMissingDates_ReturnsValidationError_WithoutQueryingReports` (`query: "?from=2026-10-01"`): verifica HTTP `400`, código `LumenValidationException`, mensaje "Falta el parámetro obligatorio: to." y cero llamadas al repositorio. |
| 5b. Faltan `from` y `to` (A3) | `400 Bad Request` | No aplica: validación de presencia (Presentación) | `Summary_WithMissingDates_ReturnsValidationError_WithoutQueryingReports` (`query: ""`): verifica HTTP `400`, código `LumenValidationException`, mensaje "Faltan los parámetros obligatorios: from y to." y cero llamadas al repositorio. |
| Paso 5. `from=not-a-date` y `to` ausente | `400 Bad Request` | No aplica: validación de conversión (Presentación) | `Summary_WithInvalidDateAndMissingOtherDate_ReturnsBadRequest_WithoutQueryingReports` (`invalidParameter: "from"`): verifica HTTP `400`, `ValidationProblemDetails`, error asociado a `from` y cero llamadas al repositorio. |
| Paso 5. `to=not-a-date` y `from` ausente | `400 Bad Request` | No aplica: validación de conversión (Presentación) | `Summary_WithInvalidDateAndMissingOtherDate_ReturnsBadRequest_WithoutQueryingReports` (`invalidParameter: "to"`): verifica HTTP `400`, `ValidationProblemDetails`, error asociado a `to` y cero llamadas al repositorio. |

> Los nombres de tests del flujo principal, A1 y A2 se conservan como propuestas
> de trazabilidad; no se han verificado como nombres de pruebas existentes en esta etapa.
> Las filas de A3 y de escenarios combinados referencian métodos existentes y sus
> datos de prueba en `ReportsIntegrationTests.cs`; no se modifican las pruebas anteriores.
