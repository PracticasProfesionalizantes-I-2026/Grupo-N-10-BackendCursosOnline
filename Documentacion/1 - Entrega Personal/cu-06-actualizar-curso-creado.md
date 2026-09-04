# Caso de Uso: Actualizar Curso Creado

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-04 (solo propietario o admin edita), RN-06 (normalización y validación), RN-03 (curso debe tener ≥1 módulo), RN-14 (actualización lleva a EN_REVISION) **implementadas** en el código; cada caso borde cuenta con su test unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-06 |
| **Nombre** | Actualizar Curso Creado |
| **Actor Principal** | Profesor |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Profesor → mantener sus cursos actualizados; Administrador → auditar cambios antes de publicar; Alumno → acceder a información actualizada; Sistema → trazabilidad de versiones y auditoría |
| **Disparador (Trigger)** | El usuario selecciona la opción "Editar Curso" sobre un curso existente en su listado |
| **Prioridad / Frecuencia** | Media; frecuencia baja-media (actualizaciones ocasionales) |
| **Reglas de negocio relacionadas** | RN-04 (solo el profesor propietario o un administrador pueden editar el curso); RN-06 (normalización de espacios y validación de longitud en campos obligatorios); RN-03 (curso debe tener al menos un módulo para enviarse a revisión); RN-14 (cualquier actualización cambia el estado a EN_REVISION y genera nueva solicitud de auditoría) |

---

### 1. BREVE DESCRIPCIÓN
Permite que un profesor (propietario) o administrador modifique la información de un curso existente (datos básicos, módulos, contenidos, duración) para actualizar sus datos, y al confirmar los cambios, el curso pasa automáticamente a estado `EN_REVISION` generando una nueva solicitud de auditoría administrativa (CU-04).

