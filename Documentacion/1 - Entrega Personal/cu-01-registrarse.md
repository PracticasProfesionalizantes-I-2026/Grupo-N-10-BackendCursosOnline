# Caso de Uso: Registrarse

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-01 (email único) y RN-06 (normalización y validación de campos obligatorios) **implementadas** en el código; cada caso borde cuenta con su test unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-01 |
| **Nombre** | Registrarse |
| **Actor Principal** | Alumno / Profesor |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Alumno/Profesor → crear cuenta y acceder a la plataforma; Administración → tener usuarios registrados válidos; Sistema → integridad de datos de usuarios |
| **Disparador (Trigger)** | El usuario selecciona la opción "Comenzar / Comenzar gratis" desde la página principal de la plataforma |
| **Prioridad / Frecuencia** | Alta; frecuencia media (nuevos usuarios periódicos) |
| **Reglas de negocio relacionadas** | RN-01 (email único por usuario); RN-06 (normalización de espacios y validación de longitud en campos obligatorios) |

---

### 1. BREVE DESCRIPCIÓN
Permite que un usuario cree una cuenta en la plataforma seleccionando su rol (Alumno o Profesor), registrando sus credenciales (email, usuario, contraseña) y completando la información personal requerida (nombre, apellido, DNI, teléfono, dirección, código postal) para acceder al sistema.

