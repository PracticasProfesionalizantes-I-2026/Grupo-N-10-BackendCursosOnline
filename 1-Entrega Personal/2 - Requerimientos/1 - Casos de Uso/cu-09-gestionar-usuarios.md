# Caso de Uso: Gestionar Usuarios

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP describen la API vigente. La sección de evidencia vincula las pruebas
> ejecutables y distingue las diferencias del prototipo local.

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
  4. El sistema verifica los datos obligatorios y que el email no esté registrado. Si el cuerpo del alta interna supera el límite de transporte, la API responde HTTP 413 sin crear la cuenta. El alta interna aplica los mismos formatos argentinos y la contraseña de 8 a 128 caracteres que el registro público; un dato inválido da HTTP 400 sin crear al Administrador.
  5. El sistema registra la cuenta con rol Administrador y muestra una confirmación.

* **3b. Datos inválidos o correo duplicado en la creación interna — A2 (HTTP 400 o 409):**
  1. Durante A1, el sistema identifica los datos faltantes o el email ya registrado. **Reglas aplicables:** **RN-01**. La API valida el correo, los campos obligatorios y las longitudes. También rechaza caracteres de control internos en DNI o teléfono. El listado de usuarios admite paginación optativa. Responde 400 ante datos inválidos y 409 ante correo duplicado. En ambos casos conserva los usuarios existentes. El alta interna se ofrece en la API. El prototipo simula la consulta y el estado de cuentas.
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
| `400` | Bad Request | Datos del alta o parámetros de paginación inválidos. |
| `403` | Forbidden | A3: Acceso con un rol sin permiso. |
| `409` | Conflict | A2: correo duplicado en el alta interna. |
| `413` | Content Too Large | Cuerpo mayor que el límite de transporte de la API. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

La paginación de la API es optativa. Acepta limit de 1 a 100 y offset desde cero con limit. Sin parámetros mantiene el arreglo completo.

### Evidencia ejecutable de CU-09

| Fuente | Recorrido o contrato |
|---|---|
| API | `GET /api/users; GET /api/users/{id}; POST /api/users/administrators` |
| Pruebas automatizadas | PagingIntegrationTests; AccountValidationIntegrationTests; OpenApiContractTests; RegistrationConcurrencyRegressionTests |
| Bruno | Users/01–03 |

201 en alta, 400 en validación y 409 por correo duplicado. El alta interna está en la API; el prototipo consulta y simula el estado.