### 2. PRECONDICIONES
- El usuario (profesor o administrador) debe haber iniciado sesión y poseer un Token JWT válido.
- El curso debe existir en el sistema.
- **Si el actor es Profesor:** debe ser el propietario del curso (`curso.profesorId == usuarioId` del token) — regla **RN-04**.
- **Si el actor es Administrador:** puede editar cualquier curso (rol Admin).
- El sistema debe encontrarse disponible y con la Capa de Persistencia accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)
1. El Actor (autenticado) envía una petición al endpoint `PUT /api/cursos/{id}` con un JSON que contiene los campos a actualizar: información básica (`titulo`, `descripcion`, `categoria`, `nivel`), detalles operativos (`modalidad`, `cupoMaximo`, `objetivosAprendizaje`, `requisitosPrevios`), y lista completa de módulos (cada uno con `id` si existe, `nombre`, `descripcion`, `duracion`, `contenidos` — permitiendo agregar, modificar, eliminar módulos/contenidos).
2. La **Capa de Presentación** (`CursosController.ActualizarCurso`) valida que el JSON sea estructuralmente correcto, que el `id` de la ruta coincida con el del cuerpo (si se envía), y que al menos un campo esté presente para actualizar.
3. La **Capa de Negocio** (`CursoService.ActualizarCursoAsync`) verifica la regla **RN-04**: si el actor es Profesor, valida que `curso.profesorId == usuarioId` del token; si es Admin, permite la edición.
4. La **Capa de Negocio** aplica la regla **RN-06**: normaliza campos de texto con `Trim()`, verifica que no queden vacíos, valida longitudes máximas (`[MaxLength]`).
5. La **Capa de Negocio** verifica la regla **RN-03**: si la lista de módulos resultante está vacía, lanza `ValidationException`.
6. La **Capa de Negocio** recalcula automáticamente la duración total del curso sumando la duración de todos los módulos.
7. La **Capa de Negocio** aplica la regla **RN-14**: cambia el estado del curso a `EN_REVISION` (independiente del estado anterior: BORRADOR, PUBLICADO, RECHAZADO, PAUSADO).
8. La **Capa de Negocio** actualiza los datos del curso, sincroniza módulos y contenidos (agrega nuevos, actualiza existentes, elimina los que no vienen en la lista), y registra la fecha de última actualización.
9. La **Capa de Persistencia** persiste los cambios en `Cursos`, `Modulos`, `Contenidos` (operación transaccional).
10. La **Capa de Negocio** genera una nueva entrada en `AuditoriaCursos` (cursoId, usuarioId, accion: "ACTUALIZACION_SOLICITADA", fecha).
11. El Sistema devuelve un código **200 OK** con la información del curso actualizado (estado `EN_REVISION`, duración total, módulos).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido.
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición por error de esquema.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. ID de ruta no coincide con cuerpo (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el `id` en la URL (`/api/cursos/{id}`) no coincide con el `id` enviado en el JSON del cuerpo.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación.
  3. El Sistema devuelve un código **400 Bad Request** con mensaje: `"El ID de la ruta no coincide con el ID del cuerpo."`. Fin del caso de uso.

* **2b. Ningún campo para actualizar (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el JSON está vacío o solo contiene el `id` sin campos modificables.
  2. El Sistema (Capa de Presentación) rechaza la petición.
  3. El Sistema devuelve un código **400 Bad Request** con mensaje: `"Debe proporcionar al menos un campo para actualizar."`. Fin del caso de uso.

* **2c. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 se envía `titulo` como cadena vacía o nulo (si se incluye en la actualización).
  2. El Sistema (Capa de Presentación) rechaza por validación (`[Required]` en `CursoUpdateDTO` para campos presentes).
  3. El Sistema devuelve un código **400 Bad Request** con mensaje del campo. Fin del caso de uso.

* **2d. Campo con solo espacios en blanco (HTTP 400 Bad Request):**
  1. Si en el Paso 2 un campo incluido llega con únicamente espacios: **RN-06** normaliza con `Trim()` y si queda vacío, rechaza.
  2. El Sistema (Capa de Negocio) lanza `ValidationException`.
  3. El Sistema devuelve **400 Bad Request**. Fin del caso de uso.

* **2e. Longitud excesiva (HTTP 400 Bad Request):**
  1. Si en el Paso 2 algún campo supera `[MaxLength]`.
  2. El Sistema (Capa de Presentación) rechaza por validación de esquema.
  3. El Sistema devuelve **400 Bad Request**. Fin del caso de uso.

* **3a. Usuario no autorizado (HTTP 403 Forbidden):**
  1. Si en el Paso 3 el actor es Profesor pero **no es el propietario** del curso (`curso.profesorId != usuarioId`), violando **RN-04**.
  2. La **Capa de Negocio** lanza `UnauthorizedAccessException`.
  3. El Sistema devuelve un código **403 Forbidden** con mensaje: `"No tiene permisos para editar este curso."`. Fin del caso de uso.

* **3b. Curso no encontrado (HTTP 404 Not Found):**
  1. Si en el Paso 3 el `id` del curso no existe en la base de datos.
  2. La **Capa de Negocio** no encuentra la entidad.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **5a. Curso sin módulos tras actualización (HTTP 400 Bad Request):**
  1. Si en el Paso 5 la lista de módulos resultante queda vacía (el profesor eliminó todos), violando **RN-03**.
  2. La **Capa de Negocio** lanza `ValidationException` con mensaje: `"El curso debe tener al menos un módulo."`.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **5b. Módulo sin contenidos (HTTP 400 Bad Request):**
  1. Si en el Paso 5 algún módulo no tiene al menos un contenido.
  2. La **Capa de Negocio** lanza `ValidationException`.
  3. El Sistema devuelve **400 Bad Request**. Fin del caso de uso.

* **9a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 9 la **Capa de Persistencia** falla al guardar (constraint, conexión).
  2. El Sistema interrumpe y registra el error.
  3. El Sistema devuelve **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. **Actualización parcial (PATCH):** El actor puede usar `PATCH /api/cursos/{id}` enviando solo los campos a modificar (JSON Patch o DTO con propiedades opcionales). El comportamiento de reglas (RN-03, RN-14) es idéntico.
2. **Guardar como borrador sin enviar a revisión:** Variante `PUT /api/cursos/{id}/borrador` que actualiza datos pero mantiene estado `BORRADOR` (no genera auditoría). Ver CU-12.
3. El actor puede editar desde el panel web del profesor, colección Bruno (`PUT Editar Curso.bru`), o cliente HTTP.

### 6. POSTCONDICIONES
- Los cambios realizados quedan registrados persistentemente en `Cursos`, `Modulos`, `Contenidos`.
- El curso pasa a estado `EN_REVISION` (regla **RN-14**), independientemente de su estado anterior.
- Se genera una nueva solicitud de auditoría disponible para el administrador (CU-04).
- El administrador podrá aprobar (curso vuelve a `PUBLICADO`) o rechazar (curso a `RECHAZADO`) los cambios.
- Queda registro en `AuditoriaCursos` de la solicitud de actualización (usuarioId, fecha, acción).
- Si el curso estaba `PUBLICADO`, deja de ser visible en catálogo hasta nueva aprobación (impacto en visibilidad para alumnos).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | Actualización exitosa; respuesta con curso actualizado (estado EN_REVISION). |
| `400` | Bad Request | Validación de esquema: ID mismatch, ningún campo, campo obligatorio faltante, solo espacios, longitud excesiva, curso sin módulos, módulo sin contenidos. |
| `401` | Unauthorized | Token JWT inválido o expirado (middleware global). |
| `403` | Forbidden | Profesor no es propietario del curso (violación RN-04). |
| `404` | Not Found | Curso no existe en la base de datos. |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** ID ruta vs cuerpo, al menos un campo, campos obligatorios presentes, formato, longitudes (`[Required]`/`[MaxLength]` en `CursoUpdateDTO` + `ModelState.IsValid`), JSON válido.
- **Verificación (Negocio, → 400/403/404):** RN-06 normalización `Trim()` y detección vacío solo espacios (`ValidationException`); RN-04 autorización: propietario o Admin (`UnauthorizedAccessException` → 403); RN-03 al menos un módulo y contenido por módulo; RN-14 cambio de estado a `EN_REVISION` y generación de auditoría. El negocio funciona como *defensa en profundidad*.

### Matriz de trazabilidad CU-06 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal (profesor propietario) | `200 OK` | `ActualizarCursoAsync_AsOwner_ReturnsUpdatedCursoEnRevision` | `ActualizarCurso_AsOwner_Returns200OkAndEnRevision` |
| Flujo principal (admin) | `200 OK` | `ActualizarCursoAsync_AsAdmin_ReturnsUpdatedCursoEnRevision` | `ActualizarCurso_AsAdmin_Returns200OkAndEnRevision` |
| Flujo variante PATCH | `200 OK` | `ActualizarCursoAsync_PartialUpdate_ReturnsUpdatedCurso` | `ActualizarCurso_Patch_Returns200Ok` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding) | `ActualizarCurso_WithInvalidJson_Returns400BadRequest` |
| 2a. ID mismatch | `400 Bad Request` | `ActualizarCursoAsync_WithIdMismatch_ThrowsValidationException` | `ActualizarCurso_WithIdMismatch_Returns400BadRequest` |
| 2b. Sin campos para actualizar | `400 Bad Request` | `ActualizarCursoAsync_WithNoFields_ThrowsValidationException` | `ActualizarCurso_WithNoFields_Returns400BadRequest` |
| 2c. Campo obligatorio faltante | `400 Bad Request` | — (validación `[Required]`) | `ActualizarCurso_WithEmptyRequiredField_Returns400BadRequest` |
| 2d. Solo espacios | `400 Bad Request` | `ActualizarCursoAsync_WithWhitespaceOnly_ThrowsValidationException` | `ActualizarCurso_WithWhitespaceOnly_Returns400BadRequest` |
| 2e. Longitud excesiva | `400 Bad Request` | — (validación DTO) | `ActualizarCurso_WithOversizedField_Returns400BadRequest` |
| 3a. No propietario (profesor) | `403 Forbidden` | `ActualizarCursoAsync_WhenNotOwner_ThrowsUnauthorizedAccessException` | `ActualizarCurso_WhenNotOwner_Returns403Forbidden` |
| 3b. Curso no encontrado | `404 Not Found` | `ActualizarCursoAsync_WhenNotExists_ThrowsNotFoundException` | `ActualizarCurso_WhenNotExists_Returns404NotFound` |
| 5a. Sin módulos | `400 Bad Request` | `ActualizarCursoAsync_WithNoModulos_ThrowsValidationException` | `ActualizarCurso_WithNoModulos_Returns400BadRequest` |
| 5b. Módulo sin contenidos | `400 Bad Request` | `ActualizarCursoAsync_WithModuloSinContenidos_ThrowsValidationException` | `ActualizarCurso_WithModuloSinContenidos_Returns400BadRequest` |
| 9a. Error persistencia | `500 Internal Server Error` | `ActualizarCursoAsync_WhenDbFails_ThrowsException` | `ActualizarCurso_WhenDbFails_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. Los tests se ejecutan con `dotnet test CursosOnline.slnx`.
