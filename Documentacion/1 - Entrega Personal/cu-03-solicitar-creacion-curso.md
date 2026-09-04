# Caso de Uso: Solicitar Creación de Curso

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-03 (curso debe tener ≥1 módulo), RN-06 (normalización y validación de campos obligatorios), RN-07 (propietario del curso) **implementadas** en el código; cada caso borde cuenta con su test unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-03 |
| **Nombre** | Solicitar Creación de Curso |
| **Actor Principal** | Profesor |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Profesor → crear y proponer cursos para dictar; Administrador → revisar y aprobar propuestas; Alumno → acceder a catálogo de cursos aprobados; Sistema → integridad de datos de cursos y módulos |
| **Disparador (Trigger)** | El profesor selecciona la opción "Crear Curso" desde su panel principal |
| **Prioridad / Frecuencia** | Alta; frecuencia media (profesores crean cursos periódicamente) |
| **Reglas de negocio relacionadas** | RN-03 (curso debe tener al menos un módulo para enviarse a revisión); RN-06 (normalización de espacios y validación de longitud en campos obligatorios); RN-07 (el profesor queda registrado como propietario del curso) |

---

### 1. BREVE DESCRIPCIÓN
Permite que un profesor autenticado cree una solicitud de curso ingresando la información básica (título, descripción, categoría, nivel), configurando detalles operativos (modalidad, cupo máximo, objetivos, requisitos previos), agregando uno o más módulos con su contenido y recursos (video, PDF, actividad, evaluación), y enviando la propuesta para su posterior revisión por parte de un administrador.

