# Caso de Uso: Consultar Reportes

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y adaptada a la
> sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-10 |
| **Nombre** | Consultar Reportes |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Administrador → consultar información básica por período; Institución → disponer de datos consistentes sobre usuarios, cursos e inscripciones |
| **Disparador (Trigger)** | El Administrador selecciona la sección “Reportes” |
| **Prioridad / Frecuencia** | Media; frecuencia periódica |
| **Reglas de negocio relacionadas** | RN-30 (Desde y Hasta son obligatorios y Desde no puede superar Hasta); RN-31 (el reporte incluye solo registros del rango); RN-32 (un rango sin registros devuelve un resultado vacío válido) |

---

### 1. BREVE DESCRIPCIÓN

Permite que un Administrador consulte información básica de usuarios registrados,
cursos creados e inscripciones realizadas dentro de un rango de fechas.

### 2. PRECONDICIONES

- El Administrador debe haber iniciado sesión con un Token JWT válido.
- La cuenta autenticada debe poseer rol Administrador.
- El Sistema debe disponer de los campos Desde y Hasta para definir el período.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El actor informa una fecha inicial y una fecha final y envía
   `GET /api/reportes?desde={fechaDesde}&hasta={fechaHasta}`.
2. La **Capa de Presentación** valida el formato y la presencia de ambas fechas.
3. La **Capa de Negocio** verifica que Desde no sea posterior a Hasta, aplicando
   **RN-30**.
4. La **Capa de Persistencia** consulta los usuarios registrados, cursos creados e
   inscripciones realizadas dentro del período, conforme a **RN-31**.
5. La **Capa de Negocio** organiza la información básica del reporte.
6. El Sistema devuelve **200 OK** con los datos correspondientes al rango.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **2a. Fechas faltantes o con formato inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 2 falta Desde o Hasta, o alguna fecha no posee el formato
     admitido, la Capa de Presentación rechaza la consulta.
  2. El Sistema devuelve **400 Bad Request** con el detalle del parámetro. Fin del
     caso de uso.

* **3a. Rango de fechas inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 3 Desde es posterior a Hasta, se incumple **RN-30**.
  2. La Capa de Negocio rechaza el rango.
  3. El Sistema devuelve **400 Bad Request** y solicita corregir las fechas. Fin
     del caso de uso.

* **4a. Sin resultados (HTTP 200 OK):**
  1. Si en el Paso 4 no existen registros dentro del rango, se aplica **RN-32**.
  2. La Capa de Negocio construye un resultado vacío válido.
  3. El Sistema devuelve **200 OK** e informa que no hay datos para mostrar. Fin
     del caso de uso.

* **1a. Usuario no autenticado (HTTP 401 Unauthorized):**
  1. Si en el Paso 1 no se presenta un Token JWT válido, el Sistema devuelve
     **401 Unauthorized**. Fin del caso de uso.

* **1b. Rol no autorizado (HTTP 403 Forbidden):**
  1. Si en el Paso 1 la cuenta autenticada no posee rol Administrador, la Capa de
     Negocio restringe la consulta.
  2. El Sistema devuelve **403 Forbidden**. Fin del caso de uso.

### 5. SUB-VARIACIONES

1. El rango puede contener datos de una, dos o las tres categorías disponibles:
   usuarios, cursos e inscripciones; el contrato de respuesta se conserva.

### 6. POSTCONDICIONES

- El Sistema muestra la información básica correspondiente al rango seleccionado.
- Si no existen registros, muestra un resultado vacío válido.
- La consulta no modifica usuarios, cursos ni inscripciones.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | El reporte fue devuelto con datos o con un resultado vacío válido. |
| `400` | Bad Request | Falta una fecha, su formato es inválido o Desde es posterior a Hasta. |
| `401` | Unauthorized | No existe una autenticación válida. |
| `403` | Forbidden | El usuario autenticado no posee rol Administrador. |

### Matriz de trazabilidad CU-10 → Test

| Paso del CU | Excepción / Código | Test unitario (Negocio) | Test de integración (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Enviar rango | `200 OK` | — (entrada HTTP) | `GetReports_WithValidRange_AcceptsParameters` |
| Paso 2. Validar fechas | `200 OK` | `GetReportAsync_WithValidDateFormats_ContinuesQuery` | `GetReports_WithValidDates_Returns200OK` |
| Paso 3. Validar orden | `200 OK` | `GetReportAsync_WithOrderedRange_AllowsQuery` | `GetReports_WithOrderedRange_Returns200OK` |
| Paso 4. Consultar registros | `200 OK` | `GetReportAsync_FiltersRecordsInsideRange` | `GetReports_ReturnsOnlyRecordsInsideRange` |
| Paso 5. Organizar reporte | `200 OK` | `GetReportAsync_GroupsUsersCoursesAndEnrollments` | `GetReports_ReturnsExpectedReportStructure` |
| Paso 6. Responder consulta | `200 OK` | `GetReportAsync_WithResults_ReturnsReport` | `GetReports_WithResults_Returns200OK` |
| 2a. Fechas inválidas | `400 Bad Request` | `GetReportAsync_WithMissingDate_ThrowsValidationException` | `GetReports_WithMissingOrInvalidDate_Returns400BadRequest` |
| 3a. Rango inválido | `400 Bad Request` | `GetReportAsync_WhenFromIsAfterTo_ThrowsInvalidRangeException` | `GetReports_WhenFromIsAfterTo_Returns400BadRequest` |
| 4a. Sin resultados | `200 OK` | `GetReportAsync_WithoutRecords_ReturnsEmptyReport` | `GetReports_WithoutRecords_Returns200WithEmptyReport` |
| 1a. Sin autenticación | `401 Unauthorized` | — (autenticación HTTP) | `GetReports_WithoutToken_Returns401Unauthorized` |
| 1b. Rol no autorizado | `403 Forbidden` | `GetReportAsync_AsNonAdmin_ThrowsForbiddenException` | `GetReports_AsTeacher_Returns403Forbidden` |

> Los nombres de tests establecen el contrato de trazabilidad del caso de uso y
> deberán coincidir con la suite automatizada cuando se implemente.
