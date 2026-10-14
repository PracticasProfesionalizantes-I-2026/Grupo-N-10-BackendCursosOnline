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
- Para obtener un reporte, la solicitud debe incluir los parámetros `from` y `to`; su presencia se valida en el paso 5 y su ausencia se trata mediante A3.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Administrador accede a "Reportes".
2. El sistema muestra los campos obligatorios "Desde" y "Hasta".
3. El Administrador selecciona una fecha inicial (`from`) y una fecha final (`to`).
4. El Administrador confirma el filtro.
5. El sistema valida primero que estén presentes los parámetros obligatorios `from` y `to`, luego que sus formatos sean válidos y que el rango de fechas sea válido. Si falta alguno de los parámetros, aplica A3. **Reglas aplicables:** **RN-16**.
6. El sistema muestra información básica de usuarios registrados, cursos creados e inscripciones realizadas dentro del rango seleccionado. **Reglas aplicables:** **RN-16**.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **5a. Rango de fechas inválido — A1 (HTTP 400 Bad Request):**
  1. El sistema detecta que la fecha Desde es posterior a la fecha Hasta. **Reglas aplicables:** **RN-16**.
  2. El sistema informa el error y solicita corregir el rango.

* **6a. Sin resultados — A2 (HTTP 200 OK):**
  1. No existen registros para el rango seleccionado.
  2. El sistema informa que no hay datos para mostrar.

* **5b. Fechas obligatorias ausentes — A3 (HTTP 400 Bad Request):**
  1. El sistema detecta que falta `from`, falta `to` o faltan ambos parámetros, antes de validar el formato y el orden de las fechas.
  2. El sistema responde con HTTP `400 Bad Request` y un mensaje comprensible que identifica los parámetros ausentes: "Falta el parámetro obligatorio: from.", "Falta el parámetro obligatorio: to." o "Faltan los parámetros obligatorios: from y to.", según corresponda.
  3. El sistema solicita completar las fechas ausentes y no genera el reporte.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- En el flujo principal, el sistema muestra información básica correspondiente al rango de fechas seleccionado.
- Si se aplica A3, el sistema responde con HTTP `400 Bad Request`, identifica los parámetros ausentes y no genera el reporte.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Consultar Reportes; A2: Sin resultados. |
| `400` | Bad Request | A1: Rango de fechas inválido; A3: Fechas obligatorias ausentes (`from`, `to` o ambos). |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Estado de implementación y comportamiento esperado

- **Comportamiento esperado:** `from` y `to` son obligatorios. Toda omisión debe producir HTTP `400 Bad Request` con un mensaje que identifique los parámetros ausentes, conforme a A3.
- **Implementación actual, según las pruebas de integración de la revisión técnica:** las fechas invertidas (A1) y las fechas con formato inválido se rechazan con HTTP `400`. Si falta `from`, la API puede devolver HTTP `200` utilizando `DateTime.MinValue`; si falta `to`, devuelve HTTP `400` con un mensaje que no representa correctamente la ausencia del parámetro; si faltan ambos, puede devolver HTTP `200`.
- **Pendiente:** la corrección en C# y la cobertura automatizada del contrato de A3. Esta actualización documenta el requisito esperado; no acredita su implementación.

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
| 5b. Falta `from`, con `to` presente y válido (A3) | `400 Bad Request` | No aplica: validación de presencia (Presentación) | Escenario pendiente: omitir `from` y verificar HTTP `400` y el mensaje "Falta el parámetro obligatorio: from.". |
| 5b. Falta `to`, con `from` presente y válido (A3) | `400 Bad Request` | No aplica: validación de presencia (Presentación) | Escenario pendiente: omitir `to` y verificar HTTP `400` y el mensaje "Falta el parámetro obligatorio: to.". |
| 5b. Faltan `from` y `to` (A3) | `400 Bad Request` | No aplica: validación de presencia (Presentación) | Escenario pendiente: omitir ambos parámetros y verificar HTTP `400` y el mensaje "Faltan los parámetros obligatorios: from y to.". |

> Los nombres de tests anteriores se conservan como propuestas de trazabilidad;
> no se han verificado como nombres de pruebas existentes en esta etapa.
> Los tres escenarios de A3 están pendientes de cobertura del contrato esperado y
> deberán vincularse con la suite automatizada cuando se implemente la corrección.