### 2. PRECONDICIONES
- El profesor debe haber iniciado sesión y poseer un Token JWT válido con rol "Profesor".
- El sistema debe encontrarse disponible y con la Capa de Persistencia accesible.
- El profesor debe tener permisos de creación de cursos.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor (Profesor autenticado) envía una petición al endpoint `POST /api/cursos` con un JSON que contiene: información básica (`titulo`, `descripcion`, `categoria`, `nivel`), detalles operativos (`modalidad`, `cupoMaximo`, `objetivosAprendizaje`, `requisitosPrevios`), y una lista de módulos (cada uno con `nombre`, `descripcion`, `duracion`, `contenidos` — lista de recursos con `tipo` [Video/PDF/Actividad/Evaluación], `url`, `orden`).
2. La **Capa de Presentación** (`CursosController.CrearCurso`) valida que el JSON sea estructuralmente correcto y que los campos requeridos estén presentes (data annotations `[Required]` sobre `CursoCreateDTO` y `ModuloCreateDTO`).
3. La **Capa de Negocio** (`CursoService.CrearCursoAsync`) aplica la regla **RN-06**: normaliza campos de texto aplicando `Trim()` y verifica que no queden vacíos; valida longitudes máximas (`[MaxLength]`).
4. La **Capa de Negocio** verifica que la lista de módulos no esté vacía (regla **RN-03**: al menos un módulo).
5. La **Capa de Negocio** calcula automáticamente la duración total del curso sumando la duración de todos los módulos.
6. La **Capa de Negocio** construye la entidad `Curso` con estado `BORRADOR`, asigna el `profesorId` del token JWT como propietario (regla **RN-07**), y asocia los módulos y contenidos.
7. La **Capa de Persistencia** genera un nuevo `Id` (GUID) para el curso y cada módulo/contenido, y guarda los registros en las tablas `Cursos`, `Modulos`, `Contenidos`.
8. El Sistema devuelve un código **201 Created** con la información resultante (ID del curso, título, estado `BORRADOR`, duración total, lista de módulos).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota, cuerpo vacío o formato incorrecto).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición por error de esquema.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante en información básica (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el JSON no incluye `titulo` (obligatorio) u otros campos requeridos del curso o módulos.
  2. El Sistema (Capa de Presentación) rechaza la petición por error de validación (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** detallando el campo faltante, ej: `"El título del curso es obligatorio."`. Fin del caso de uso.

* **2b. Campo con cadena vacía (HTTP 400 Bad Request):**
  1. Si en el Paso 2 un campo obligatorio llega como cadena de longitud cero (`""`).
  2. El Sistema (Capa de Presentación) rechaza la petición por validación de esquema.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje del campo correspondiente. Fin del caso de uso.

* **2c. Campo con solo espacios en blanco (HTTP 400 Bad Request):**
  1. Si en el Paso 2 un campo llega con únicamente espacios (ej. `"   "`): la regla **RN-06** normaliza aplicando `Trim()`; si el valor resultante queda vacío, el Sistema rechaza la petición.
  2. El Sistema (Capa de Negocio) lanza una `ValidationException`.
  3. El Sistema devuelve un código **400 Bad Request** con el mensaje del campo correspondiente. Fin del caso de uso.

* **2d. Longitud excesiva de un campo (HTTP 400 Bad Request):**
  1. Si en el Paso 2 algún campo supera la longitud máxima (`[MaxLength]`: titulo 200, descripcion 2000, categoria 100, nombre modulo 200, url 500).
  2. El Sistema (Capa de Presentación) rechaza la petición por validación de esquema.
  3. El Sistema devuelve un código **400 Bad Request** indicando el límite excedido. Fin del caso de uso.

* **4a. Curso sin módulos (HTTP 400 Bad Request):**
  1. Si en el Paso 4 la lista de módulos está vacía o no se envió, violando la regla **RN-03**.
  2. La **Capa de Negocio** lanza una `ValidationException` con mensaje: `"El curso debe tener al menos un módulo."`.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **4b. Módulo sin contenidos (HTTP 400 Bad Request):**
  1. Si en el Paso 4 algún módulo no tiene al menos un contenido/recurso asociado.
  2. La **Capa de Negocio** lanza una `ValidationException`.
  3. El Sistema devuelve un código **400 Bad Request** con mensaje: `"Cada módulo debe tener al menos un contenido."`. Fin del caso de uso.

* **7a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 7 la **Capa de Persistencia** no puede guardar los registros (falla de conexión, constraint de BD).
  2. El Sistema interrumpe la operación y registra el error.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Guardar como borrador (flujo alternativo A1 del documento original):** El profesor puede enviar la petición a `POST /api/cursos/borrador` (o incluir `"estado": "BORRADOR"` en el JSON) sin necesidad de tener módulos. El curso queda en estado `BORRADOR` y el profesor puede continuar editando posteriormente (ver CU-12).
2. El actor puede enviar el JSON desde el panel web del profesor, desde la colección de Bruno (`POST Crear Curso.bru`) o desde un cliente HTTP (Postman, Swagger/Scalar).
3. En todas las variantes el esquema del cuerpo y el resultado (`201 Created` para envío a revisión, `201 Created` con estado `BORRADOR` para guardado) son consistentes.

### 6. POSTCONDICIONES
- Se ha creado un nuevo registro persistente en la tabla `Cursos` con ID único (GUID), estado `BORRADOR` o `EN_REVISION` según variante.
- Los módulos y contenidos asociados quedan almacenados en `Modulos` y `Contenidos` con sus respectivos IDs y orden.
- La duración total del curso se calcula y almacena automáticamente.
- El profesor queda registrado como propietario del curso (campo `profesorId`).
- La solicitud queda disponible para ser auditada por un administrador (CU-04) si el estado es `EN_REVISION`.
- El profesor puede visualizar el curso en su listado "Mis cursos creados" (CU-13).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa del nuevo recurso Curso (estado BORRADOR o EN_REVISION). |
| `400` | Bad Request | Fallo en validación de esquema (campos faltantes, vacíos, solo espacios, longitud excesiva, curso sin módulos, módulo sin contenidos). |
| `401` | Unauthorized | Token JWT inválido, expirado o sin rol Profesor (validación global en middleware). |
| `403` | Forbidden | Usuario autenticado pero sin rol Profesor (validación de autorización en policy). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** campos obligatorios (`titulo`), cadena vacía, límites de longitud (`[Required]`/`[MaxLength]` sobre `CursoCreateDTO`/`ModuloCreateDTO` + `ModelState.IsValid`), formato JSON.
- **Verificación (Negocio, → 400):** RN-06 normalización `Trim()` y detección de campos vacíos solo con espacios (`ValidationException`); RN-03 verificación de al menos un módulo y al menos un contenido por módulo; RN-07 asignación de `profesorId` desde token JWT. El negocio funciona como *defensa en profundidad*.

### Matriz de trazabilidad CU-03 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (envío a revisión) | `201 Created` | `CrearCursoAsync_WithValidData_ReturnsCreatedCursoResponseDTO` / `CrearCursoAsync_CalculatesTotalDuration` | `CrearCurso_WithValidData_Returns201Created` |
| Flujo variante (guardar borrador) | `201 Created` | `CrearCursoAsync_AsDraft_SavesWithBorradorState` | `CrearCurso_AsDraft_Returns201CreatedAndBorradorState` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding) | `CrearCurso_WithInvalidJson_Returns400BadRequest` |
| 2a. Dato obligatorio faltante | `400 Bad Request` | — (se detecta vía `[Required]`) | `CrearCurso_WithMissingRequiredField_Returns400BadRequest` |
| 2b. Campo con cadena vacía | `400 Bad Request` | `CrearCursoAsync_WithEmptyRequiredField_ThrowsValidationException` | *(cubierto por `CrearCurso_WithMissingRequiredField_Returns400BadRequest`)* |
| 2c. Campo con solo espacios | `400 Bad Request` | `CrearCursoAsync_WithWhitespaceOnlyField_ThrowsValidationException` | `CrearCurso_WithWhitespaceOnlyField_Returns400BadRequest` |
| 2d. Longitud excesiva | `400 Bad Request` | — (validación de esquema DTO) | `CrearCurso_WithOversizedField_Returns400BadRequest` |
| 4a. Curso sin módulos | `400 Bad Request` | `CrearCursoAsync_WithNoModulos_ThrowsValidationException` | `CrearCurso_WithNoModulos_Returns400BadRequest` |
| 4b. Módulo sin contenidos | `400 Bad Request` | `CrearCursoAsync_WithModuloSinContenidos_ThrowsValidationException` | `CrearCurso_WithModuloSinContenidos_Returns400BadRequest` |
| 7a. Error persistencia | `500 Internal Server Error` | `CrearCursoAsync_WhenDbFails_ThrowsException` | `CrearCurso_WhenDbFails_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. Los tests se ejecutan con `dotnet test CursosOnline.slnx`.