### 2. PRECONDICIONES
- El usuario no debe poseer una cuenta registrada en el sistema con el mismo email.
- El sistema debe encontrarse disponible y con la Capa de Persistencia accesible.
- El actor debe tener acceso a la página principal de la plataforma.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor envía una petición al endpoint `POST /api/auth/registro` con un JSON que contiene los datos de registro: `rol` (Alumno/Profesor), `email`, `usuario`, `contraseña`, `nombre`, `apellido`, `dni`, `telefono`, `direccion`, `codigoPostal`.
2. La **Capa de Presentación** (`AuthController.Registro`) valida que el JSON sea estructuralmente correcto y que los campos requeridos estén presentes y no vacíos (data annotations `[Required]` sobre `RegistroDTO`).
3. La **Capa de Negocio** (`AuthService.RegistrarAsync`) aplica la regla **RN-06**: normaliza los campos de texto aplicando `Trim()` y verifica que no queden vacíos tras la normalización; valida longitudes máximas.
4. La **Capa de Negocio** verifica la regla **RN-01**: consulta si ya existe un usuario con el mismo email en la base de datos.
5. La **Capa de Negocio** hashea la contraseña de forma segura (bcrypt/Argon2) y construye la entidad `Usuario` con el rol seleccionado.
6. La **Capa de Persistencia** genera un nuevo `Id` (GUID) y guarda el registro en la tabla `Usuarios`.
7. El Sistema devuelve un código **201 Created** con la información resultante (ID, email, usuario, rol, nombre, apellido).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota, cuerpo vacío o formato incorrecto).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición por error de esquema.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el JSON no incluye alguno de los campos requeridos (`rol`, `email`, `usuario`, `contraseña`, `nombre`, `apellido`, `dni`, `telefono`, `direccion`, `codigoPostal`).
  2. El Sistema (Capa de Presentación) rechaza la petición por error de validación (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** detallando el campo faltante con su mensaje, ej: `"El email es obligatorio."`. Fin del caso de uso.

* **2b. Campo con cadena vacía (HTTP 400 Bad Request):**
  1. Si en el Paso 2 un campo obligatorio llega como cadena de longitud cero (`""`).
  2. El Sistema (Capa de Presentación) rechaza la petición por validación de esquema (`[Required]` no permite strings vacíos por defecto).
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje del campo correspondiente. Fin del caso de uso.

* **2c. Campo con solo espacios en blanco (HTTP 400 Bad Request):**
  1. Si en el Paso 2 un campo llega con únicamente espacios (ej. `"   "`): la regla **RN-06** normaliza los valores aplicando `Trim()`; si el valor resultante queda vacío, el Sistema rechaza la petición.
  2. El Sistema (Capa de Negocio) lanza una `ValidationException`.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje del campo correspondiente. Fin del caso de uso.

* **2d. Longitud excesiva de un campo (HTTP 400 Bad Request):**
  1. Si en el Paso 2 algún campo supera la longitud máxima definida en el DTO (`[MaxLength]`: email 256, usuario 100, contraseña 256, nombre/apellido 100, dni 20, telefono 30, direccion 200, codigoPostal 10).
  2. El Sistema (Capa de Presentación) rechaza la petición por validación de esquema (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** indicando el límite excedido. Fin del caso de uso.

* **2e. Formato de email inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el campo `email` no cumple con el formato estándar de correo electrónico (validación `[EmailAddress]` en DTO).
  2. El Sistema (Capa de Presentación) rechaza la petición por validación de esquema.
  3. El Sistema devuelve un código **400 Bad Request** con mensaje: `"El formato del email no es válido."`. Fin del caso de uso.

* **4a. Email ya registrado (HTTP 409 Conflict):**
  1. Si en el Paso 4 la verificación determina que ya existe un usuario registrado con el mismo email, violando la regla de negocio **RN-01** (email único).
  2. El Sistema frena la ejecución en la **Capa de Negocio** y lanza la excepción de dominio `EmailDuplicadoException`.
  3. El Sistema devuelve un código **409 Conflict** con el mensaje: `"Ya existe un usuario registrado con el email {email}"`. Fin del caso de uso.

* **6a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 6 la **Capa de Persistencia** no puede guardar el registro (ej. falla de conexión, corrupción de la base de datos).
  2. El Sistema interrumpe la operación y registra el error como no controlado.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El actor puede enviar el JSON desde el panel web, desde la colección de Bruno (`POST Registro.bru`) o desde un cliente HTTP (Postman, Swagger/Scalar).
2. En todas las variantes el esquema del cuerpo y el resultado (`201 Created`) son idénticos.

### 6. POSTCONDICIONES
- Se ha creado un nuevo registro persistente en la tabla `Usuarios` con ID único (GUID).
- El email queda registrado como único en el sistema (índice único en BD).
- El rol seleccionado queda asociado a la cuenta y determina las funcionalidades accesibles.
- Los datos personales quedan almacenados y vinculados al usuario.
- El usuario queda habilitado para iniciar sesión (CU-02).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Usuario. |
| `400` | Bad Request | Fallo en la validación de esquema o sintaxis del JSON recibido (campos faltantes, vacíos, solo espacios, longitud excesiva, formato email inválido). |
| `409` | Conflict | Violación de invariantes de negocio (RN-01: email duplicado). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** campos obligatorios, cadena vacía, límites de longitud (`[Required]`/`[MaxLength]`/`[EmailAddress]` sobre `RegistroDTO` + `ModelState.IsValid` en el controller), y formato del JSON (model binding).
- **Verificación (Negocio, → 400/409):** RN-06 normalización `Trim()` y detección de campos que quedan vacíos **solo con espacios** (`ValidationException` → 400); RN-01 email único revisando `ExistsByEmailAsync` antes de persistir (`EmailDuplicadoException` → 409). El negocio funciona como *defensa en profundidad*: aunque la Presentación ya rechace los casos 2a/2b, el service los vuelve a verificar.

### Matriz de trazabilidad CU-01 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `RegistrarAsync_WithValidData_ReturnsCreatedUsuarioResponseDTO` / `RegistrarAsync_WithValidData_TrimsAndSavesUsuario` | `Registro_WithValidData_Returns201Created` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding, no pasa por Negocio) | `Registro_WithInvalidJson_Returns400BadRequest` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (se detecta vía `[Required]` en Presentación) | `Registro_WithMissingRequiredField_Returns400BadRequest` |
| 2b. Campo con cadena vacía | `400 Bad Request` | `RegistrarAsync_WithEmptyRequiredField_ThrowsValidationException` | *(cubierto por `Registro_WithMissingRequiredField_Returns400BadRequest`)* |
| 2c. Campo con solo espacios | `400 Bad Request` | `RegistrarAsync_WithWhitespaceOnlyField_ThrowsValidationException` | `Registro_WithWhitespaceOnlyField_Returns400BadRequest` |
| 2d. Longitud excesiva | `400 Bad Request` | — (validación de esquema DTO) | `Registro_WithOversizedField_Returns400BadRequest` |
| 2e. Formato email inválido | `400 Bad Request` | — (validación `[EmailAddress]` en DTO) | `Registro_WithInvalidEmailFormat_Returns400BadRequest` |
| 4a. Email duplicado | `409 Conflict` | `RegistrarAsync_WhenEmailAlreadyExists_ThrowsEmailDuplicadoException` | `Registro_WhenDuplicateEmail_Returns409Conflict` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. En los flujos resueltos en la Capa de Presentación (1a, 2a, 2d, 2e) el test aplicable es el de integración HTTP, ya que el service no se invoca. Los tests se ejecutan con `dotnet test CursosOnline.slnx`.
