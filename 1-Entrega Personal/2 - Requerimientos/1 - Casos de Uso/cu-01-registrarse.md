# Caso de Uso: Registrarse

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y adaptada a la
> sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-01 |
| **Nombre** | Registrarse |
| **Actor Principal** | Alumno / Profesor |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Alumno y Profesor → crear una cuenta propia y acceder a las funciones de su rol; Administración → mantener cuentas identificadas y datos personales completos |
| **Disparador (Trigger)** | La persona selecciona la opción de registro desde la página principal |
| **Prioridad / Frecuencia** | Alta; frecuencia media |
| **Reglas de negocio relacionadas** | RN-01 (email único); RN-02 (registro público limitado a Alumno y Profesor); RN-03 (credenciales y datos personales obligatorios) |

---

### 1. BREVE DESCRIPCIÓN

Permite que una persona cree una cuenta pública como Alumno o Profesor, registre
sus credenciales y complete la información personal requerida.

### 2. PRECONDICIONES

- La persona no debe poseer una cuenta registrada con el mismo email.
- El registro público debe encontrarse habilitado.
- El rol solicitado debe ser Alumno o Profesor, conforme a **RN-02**.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)

1. El actor envía una petición `POST /api/auth/registro` con el rol elegido, email,
   contraseña, nombre, apellido, DNI, teléfono, dirección y código postal.
2. La **Capa de Presentación** valida el formato del JSON, la presencia de los
   campos obligatorios y que el rol sea Alumno o Profesor, aplicando **RN-02** y
   **RN-03**.
3. La **Capa de Negocio** normaliza el email y verifica que no exista otra cuenta
   con el mismo valor, aplicando **RN-01**.
4. La **Capa de Negocio** prepara la cuenta con el rol seleccionado y asocia los
   datos personales validados.
5. La **Capa de Persistencia** registra la cuenta y su información personal.
6. El Sistema devuelve **201 Created** con el identificador de la cuenta, el rol y
   los datos necesarios para confirmar el registro.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo está vacío, tiene sintaxis inválida o no respeta el
     esquema esperado, la Capa de Presentación rechaza la petición.
  2. El Sistema devuelve **400 Bad Request** y no registra información. Fin del
     caso de uso.

* **2a. Rol público no permitido (HTTP 400 Bad Request):**
  1. Si en el Paso 2 se solicita registrar un rol distinto de Alumno o Profesor,
     se incumple **RN-02**.
  2. La Capa de Presentación rechaza el valor recibido.
  3. El Sistema devuelve **400 Bad Request** indicando que el rol no está
     disponible para registro público. Fin del caso de uso.

* **2b. Información obligatoria incompleta o inválida (HTTP 400 Bad Request):**
  1. Si en el Paso 2 falta un dato obligatorio o su formato es inválido, se
     incumple **RN-03**.
  2. La Capa de Presentación identifica los campos pendientes o incorrectos.
  3. El Sistema devuelve **400 Bad Request** con el detalle de validación. Fin del
     caso de uso.

* **3a. Email ya registrado (HTTP 409 Conflict):**
  1. Si en el Paso 3 ya existe una cuenta con el email normalizado, se incumple
     **RN-01**.
  2. La Capa de Negocio interrumpe el alta.
  3. El Sistema devuelve **409 Conflict** e informa que debe utilizarse otro
     email. Fin del caso de uso.

* **5a. Error interno de persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 5 no es posible guardar la cuenta, la operación se revierte.
  2. El Sistema registra el error técnico.
  3. El Sistema devuelve **500 Internal Server Error** sin crear una cuenta
     parcial. Fin del caso de uso.

### 5. SUB-VARIACIONES

1. El actor puede seleccionar Alumno o Profesor; el esquema de credenciales y
   datos personales es el mismo y solo cambia el rol asignado.

### 6. POSTCONDICIONES

- La cuenta queda registrada con el rol Alumno o Profesor.
- Los datos personales obligatorios quedan asociados a la cuenta.
- El usuario queda habilitado para iniciar sesión.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | La cuenta y sus datos personales fueron registrados correctamente. |
| `400` | Bad Request | El JSON, el rol o los datos obligatorios no cumplen el contrato de entrada. |
| `409` | Conflict | El email ya pertenece a otra cuenta, en conflicto con RN-01. |
| `500` | Internal Server Error | Ocurrió un error técnico no controlado al persistir la cuenta. |

### Matriz de trazabilidad CU-01 → Test

| Paso del CU | Excepción / Código | Test unitario (Negocio) | Test de integración (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Enviar solicitud | `201 Created` | — (entrada HTTP) | `Register_WithValidRequest_AcceptsPayload` |
| Paso 2. Validar esquema y rol | `201 Created` | `RegisterAsync_WithAllowedRole_ContinuesRegistration` | `Register_WithValidData_Returns201Created` |
| Paso 3. Verificar email único | `201 Created` | `RegisterAsync_WithUniqueEmail_AllowsRegistration` | `Register_WithUniqueEmail_Returns201Created` |
| Paso 4. Preparar cuenta y perfil | `201 Created` | `RegisterAsync_WithValidData_AssignsRoleAndProfile` | `Register_WithValidData_ReturnsExpectedAccount` |
| Paso 5. Persistir cuenta | `201 Created` | `RegisterAsync_WithValidData_PersistsAccount` | `Register_WithValidData_CreatesPersistentAccount` |
| Paso 6. Responder creación | `201 Created` | `RegisterAsync_WithValidData_ReturnsCreatedAccount` | `Register_WithValidData_Returns201Created` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding) | `Register_WithInvalidJson_Returns400BadRequest` |
| 2a. Rol no permitido | `400 Bad Request` | `RegisterAsync_WithForbiddenPublicRole_ThrowsValidationException` | `Register_WithAdminRole_Returns400BadRequest` |
| 2b. Información inválida | `400 Bad Request` | `RegisterAsync_WithInvalidRequiredData_ThrowsValidationException` | `Register_WithMissingRequiredField_Returns400BadRequest` |
| 3a. Email duplicado | `409 Conflict` | `RegisterAsync_WhenEmailExists_ThrowsEmailConflictException` | `Register_WithExistingEmail_Returns409Conflict` |
| 5a. Error de persistencia | `500 Internal Server Error` | `RegisterAsync_WhenRepositoryFails_PropagatesException` | `Register_WhenPersistenceFails_Returns500InternalServerError` |

> Los nombres de tests establecen el contrato de trazabilidad del caso de uso y
> deberán coincidir con la suite automatizada cuando se implemente.
