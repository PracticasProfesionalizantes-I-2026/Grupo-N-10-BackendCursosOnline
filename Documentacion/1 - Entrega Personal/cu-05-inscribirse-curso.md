# Caso de Uso: Inscribirse a un Curso

> Especificación elaborada siguiendo la guía
> `GUIA-Especificacion-Casos-de-Uso.md` (sección 3).
> Reglas de negocio RN-05 (alumno no se inscribe 2× al mismo curso), RN-11 (curso debe estar PUBLICADO), RN-12 (cupo máximo) **implementadas** en el código; cada caso borde cuenta con su test unitario e integración (ver matriz de trazabilidad).

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-05 |
| **Nombre** | Inscribirse a un Curso |
| **Actor Principal** | Alumno |
| **Alcance / Nivel** | Sistema; meta de usuario |
| **Stakeholders e intereses** | Alumno → acceder a contenidos y seguimiento del curso; Profesor → tener alumnos inscriptos en su curso; Administración → métricas de inscripción; Sistema → control de cupos y integridad de inscripciones |
| **Disparador (Trigger)** | El alumno selecciona la opción "Inscribirse" sobre un curso disponible en el catálogo |
| **Prioridad / Frecuencia** | Alta; frecuencia alta (alumnos se inscriben regularmente) |
| **Reglas de negocio relacionadas** | RN-05 (alumno no puede inscribirse dos veces al mismo curso); RN-11 (solo cursos en estado PUBLICADO admiten inscripciones); RN-12 (respeto del cupo máximo de alumnos); RN-13 (alumno debe estar autenticado con rol Alumno) |

---

### 1. BREVE DESCRIPCIÓN
Permite que un alumno autenticado se inscriba a un curso disponible (estado PUBLICADO) dentro de la plataforma para acceder a sus contenidos, realizar el seguimiento de su progreso y formar parte del listado de alumnos del profesor.

### 2. PRECONDICIONES
- El alumno debe haber iniciado sesión y poseer un Token JWT válido con rol "Alumno".
- El curso debe existir en el sistema y estar en estado `PUBLICADO`.
- El alumno no debe encontrarse previamente inscripto en el curso.
- El curso no debe haber alcanzado su cupo máximo de alumnos (si tiene cupo definido).
- El sistema debe encontrarse disponible y con la Capa de Persistencia accesible.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 201)
1. El Actor (Alumno autenticado) envía una petición al endpoint `POST /api/inscripciones` con un JSON que contiene: `cursoId` (GUID del curso al que desea inscribirse).
2. La **Capa de Presentación** (`InscripcionesController.CrearInscripcion`) valida que el JSON sea estructuralmente correcto y que `cursoId` esté presente y sea un GUID válido (data annotations `[Required]` sobre `InscripcionCreateDTO`).
3. La **Capa de Negocio** (`InscripcionService.CrearInscripcionAsync`) extrae el `alumnoId` del token JWT (claim `sub`).
4. La **Capa de Negocio** verifica la regla **RN-11**: busca el curso y valida que su estado sea `PUBLICADO`; si no, lanza `InvalidStateException`.
5. La **Capa de Negocio** verifica la regla **RN-12**: si el curso tiene `cupoMaximo` definido, cuenta inscripciones actuales y valida que no se haya alcanzado el límite; si se alcanzó, lanza `CupoCompletoException`.
6. La **Capa de Negocio** verifica la regla **RN-05**: consulta si ya existe una inscripción activa para el par (`alumnoId`, `cursoId`); si existe, lanza `InscripcionDuplicadaException`.
7. La **Capa de Negocio** construye la entidad `Inscripcion` con estado `ACTIVA`, fecha de inscripción (UTC now), `alumnoId` y `cursoId`.
8. La **Capa de Persistencia** genera un nuevo `Id` (GUID) y guarda el registro en la tabla `Inscripciones`.
9. El Sistema devuelve un código **201 Created** con la información resultante (ID de inscripción, cursoId, alumnoId, fechaInscripcion, estado `ACTIVA`).

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **1a. JSON inválido o ilegible (HTTP 400 Bad Request):**
  1. Si en el Paso 1 el cuerpo de la petición no es un JSON válido (sintaxis rota, cuerpo vacío o formato incorrecto).
  2. El Sistema (Capa de Presentación / model binding) rechaza la petición por error de esquema.
  3. El Sistema devuelve un código **400 Bad Request**. Fin del caso de uso.

