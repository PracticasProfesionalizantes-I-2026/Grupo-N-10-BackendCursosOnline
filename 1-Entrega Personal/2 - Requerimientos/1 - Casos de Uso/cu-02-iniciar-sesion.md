# Caso de Uso: Iniciar Sesión

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP describen la API vigente. La sección de evidencia vincula las pruebas
> ejecutables y distingue las diferencias del prototipo local.

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
4. El sistema valida las credenciales. La API verifica la contraseña antes de informar si la cuenta está deshabilitada. Una contraseña incorrecta recibe 401 y una válida en una cuenta inactiva recibe 403. Si una misma IP supera 20 intentos en una ventana de 60 segundos, la API responde 429 con Retry-After y el usuario debe esperar antes de reintentar. Las credenciales demo de la API solo se admiten en entornos de demostración. Un correo inexistente también pasa por una verificación de contraseña de costo comparable y recibe HTTP 401. Si el cuerpo supera el límite de transporte, la API devuelve HTTP 413 sin iniciar sesión.
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

* **4c. Cuenta inactiva con contraseña válida — A3 (HTTP 403 Forbidden):** El sistema deniega el acceso.
* **4d. Límite temporal de intentos — A4 (HTTP 429 Too Many Requests):** El sistema informa el tiempo de espera mediante `Retry-After`.

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
| `403` | Forbidden | A3: cuenta inactiva con contraseña válida. |
| `429` | Too Many Requests | A4: límite temporal de login. |
| `413` | Content Too Large | Cuerpo mayor que el límite de transporte de la API. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Evidencia ejecutable de CU-02

| Fuente | Recorrido o contrato |
|---|---|
| API | `POST /api/auth/login` |
| Pruebas automatizadas | AccountValidationIntegrationTests; TraceabilityContractIntegrationTests; JwtSecurityIntegrationTests |
| Bruno | Auth/01–03 |

200 con token; 400, 401, 403 o 429 según el flujo. Los tokens demo solo sirven en entornos demo.
