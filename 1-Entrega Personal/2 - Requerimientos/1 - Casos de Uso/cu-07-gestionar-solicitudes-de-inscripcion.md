# Caso de Uso: Gestionar Solicitudes de Inscripción

> Especificación derivada de `Lumen_Actores_CasosDeUso.docx` y estructurada
> según la sección 3 de `GUIA-Especificacion-Casos-de-Uso.md`.
> Los códigos HTTP describen la API vigente. La sección de evidencia vincula las pruebas
> ejecutables y distingue las diferencias del prototipo local.

| Campo | Valor |
| --- | --- |
| **ID del Caso de Uso** | CU-07 |
| **Nombre** | Gestionar Solicitudes de Inscripción |
| **Actor Principal** | Administrador |
| **Alcance / Nivel** | Sistema Lumen; meta de usuario |
| **Stakeholders e intereses** | Administrador → resolver solicitudes pendientes; Alumno → conocer el resultado y acceder solo con aprobación; Profesor → mantener participantes autorizados |
| **Disparador (Trigger)** | El Administrador accede a las solicitudes de inscripción pendientes. |
| **Prioridad / Frecuencia** | No especificada en el documento fuente |
| **Reglas de negocio relacionadas** | RN-10 y RN-11 |
| **Referencias funcionales** | RF-23 y RF-24; RN-10 y RN-11. |
| **Autores / Fecha** | Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026) |

**Actores involucrados:**

- **Principal:** Administrador
- **Secundarios:** Alumno

---

### 1. BREVE DESCRIPCIÓN

Permite que un Administrador revise solicitudes de inscripción pendientes y las apruebe o rechace.

### 2. PRECONDICIONES

- El Administrador debe haber iniciado sesión.
- Para resolver una solicitud, la inscripción seleccionada debe estar PENDIENTE.

### 3. FLUJO PRINCIPAL (Camino Feliz - HTTP 200)

1. El Administrador accede a la gestión de inscripciones.
2. El sistema muestra las solicitudes PENDIENTES. **Reglas aplicables:** **RN-10**.
3. El Administrador selecciona una solicitud.
4. El sistema muestra el Alumno y el curso relacionado.
5. El Administrador selecciona "Aprobar".
6. El sistema cambia la inscripción a APROBADA. **Reglas aplicables:** **RN-10**. En la implementación vigente, pausar o finalizar no resuelve las solicitudes pendientes. El efecto del cupo sobre una aprobación sigue pendiente de decisión.
7. El sistema habilita el curso dentro de los cursos del Alumno. **Reglas aplicables:** **RN-11**.
8. El sistema informa que la solicitud fue aprobada.

### 4. FLUJOS ALTERNATIVOS (Caminos Tristes / Excepciones)

* **5a. Rechazar inscripción — A1 (HTTP 200 OK):**
  1. El Administrador selecciona "Rechazar". **Reglas aplicables:** **RN-10**.
  2. El sistema cambia la inscripción a RECHAZADA.
  3. El Alumno no obtiene acceso al curso.

* **2a. No hay solicitudes PENDIENTES — A2 (HTTP 200 OK):**
  1. El sistema informa que no hay solicitudes pendientes para resolver.
  2. El caso finaliza sin modificar inscripciones.

* **5b. La solicitud ya fue resuelta — A3 (HTTP 409 Conflict):**
  1. Antes de aplicar la decisión, el sistema detecta que la inscripción ya no está PENDIENTE. **Reglas aplicables:** **RN-10**.
  2. Muestra el estado actual y no vuelve a aprobarla o rechazarla. Si está APROBADA y debe cancelarse, corresponde CU-13.

### 5. SUB-VARIACIONES (opcional)

- No se especifican sub-variaciones adicionales en el documento fuente.

### 6. POSTCONDICIONES

- La solicitud queda APROBADA o RECHAZADA.
- Si es APROBADA, el Alumno obtiene acceso al curso.
- Si es RECHAZADA, el Alumno no obtiene acceso. El estado resultante puede consultarse mediante CU-12.

---

## Anexo: matrices de referencia

### Códigos HTTP usados

| Código HTTP | Nombre Técnico | Contexto de Aplicación en el Caso de Uso |
| --- | --- | --- |
| `200` | OK | resultado satisfactorio de Gestionar Solicitudes de Inscripción; A1: Rechazar inscripción; A2: No hay solicitudes PENDIENTES. |
| `409` | Conflict | A3: La solicitud ya fue resuelta. |

### Nota: Validación vs. Verificación aplicada

- **Validación (Presentación):** controla formato, presencia y estructura de los datos de entrada; los errores detectables en esta capa se representan con `400 Bad Request`.
- **Verificación (Negocio):** controla permisos, estados y reglas RN aplicables; los rechazos se representan con `403 Forbidden` o `409 Conflict`, según corresponda.

### Evidencia ejecutable de CU-07

| Fuente | Recorrido o contrato |
|---|---|
| API | `GET /api/enrollments?status=Pendiente; POST /api/enrollments/{id}/decision` |
| Pruebas automatizadas | EnrollmentResolutionServiceTests; EnrollmentResolutionConcurrencyIntegrationTests |
| Bruno | Enrollments/03,06; Exposición/09–10 |

200 resuelve; 409 si dejó de estar pendiente. Cupo y tratamiento de pendientes siguen en DOC-004.
