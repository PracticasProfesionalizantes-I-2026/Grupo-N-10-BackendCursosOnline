# Caso de Uso: Iniciar Sesión

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-02 (credenciales válidas) y RN-06 (normalización y validación de campos obligatorios) **implementadas** en el código; cada caso borde cuenta con su test unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-02 |
| **Nombre** | Iniciar Sesión |
| **Actor Principal** | Alumno / Profesor / Administrador |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Alumno/Profesor/Admin → acceder a su panel correspondiente; Administración → auditar accesos; Sistema → autenticación segura y autorización por roles |
| **Disparador (Trigger)** | El usuario selecciona la opción "Iniciar Sesión" desde la página principal de la plataforma |
| **Prioridad / Frecuencia** | Alta; frecuencia muy alta (acceso diario de usuarios) |
| **Reglas de negocio relacionadas** | RN-02 (credenciales válidas para login); RN-06 (normalización de espacios y validación de campos obligatorios) |

---

### 1. BREVE DESCRIPCIÓN
Permite que un usuario registrado acceda a la plataforma utilizando sus credenciales (email y contraseña) y sea redirigido al panel correspondiente según su rol (Alumno, Profesor o Administrador), habilitando las funcionalidades visibles restringidas según dicho rol.

### 2. PRECONDICIONES
- El usuario debe encontrarse previamente registrado en el sistema (CU-01 completado).
- El usuario debe poseer credenciales válidas (email y contraseña correctos).
- El sistema debe encontrarse disponible y con la Capa de Persistencia accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor envía una petición al endpoint `POST /api/auth/login` con un JSON que contiene las credenciales: `email` y `contraseña`.
2. La **Capa de Presentación** (`AuthController.Login`) valida que el JSON sea estructuralmente correcto y que los campos requeridos estén presentes y no vacíos (data annotations `[Required]` sobre `LoginDTO`).
3. La **Capa de Negocio** (`AuthService.LoginAsync`) aplica la regla **RN-06**: normaliza el email aplicando `Trim()` y verifica que no quede vacío tras la normalización.
4. La **Capa de Negocio** busca el usuario por email en la base de datos.
5. La **Capa de Negocio** verifica la regla **RN-02**: valida que la contraseña proporcionada coincida con el hash almacenado (verificación bcrypt/Argon2).
6. La **Capa de Negocio** identifica el rol asociado al usuario (Alumno, Profesor, Administrador).
7. La **Capa de Negocio** genera un Token JWT firmado con los claims correspondientes (sub=usuarioId, email, rol, permisos).
8. El Sistema devuelve un código **200 OK** con el Token JWT y la información del usuario (ID, email, usuario, rol, nombre).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota, cuerpo vacío o formato incorrecto).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición por error de esquema.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el JSON no incluye `email` o `contraseña`.
  2. El Sistema (Capa de Presentación) rechaza la petición por error de validación (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** detallando el campo faltante con su mensaje, ej: `"El email es obligatorio."`. Fin del caso de uso.

* **2b. Campo con cadena vacía (HTTP 400 Bad Request):**
  1. Si en el Paso 2 un campo obligatorio llega como cadena de longitud cero (`""`).
  2. El Sistema (Capa de Presentación) rechaza la petición por validación de esquema (`[Required]` no permite strings vacíos por defecto).
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje del campo correspondiente. Fin del caso de uso.

* **2c. Campo con solo espacios en blanco (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el campo `email` llega con únicamente espacios (ej. `"   "`): la regla **RN-06** normaliza aplicando `Trim()`; si el valor resultante queda vacío, el Sistema rechaza la petición.
  2. El Sistema (Capa de Negocio) lanza una `ValidationException`.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje: `"El email es obligatorio."`. Fin del caso de uso.

* **2d. Longitud excesiva de un campo (HTTP 400 Bad Request):**
  1. Si en el Paso 2 algún campo supera la longitud máxima definida en el DTO (`[MaxLength]`: email 256, contraseña 256).
  2. El Sistema (Capa de Presentación) rechaza la petición por validación de esquema (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** indicando el límite excedido. Fin del caso de uso.

* **4a. Usuario no encontrado (HTTP 401 Unauthorized):**
  1. Si en el Paso 4 el email proporcionado no existe en la base de datos.
  2. La **Capa de Negocio** no encuentra la entidad correspondiente.
  3. Por seguridad (evitar enumeración de usuarios), el Sistema devuelve un código **401 Unauthorized** con mensaje genérico: `"Credenciales inválidas."`. Fin del caso de uso.

* **5a. Contraseña incorrecta (HTTP 401 Unauthorized):**
  1. Si en el Paso 5 la verificación de la contraseña falla (hash no coincide), violando la regla **RN-02**.
  2. La **Capa de Negocio** lanza una `InvalidCredentialsException`.
  3. El Sistema devuelve un código **401 Unauthorized** con mensaje: `"Credenciales inválidas."`. Fin del caso de uso.

* **7a. Error interno al generar token (HTTP 500 Internal Server Error):**
  1. Si en el Paso 7 ocurre un error técnico al firmar el JWT (clave de firma no configurada, error de librería).
  2. El Sistema interrumpe la operación y registra el error.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El actor puede enviar el JSON desde el panel web, desde la colección de Bruno (`POST Login.bru`) o desde un cliente HTTP (Postman, Swagger/Scalar).
2. En todas las variantes el esquema del cuerpo y el resultado (`200 OK` con JWT) son idénticos.
3. El Token JWT debe incluirse en el header `Authorization: Bearer <token>` en peticiones subsiguientes.

### 6. POSTCONDICIONES
- El usuario accede correctamente al sistema con un Token JWT válido.
- El sistema identifica el rol asociado a la cuenta (claim `rol` en el token).
- El usuario puede acceder a los endpoints protegidos según su rol (autorización basada en claims).
- Las funcionalidades visibles en el frontend quedan restringidas según el rol del usuario (Alumno → Panel Alumno, Profesor → Panel Profesor, Administrador → Panel Admin).
- Se registra el evento de inicio de sesión en logs de auditoría (timestamp, usuarioId, IP).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Autenticación exitosa; respuesta con Token JWT y datos de usuario. |
| `400` | Bad Request | Fallo en la validación de esquema o sintaxis del JSON recibido (campos faltantes, vacíos, solo espacios, longitud excesiva). |
| `401` | Unauthorized | Credenciales inválidas (usuario no existe o contraseña incorrecta) — mensaje genérico por seguridad. |
| `500` | Internal Server Error | Error técnico no controlado al generar el Token JWT. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** campos obligatorios, cadena vacía, límites de longitud (`[Required]`/`[MaxLength]` sobre `LoginDTO` + `ModelState.IsValid` en el controller), y formato del JSON (model binding).
- **Verificación (Negocio, → 400/401):** RN-06 normalización `Trim()` del email y detección de campo que queda vacío **solo con espacios** (`ValidationException` → 400); RN-02 verificación de credenciales: búsqueda de usuario por email + verificación de hash de contraseña (`InvalidCredentialsException` → 401). El negocio funciona como *defensa en profundidad*: aunque la Presentación ya rechace 2a/2b, el service los vuelve a verificar. **Nota de seguridad**: tanto usuario inexistente como contraseña incorrecta devuelven `401` con mensaje genérico para evitar enumeración de cuentas.

### Matriz de trazabilidad CU-02 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `200 OK` | `LoginAsync_WithValidCredentials_ReturnsLoginResponseDTO` | `Login_WithValidCredentials_Returns200OkAndToken` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding) | `Login_WithInvalidJson_Returns400BadRequest` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (se detecta vía `[Required]`) | `Login_WithMissingRequiredField_Returns400BadRequest` |
| 2b. Campo con cadena vacía | `400 Bad Request` | `LoginAsync_WithEmptyPassword_ThrowsValidationException` | *(cubierto por `Login_WithMissingRequiredField_Returns400BadRequest`)* |
| 2c. Campo con solo espacios | `400 Bad Request` | `LoginAsync_WithWhitespaceOnlyEmail_ThrowsValidationException` | `Login_WithWhitespaceOnlyEmail_Returns400BadRequest` |
| 2d. Longitud excesiva | `400 Bad Request` | — (validación de esquema DTO) | `Login_WithOversizedField_Returns400BadRequest` |
| 4a. Usuario no encontrado | `401 Unauthorized` | `LoginAsync_WithNonExistentUser_ThrowsInvalidCredentialsException` | `Login_WithNonExistentUser_Returns401Unauthorized` |
| 5a. Contraseña incorrecta | `401 Unauthorized` | `LoginAsync_WithWrongPassword_ThrowsInvalidCredentialsException` | `Login_WithWrongPassword_Returns401Unauthorized` |
| 7a. Error generando token | `500 Internal Server Error` | `LoginAsync_WhenTokenGenerationFails_ThrowsException` | `Login_WhenTokenGenerationFails_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación (1a, 2a, 2d) el test aplicable es el de integración HTTP. Los tests se ejecutan con `dotnet test CursosOnline.slnx`.
