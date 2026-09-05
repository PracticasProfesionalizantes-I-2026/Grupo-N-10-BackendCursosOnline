# Caso de Uso: Gestionar Usuarios

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP y los nombres de tests constituyen una propuesta de trazabilidad
> técnica; el documento fuente define actualmente un prototipo frontend académico.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-09 |
| **Nombre** | Gestionar Usuarios |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Administrador → consultar usuarios y gestionar cuentas internas; usuarios → conservar datos y roles coherentes |
| **Disparador (Trigger)** | El Administrador selecciona la gestión de usuarios desde su panel. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-01 |
| **Referencias funcionales** | RF-06 y RF-32; RN-01; permiso de gestión de usuarios. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Administrador

---

### 1. BREVE DESCRIPCIÓN

Permite que un Administrador consulte usuarios y gestione internamente cuentas de Administrador. La gestión de cursos se desarrolla en CU-03, CU-05, CU-14 y CU-15; su auditoría corresponde al CU-04.

### 2. PRECONDICIONES

- El usuario debe haber iniciado sesión como Administrador.
- La gestión de cuentas de Administrador se realiza internamente y no está disponible en el registro público.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Administrador accede a su panel y selecciona la gestión de usuarios.
2. El sistema muestra los usuarios registrados y sus roles.
3. El Administrador selecciona un usuario para consultar su información.
4. El sistema muestra los datos de la cuenta y el rol asociado.
5. El Administrador revisa la información y vuelve al listado al terminar.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **3a. Crear una cuenta de Administrador por gestión interna — A1 (HTTP 201 Created):**
  1. El Administrador selecciona la creación interna de una cuenta de Administrador. **Reglas aplicables:** **RN-01**.
  2. El sistema solicita los datos de identificación y acceso de la cuenta, incluidos email y contraseña, y presenta el rol Administrador.
  3. El Administrador completa los datos y confirma la creación.
  4. El sistema verifica los datos obligatorios y que el email no esté registrado.
  5. El sistema registra la cuenta con rol Administrador y muestra una confirmación.

* **3b. Datos incompletos o email ya registrado en la creación interna — A2 (HTTP 400 Bad Request):**
  1. Durante A1, el sistema identifica los datos faltantes o el email ya registrado. **Reglas aplicables:** **RN-01**.
  2. No crea la cuenta; el Administrador corrige los datos y vuelve a confirmar.

* **1a. Acceso con un rol sin permiso — A3 (HTTP 403 Forbidden):**
  1. El sistema detecta que el usuario no es Administrador.
  2. Restringe el acceso a la gestión de usuarios.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- El Administrador consulta la información y el rol del usuario seleccionado.
- Si crea una cuenta de Administrador mediante A1, la cuenta queda asociada a ese rol y puede utilizar CU-02.
- Si abandona la operación o faltan los datos de acceso requeridos, no se crea la cuenta.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Gestionar Usuarios. |
| `201` | Created | A1: Crear una cuenta de Administrador por gestión interna. |
| `400` | Bad Request | A2: Datos incompletos o email ya registrado en la creación interna. |
| `403` | Forbidden | A3: Acceso con un rol sin permiso. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Matriz de trazabilidad CU-09 → Test

| Paso del CU | Excepción / Código | Test unitario propuesto (Negocio) | Test de integración propuesto (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Flujo principal | `200 OK` | `CU09_Step01_WhenValidState_ContinuesUseCase` | `CU09_Step01_WhenValidRequest_Returns200OK` |
| Paso 2. Flujo principal | `200 OK` | `CU09_Step02_WhenValidState_ContinuesUseCase` | `CU09_Step02_WhenValidRequest_Returns200OK` |
| Paso 3. Flujo principal | `200 OK` | `CU09_Step03_WhenValidState_ContinuesUseCase` | `CU09_Step03_WhenValidRequest_Returns200OK` |
| Paso 4. Flujo principal | `200 OK` | `CU09_Step04_WhenValidState_ContinuesUseCase` | `CU09_Step04_WhenValidRequest_Returns200OK` |
| Paso 5. Flujo principal | `200 OK` | `CU09_Step05_WhenValidState_ContinuesUseCase` | `CU09_Step05_WhenValidRequest_Returns200OK` |
| 3a. Crear una cuenta de Administrador por gestión interna (A1) | `201 Created` | `CU09_Alt01_WhenConditionOccurs_HandlesExpectedBranch` | `CU09_Alt01_WhenConditionOccurs_Returns201Created` |
| 3b. Datos incompletos o email ya registrado en la creación interna (A2) | `400 Bad Request` | `CU09_Alt02_WhenConditionOccurs_HandlesExpectedBranch` | `CU09_Alt02_WhenConditionOccurs_Returns400BadRequest` |
| 1a. Acceso con un rol sin permiso (A3) | `403 Forbidden` | `CU09_Alt03_WhenConditionOccurs_HandlesExpectedBranch` | `CU09_Alt03_WhenConditionOccurs_Returns403Forbidden` |

> Los nombres de tests documentan el contrato esperado y deberán vincularse con la
> suite automatizada cuando exista una implementación backend.