* **2a. Dato obligatorio faltante (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el JSON no incluye `cursoId`.
  2. El Sistema (Capa de Presentación) rechaza la petición por error de validación (`ModelState.IsValid == false`).
  3. El Sistema devuelve un código **400 Bad Request** con mensaje: `"El ID del curso es obligatorio."`. Fin del caso de uso.

* **2b. cursoId con formato inválido (HTTP 400 Bad Request):**
  1. Si en el Paso 2 el `cursoId` no es un GUID válido.
  2. El Sistema (Capa de Presentación) rechaza la petición por validación de esquema.
  3. El Sistema devuelve un código **400 Bad Request** con mensaje: `"El formato del ID del curso no es válido."`. Fin del caso de uso.

* **4a. Curso no está PUBLICADO (HTTP 409 Conflict):**
  1. Si en el Paso 4 el curso existe pero su estado no es `PUBLICADO` (ej. BORRADOR, EN_REVISION, RECHAZADO, PAUSADO), violando **RN-11**.
  2. La **Capa de Negocio** lanza una `InvalidStateException`.
  3. El Sistema devuelve un código **409 Conflict** con mensaje: `"El curso no se encuentra disponible para inscripción."`. Fin del caso de uso.

* **4b. Curso no encontrado (HTTP 404 Not Found):**
  1. Si en el Paso 4 el `cursoId` no existe en la base de datos.
  2. La **Capa de Negocio** no encuentra la entidad.
  3. El Sistema devuelve un código **404 Not Found**. Fin del caso de uso.

* **5a. Cupo completo (HTTP 409 Conflict):**
  1. Si en el Paso 5 el curso tiene `cupoMaximo` definido y el número de inscripciones activas actuales >= `cupoMaximo`, violando **RN-12**.
  2. La **Capa de Negocio** lanza una `CupoCompletoException`.
  3. El Sistema devuelve un código **409 Conflict** con mensaje: `"El curso ha alcanzado su cupo máximo de alumnos."`. Fin del caso de uso.

* **6a. Alumno ya inscripto (HTTP 409 Conflict):**
  1. Si en el Paso 6 ya existe una inscripción activa para el mismo `alumnoId` y `cursoId`, violando **RN-05**.
  2. La **Capa de Negocio** lanza una `InscripcionDuplicadaException`.
  3. El Sistema devuelve un código **409 Conflict** con mensaje: `"El alumno ya se encuentra inscripto en este curso."`. Fin del caso de uso.

* **6b. Inscripción previa cancelada (HTTP 201 Created - reactivación):**
  1. Si en el Paso 6 existe una inscripción previa pero con estado `CANCELADA` (el alumno se dio de baja antes).
  2. La **Capa de Negocio** reactiva la inscripción existente: actualiza estado a `ACTIVA` y fecha de inscripción a ahora.
  3. La **Capa de Persistencia** actualiza el registro.
  4. El Sistema devuelve **201 Created** (o **200 OK** si se considera actualización) con la inscripción reactivada.

* **8a. Error interno en la persistencia (HTTP 500 Internal Server Error):**
  1. Si en el Paso 8 la **Capa de Persistencia** no puede guardar el registro.
  2. El Sistema interrumpe la operación y registra el error.
  3. El Sistema devuelve un código **500 Internal Server Error**. Fin del caso de uso.

### 5. SUB-VARIACIONES (opcional)
1. El alumno puede inscribirse desde el catálogo web, desde la colección de Bruno (`POST Inscribirse.bru`) o desde un cliente HTTP.
2. Si el curso es gratuito, la inscripción es directa. Si fuera de pago (extensión futura), se integraría con pasarela de pagos antes del paso 7.

### 6. POSTCONDICIONES
- Se ha creado (o reactivado) un registro persistente en la tabla `Inscripciones` con ID único (GUID), estado `ACTIVA`.
- El curso pasa a formar parte de los cursos del alumno (visible en "Mis cursos" / CU-09).
- El alumno obtiene acceso al contenido del curso (módulos, recursos) y puede consultar su progreso (CU-10).
- El alumno aparece en el listado de alumnos inscriptos del profesor (CU-15).
- Se decrementa el cupo disponible del curso (si aplica RN-12).

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `201` | Created | Confirmación de persistencia exitosa de la nueva inscripción (o reactivación). |
| `200` | OK | Reactivación de inscripción previa cancelada (alternativa 6b). |
| `400` | Bad Request | Fallo en validación de esquema (cursoId faltante, formato GUID inválido). |
| `401` | Unauthorized | Token JWT inválido, expirado o sin rol Alumno (validación global en middleware). |
| `403` | Forbidden | Usuario autenticado pero sin rol Alumno (policy de autorización). |
| `404` | Not Found | Curso no existe en la base de datos. |
| `409` | Conflict | Violación de invariantes de negocio: curso no PUBLICADO (RN-11), cupo completo (RN-12), alumno ya inscripto (RN-05). |
| `500` | Internal Server Error | Error técnico no controlado durante la persistencia. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación, → 400):** campo `cursoId` obligatorio y formato GUID (`[Required]` + validación personalizada en `InscripcionCreateDTO` + `ModelState.IsValid`), formato JSON.
- **Verificación (Negocio, → 404/409):** RN-11 verificación de estado `PUBLICADO` (`InvalidStateException` → 409); RN-12 verificación de cupo (`CupoCompletoException` → 409); RN-05 verificación de inscripción duplicada (`InscripcionDuplicadaException` → 409); RN-13 autorización por rol Alumno (policy + claim `sub`). El negocio funciona como *defensa en profundidad*.

