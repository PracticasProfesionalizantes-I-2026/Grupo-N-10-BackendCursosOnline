# Caso de Uso: Registrarse

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP y los nombres de tests constituyen una propuesta de trazabilidad
> técnica; el documento fuente define actualmente un prototipo frontend académico.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-01 |
| **Nombre** | Registrarse |
| **Actor Principal** | Alumno / Profesor |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Alumno y Profesor → crear una cuenta propia con los datos requeridos; Administrador → mantener usuarios identificados y roles controlados |
| **Disparador (Trigger)** | El usuario selecciona la opción de registro desde la página principal. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-01 (registro público limitado a ALUMNO y PROFESOR); RN-02 (información personal obligatoria) |
| **Referencias funcionales** | No especificadas en el documento fuente |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Alumno / Profesor

---

### 1. BREVE DESCRIPCIÓN

Permite que una persona cree una cuenta pública como Alumno o Profesor, registre sus credenciales y complete la información personal requerida.

### 2. PRECONDICIONES

- El usuario no debe poseer una cuenta registrada con el mismo email.
- El registro público se encuentra disponible únicamente para Alumno y Profesor.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)

1. El usuario selecciona la opción de registro.
2. El sistema solicita seleccionar el tipo de cuenta: Alumno o Profesor. **Reglas aplicables:** **RN-01**.
3. El usuario ingresa email y contraseña.
4. El sistema valida que el email no se encuentre registrado y que los campos obligatorios estén completos.
5. El usuario completa Nombre, Apellido, DNI, Teléfono, Dirección y Código Postal. **Reglas aplicables:** **RN-02**.
6. El sistema valida la información personal. **Reglas aplicables:** **RN-02**.
7. El sistema muestra una revisión de los datos ingresados.
8. El usuario confirma el registro.
9. El sistema registra la cuenta y muestra una confirmación.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **4a. Email ya registrado — A1 (HTTP 409 Conflict):**
  1. El sistema detecta que el email ya se encuentra registrado.
  2. El sistema informa el error y solicita utilizar otro email.

* **6a. Información obligatoria incompleta — A2 (HTTP 400 Bad Request):**
  1. El sistema detecta campos obligatorios incompletos. **Reglas aplicables:** **RN-02**.
  2. El sistema identifica los campos pendientes.
  3. El usuario completa o corrige la información y continúa.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- La cuenta queda registrada con el rol seleccionado.
- Los datos personales obligatorios quedan asociados a la cuenta.
- El usuario queda habilitado para iniciar sesión.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | resultado satisfactorio de Registrarse. |
| `409` | Conflict | A1: Email ya registrado. |
| `400` | Bad Request | A2: Información obligatoria incompleta. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Matriz de trazabilidad CU-01 → Test

| Paso del CU | Excepción / Código | Test unitario propuesto (Negocio) | Test de integración propuesto (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Flujo principal | `201 Created` | `CU01_Step01_WhenValidState_ContinuesUseCase` | `CU01_Step01_WhenValidRequest_Returns201Created` |
| Paso 2. Flujo principal | `201 Created` | `CU01_Step02_WhenValidState_ContinuesUseCase` | `CU01_Step02_WhenValidRequest_Returns201Created` |
| Paso 3. Flujo principal | `201 Created` | `CU01_Step03_WhenValidState_ContinuesUseCase` | `CU01_Step03_WhenValidRequest_Returns201Created` |
| Paso 4. Flujo principal | `201 Created` | `CU01_Step04_WhenValidState_ContinuesUseCase` | `CU01_Step04_WhenValidRequest_Returns201Created` |
| Paso 5. Flujo principal | `201 Created` | `CU01_Step05_WhenValidState_ContinuesUseCase` | `CU01_Step05_WhenValidRequest_Returns201Created` |
| Paso 6. Flujo principal | `201 Created` | `CU01_Step06_WhenValidState_ContinuesUseCase` | `CU01_Step06_WhenValidRequest_Returns201Created` |
| Paso 7. Flujo principal | `201 Created` | `CU01_Step07_WhenValidState_ContinuesUseCase` | `CU01_Step07_WhenValidRequest_Returns201Created` |
| Paso 8. Flujo principal | `201 Created` | `CU01_Step08_WhenValidState_ContinuesUseCase` | `CU01_Step08_WhenValidRequest_Returns201Created` |
| Paso 9. Flujo principal | `201 Created` | `CU01_Step09_WhenValidState_ContinuesUseCase` | `CU01_Step09_WhenValidRequest_Returns201Created` |
| 4a. Email ya registrado (A1) | `409 Conflict` | `CU01_Alt01_WhenConditionOccurs_HandlesExpectedBranch` | `CU01_Alt01_WhenConditionOccurs_Returns409Conflict` |
| 6a. Información obligatoria incompleta (A2) | `400 Bad Request` | `CU01_Alt02_WhenConditionOccurs_HandlesExpectedBranch` | `CU01_Alt02_WhenConditionOccurs_Returns400BadRequest` |

> Los nombres de tests documentan el contrato esperado y deberán vincularse con la
> suite automatizada cuando exista una implementación backend.
