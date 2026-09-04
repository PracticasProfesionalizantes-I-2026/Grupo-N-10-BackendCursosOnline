# Caso de Uso: Iniciar Sesión

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y adaptada a la
> sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-02 |
| **Nombre** | Iniciar Sesión |
| **Actor Principal** | Alumno / Profesor / Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Usuario → acceder de forma segura; Administración → impedir accesos no autorizados; Sistema → habilitar únicamente las funciones del rol autenticado |
| **Disparador (Trigger)** | El usuario selecciona la opción “Iniciar Sesión” |
| **Prioridad / Frecuencia** | Alta; frecuencia muy alta |
| **Reglas de negocio relacionadas** | RN-04 (la cuenta debe estar registrada y habilitada); RN-05 (las credenciales deben ser válidas); RN-06 (acceso limitado por rol) |

---

### 1. BREVE DESCRIPCIÓN

Permite que un usuario registrado acceda a Lumen y sea dirigido al panel
correspondiente según su rol.

### 2. PRECONDICIONES

- El usuario debe poseer una cuenta registrada y habilitada, conforme a **RN-04**.
- El servicio de autenticación debe encontrarse disponible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El actor envía una petición `POST /api/auth/inicio-sesion` con su email y
   contraseña.
2. La **Capa de Presentación** valida el formato del JSON y que ambos campos estén
   completos.
3. La **Capa de Negocio** localiza la cuenta y verifica que esté habilitada,
   aplicando **RN-04**.
4. La **Capa de Negocio** verifica las credenciales, aplicando **RN-05**.
5. El Sistema identifica el rol de la cuenta y determina las funciones permitidas,
   aplicando **RN-06**.
6. El Sistema genera el estado de autenticación correspondiente.
7. El Sistema devuelve **200 OK** con la información necesaria para acceder y el
   rol que determina el panel de Alumno, Profesor o Administrador.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo está vacío o no es un JSON válido, la Capa de
     Presentación rechaza la petición.
  2. El Sistema devuelve **400 Bad Request**. Fin del caso de uso.

* **2a. Campos vacíos (HTTP 400 Bad Request):**
  1. Si en el Paso 2 falta el email o la contraseña, la Capa de Presentación
     identifica la información pendiente.
  2. El Sistema devuelve **400 Bad Request** y no inicia la autenticación. Fin del
     caso de uso.

* **3a. Cuenta inexistente (HTTP 401 Unauthorized):**
  1. Si en el Paso 3 no existe una cuenta asociada al email, no puede comprobarse
     **RN-05**.
  2. La Capa de Negocio rechaza la autenticación sin revelar si el email existe.
  3. El Sistema devuelve **401 Unauthorized** con un mensaje genérico de
     credenciales incorrectas. Fin del caso de uso.

* **3b. Cuenta no habilitada (HTTP 403 Forbidden):**
  1. Si en el Paso 3 la cuenta existe pero no está habilitada, se incumple
     **RN-04**.
  2. La Capa de Negocio impide el acceso.
  3. El Sistema devuelve **403 Forbidden**. Fin del caso de uso.

* **4a. Contraseña incorrecta (HTTP 401 Unauthorized):**
  1. Si en el Paso 4 la contraseña no corresponde a la cuenta, se incumple
     **RN-05**.
  2. La Capa de Negocio rechaza la autenticación.
  3. El Sistema devuelve **401 Unauthorized** con un mensaje genérico de
     credenciales incorrectas. Fin del caso de uso.

### 5. SUB-VARIACIONES

1. El resultado conserva el mismo código HTTP para los tres roles; el rol devuelto
   determina si se habilita el panel de Alumno, Profesor o Administrador.

### 6. POSTCONDICIONES

- El usuario posee un estado de autenticación activo.
- El Sistema identifica su rol.
- El usuario puede acceder únicamente a las funcionalidades permitidas por
  **RN-06**.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Las credenciales fueron verificadas y la autenticación fue creada. |
| `400` | Bad Request | El JSON es inválido o faltan campos obligatorios. |
| `401` | Unauthorized | La cuenta no existe o las credenciales no son válidas. |
| `403` | Forbidden | La cuenta existe pero no está habilitada para acceder. |

### Matriz de trazabilidad CU-02 → Test

| Paso del CU | Excepción / Código | Test unitario (Negocio) | Test de integración (HTTP) |
| --- | --- | --- | --- |
| Paso 1. Enviar credenciales | `200 OK` | — (entrada HTTP) | `Login_WithValidRequest_AcceptsCredentials` |
| Paso 2. Validar esquema | `200 OK` | — (validación de Presentación) | `Login_WithValidPayload_ContinuesAuthentication` |
| Paso 3. Verificar cuenta habilitada | `200 OK` | `LoginAsync_WithEnabledAccount_ContinuesAuthentication` | `Login_WithEnabledAccount_Returns200OK` |
| Paso 4. Verificar credenciales | `200 OK` | `LoginAsync_WithValidCredentials_ReturnsAuthenticatedUser` | `Login_WithValidCredentials_Returns200OK` |
| Paso 5. Identificar rol | `200 OK` | `LoginAsync_WithKnownRole_ReturnsRolePermissions` | `Login_WithValidUser_ReturnsAssignedRole` |
| Paso 6. Crear autenticación | `200 OK` | `LoginAsync_WithValidCredentials_CreatesAuthenticationState` | `Login_WithValidCredentials_ReturnsAuthenticationData` |
| Paso 7. Responder acceso | `200 OK` | `LoginAsync_WithValidCredentials_ReturnsExpectedResult` | `Login_WithValidCredentials_Returns200OK` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding) | `Login_WithInvalidJson_Returns400BadRequest` |
| 2a. Campos vacíos | `400 Bad Request` | `LoginAsync_WithEmptyCredentials_ThrowsValidationException` | `Login_WithMissingCredentials_Returns400BadRequest` |
| 3a. Cuenta inexistente | `401 Unauthorized` | `LoginAsync_WithUnknownEmail_ThrowsInvalidCredentialsException` | `Login_WithUnknownEmail_Returns401Unauthorized` |
| 3b. Cuenta no habilitada | `403 Forbidden` | `LoginAsync_WithDisabledAccount_ThrowsAccountDisabledException` | `Login_WithDisabledAccount_Returns403Forbidden` |
| 4a. Contraseña incorrecta | `401 Unauthorized` | `LoginAsync_WithWrongPassword_ThrowsInvalidCredentialsException` | `Login_WithWrongPassword_Returns401Unauthorized` |

> Los nombres de tests establecen el contrato de trazabilidad del caso de uso y
> deberán coincidir con la suite automatizada cuando se implemente.