### Matriz de trazabilidad CU-05 → Test

| Paso del CU | Excepción / Código | Test unitario (BusinessLogic) | Test integración (HTTP) |
| --- | --- | --- | --- |
| Flujo principal | `201 Created` | `CrearInscripcionAsync_WithValidData_ReturnsCreatedInscripcionResponseDTO` | `CrearInscripcion_WithValidData_Returns201Created` |
| Flujo 6b (reactivación) | `201 Created` / `200 OK` | `CrearInscripcionAsync_WithPreviousCancelled_ReactivatesAndReturnsInscripcion` | `CrearInscripcion_WithPreviousCancelled_Returns201Created` |
| 1a. JSON inválido | `400 Bad Request` | — (model binding) | `CrearInscripcion_WithInvalidJson_Returns400BadRequest` |
| 2a. cursoId faltante | `400 Bad Request` | — (validación `[Required]`) | `CrearInscripcion_WithoutCursoId_Returns400BadRequest` |
| 2b. cursoId formato inválido | `400 Bad Request` | — (validación formato GUID) | `CrearInscripcion_WithInvalidGuid_Returns400BadRequest` |
| 4a. Curso no PUBLICADO | `409 Conflict` | `CrearInscripcionAsync_WhenCursoNotPublicado_ThrowsInvalidStateException` | `CrearInscripcion_WhenCursoNotPublicado_Returns409Conflict` |
| 4b. Curso no encontrado | `404 Not Found` | `CrearInscripcionAsync_WhenCursoNotExists_ThrowsNotFoundException` | `CrearInscripcion_WhenCursoNotExists_Returns404NotFound` |
| 5a. Cupo completo | `409 Conflict` | `CrearInscripcionAsync_WhenCupoCompleto_ThrowsCupoCompletoException` | `CrearInscripcion_WhenCupoCompleto_Returns409Conflict` |
| 6a. Alumno ya inscripto | `409 Conflict` | `CrearInscripcionAsync_WhenAlreadyInscrito_ThrowsInscripcionDuplicadaException` | `CrearInscripcion_WhenAlreadyInscrito_Returns409Conflict` |
| 8a. Error persistencia | `500 Internal Server Error` | `CrearInscripcionAsync_WhenDbFails_ThrowsException` | `CrearInscripcion_WhenDbFails_Returns500InternalServerError` |

> Regla de oro: cada flujo del caso de uso debe tener al menos un test. Los tests se ejecutan con `dotnet test CursosOnline.slnx`.
