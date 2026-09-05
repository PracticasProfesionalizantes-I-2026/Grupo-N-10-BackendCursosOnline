# Caso de Uso: Iniciar Sesión

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP y los nombres de tests constituyen una propuesta de trazabilidad
> técnica; el documento fuente define actualmente un prototipo frontend académico.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-02 |
| **Nombre** | Iniciar Sesión |
| **Actor Principal** | Alumno / Profesor / Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Alumno, Profesor y Administrador → acceder a las funciones permitidas para su rol; responsables del sistema → restringir accesos indebidos |
| **Disparador (Trigger)** | El usuario selecciona la opción "Iniciar Sesión". |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | No se identifica una RN específica; aplican RF-05 a RF-07, RF-29 a RF-33 y la matriz de permisos |
| **Referencias funcionales** | RF-05 a RF-07 y RF-29 a RF-33; matriz de permisos. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Alumno / Profesor / Administrador

---

### 1. BREVE DESCRIPCIÓN

Permite que un usuario registrado acceda al sistema y sea dirigido al panel correspondiente según su rol.

### 2. PRECONDICIONES

- El usuario debe poseer una cuenta registrada y habilitada.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El usuario selecciona "Iniciar Sesión".
2. El sistema solicita email y contraseña.
3. El usuario ingresa sus credenciales.
4. El sistema valida las credenciales.
5. El sistema identifica el rol asociado a la cuenta.
6. El sistema habilita las funcionalidades correspondientes.
7. El sistema redirige al Panel de Alumno, Profesor o Administrador según corresponda y muestra sus accesos permitidos.
8. El Alumno puede elegir cursos disponibles (CU-11), solicitudes e inscripciones (CU-12) o progreso (CU-08). El Profesor puede elegir sus cursos y solicitudes de auditoría (CU-03 y CU-05) o seguimiento de alumnos (CU-08). El Administrador puede elegir usuarios, cursos, auditorías, inscripciones y reportes (CU-09, CU-03, CU-05, CU-04, CU-07 y CU-10).
9. El usuario selecciona una función y el sistema inicia el caso de uso correspondiente, manteniendo las restricciones de su rol.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **4a. Credenciales incorrectas — A1 (HTTP 401 Unauthorized):**
  1. El sistema detecta que las credenciales no son válidas.
  2. El sistema informa el error.
  3. El usuario puede volver a ingresar sus credenciales.

* **4b. Campos vacíos — A2 (HTTP 400 Bad Request):**
  1. El sistema detecta campos obligatorios sin completar.
  2. El sistema informa qué información falta y no permite continuar.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- El usuario accede al sistema.
- El sistema identifica su rol y muestra únicamente las funcionalidades permitidas.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Iniciar Sesión. |
| `401` | Unauthorized | A1: Credenciales incorrectas. |
| `400` | Bad Request | A2: Campos vacíos. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Matriz de trazabilidad CU-02 → Test

| Paso del CU | Excepción / Código | Test unitario propuesto (Negocio) | Test de integración propuesto (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Flujo principal | `200 OK` | `CU02_Step01_WhenValidState_ContinuesUseCase` | `CU02_Step01_WhenValidRequest_Returns200OK` |
| Paso 2. Flujo principal | `200 OK` | `CU02_Step02_WhenValidState_ContinuesUseCase` | `CU02_Step02_WhenValidRequest_Returns200OK` |
| Paso 3. Flujo principal | `200 OK` | `CU02_Step03_WhenValidState_ContinuesUseCase` | `CU02_Step03_WhenValidRequest_Returns200OK` |
| Paso 4. Flujo principal | `200 OK` | `CU02_Step04_WhenValidState_ContinuesUseCase` | `CU02_Step04_WhenValidRequest_Returns200OK` |
| Paso 5. Flujo principal | `200 OK` | `CU02_Step05_WhenValidState_ContinuesUseCase` | `CU02_Step05_WhenValidRequest_Returns200OK` |
| Paso 6. Flujo principal | `200 OK` | `CU02_Step06_WhenValidState_ContinuesUseCase` | `CU02_Step06_WhenValidRequest_Returns200OK` |
| Paso 7. Flujo principal | `200 OK` | `CU02_Step07_WhenValidState_ContinuesUseCase` | `CU02_Step07_WhenValidRequest_Returns200OK` |
| Paso 8. Flujo principal | `200 OK` | `CU02_Step08_WhenValidState_ContinuesUseCase` | `CU02_Step08_WhenValidRequest_Returns200OK` |
| Paso 9. Flujo principal | `200 OK` | `CU02_Step09_WhenValidState_ContinuesUseCase` | `CU02_Step09_WhenValidRequest_Returns200OK` |
| 4a. Credenciales incorrectas (A1) | `401 Unauthorized` | `CU02_Alt01_WhenConditionOccurs_HandlesExpectedBranch` | `CU02_Alt01_WhenConditionOccurs_Returns401Unauthorized` |
| 4b. Campos vacíos (A2) | `400 Bad Request` | `CU02_Alt02_WhenConditionOccurs_HandlesExpectedBranch` | `CU02_Alt02_WhenConditionOccurs_Returns400BadRequest` |

> Los nombres de tests documentan el contrato esperado y deberán vincularse con la
> suite automatizada cuando exista una implementación backend.
