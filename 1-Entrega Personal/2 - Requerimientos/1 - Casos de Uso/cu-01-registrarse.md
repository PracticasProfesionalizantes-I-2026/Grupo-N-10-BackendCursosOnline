# Caso de Uso: Registrarse

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP describen la API vigente. La sección de evidencia vincula las pruebas
> ejecutables y distingue las diferencias del prototipo local.

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
2. El sistema solicita seleccionar el tipo de cuenta: Alumno o Profesor. La API rechaza su omisión con HTTP 400 y no crea una cuenta. **Reglas aplicables:** **RN-01**.
3. El usuario ingresa email y contraseña.
4. El sistema valida que el email no se encuentre registrado y que los campos obligatorios estén completos.
5. El usuario completa Nombre, Apellido, DNI, Teléfono, Dirección y Código Postal. **Reglas aplicables:** **RN-02**.
6. El sistema valida la información personal. La API comprueba formato de email, campos obligatorios y longitudes definidas por el modelo. **Reglas aplicables:** **RN-02**. El alta exige los datos personales y el rol permitido. DNI y teléfono con caracteres de control internos reciben HTTP 400. Se aplican los formatos argentinos aprobados para DNI, teléfono y código postal. El correo mal formado, un campo obligatorio incompleto o una longitud superior al modelo dan HTTP 400. Si el correo ya existe, la API devuelve 409. El alta exitosa devuelve 201 con los datos de la cuenta. El prototipo conserva un derivado local de la contraseña para iniciar sesión. Si el cuerpo del alta supera el límite de transporte, la API responde HTTP 413 y no crea la cuenta. Para cuentas nuevas, el DNI tiene 7 u 8 dígitos, el teléfono argentino usa de 10 a 15 dígitos y el código postal tiene cuatro dígitos o un CPA de ocho caracteres. La contraseña admite de 8 a 128 caracteres sin exigir composición; los datos inválidos reciben HTTP 400 sin crear la cuenta.
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
| `413` | Content Too Large | Cuerpo mayor que el límite de transporte de la API. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Evidencia ejecutable de CU-01

| Fuente | Recorrido o contrato |
|---|---|
| API | `POST /api/auth/register` |
| Pruebas automatizadas | AccountValidationIntegrationTests; RegistrationConcurrencyRegressionTests |
| Bruno | Auth/04 |

Rol y datos explícitos. 201; 400 o 409 conservan el padrón.
