# Informe de Auditoría y Análisis de Arquitectura de Software
**Proyecto:** LUMEN — Sistema de Gestión de Cursos Online (LMS)  
**Materia:** Práctica Profesionalizante I — Tecnicatura Superior en Desarrollo de Software (2026)  
**Integrantes:** Rodrigo Gabriel Astore, Nahuel Ferrino  
**Docentes:** Gabriel Majluf, Oscar Spila  
**Fecha de Auditoría:** Noviembre de 2026  
**Documentación Evaluada:** `1-Entrega Personal` (Informe General, Actores y Casos de Uso CU-01 a CU-16) vs. `4-Codigo` (Backend .NET 10, SQLite, Tests) y pautas de cátedra (`2-Documentos Oscar`).

---

## 1. Resumen Ejecutivo y Diagnóstico Global

El presente documento recopila la evaluación exhaustiva realizada desde el rol de **Analista Funcional y Arquitecto de Software Senior** sobre el proyecto LUMEN. Se contrastó detalladamente la documentación de análisis de negocio con los 16 Casos de Uso y la implementación técnica en .NET 10.

### Calificación por Dimensión de Análisis

| Dimensión | Calificación | Estado | Síntesis |
|---|---|---|---|
| **Trazabilidad Funcional (RF)** | 5/5 | Completa (35/35) | Todos los requerimientos del negocio están reflejados en al menos un Caso de Uso y endpoint. |
| **Ausencia de Orfandad** | 4/5 | Sólida con anomalías | No hay casos de uso inventados fuera del alcance, pero existen problemas de granularidad y acciones omitidas en los RFs. |
| **Coherencia de NFRs** | 2.5/5 | Desfasaje temporal | La sección de NFRs del Informe General quedó redactada para un prototipo frontend; no refleja las restricciones reales del backend. |
| **Modelado de Actores** | 3.5/5 | Errores de rol previo | Se omitió el rol "Visitante / Usuario Anónimo" y no se documentó el *bootstraping* del Administrador inicial. |
| **Arquitectura de Software** | 5/5 | Nivel industrial | Arquitectura N-Tier limpia, control de concurrencia con ETag, triggers nativos en SQLite, versionado bifurcado y +416 tests. |

---

## 2. Evaluación de Trazabilidad: Requerimientos Funcionales (RF) vs. Casos de Uso (CU)

Se analizó la cobertura de los **35 Requerimientos Funcionales (RF-01 a RF-35)** contra los **16 Casos de Uso (CU-01 a CU-16)**.

### Matriz de Cobertura Detallada

| Grupo de RFs | Requerimientos Funcionales | Casos de Uso Asociados | Evaluación / Observación |
|---|---|---|---|
| **Gestión de Usuarios** | **RF-01**: Registro público de Alumno y Profesor.<br>**RF-02**: Validación de datos obligatorios y email único.<br>**RF-03**: Selección de tipo Alumno o Profesor en registro.<br>**RF-04**: Solicitud de Nombre, Apellido, DNI, Teléfono, Dirección, CP. | `CU-01 Registrarse` | **Cubierto**. *(Hallazgo: Cabecera técnica de CU-01 contiene metadatos rotos que declaran "No especificadas en el documento fuente")*. |
| | **RF-05**: Inicio de sesión mediante credenciales.<br>**RF-06**: Rol asociado y creación interna de Administradores.<br>**RF-07**: Restricción de funciones visibles por rol. | `CU-02 Iniciar Sesión`<br>`CU-09 Gestionar Usuarios` | **Cubierto**. Integrado transversalmente en toda la solución. |
| **Gestión de Cursos** | **RF-08**: Guardado en BORRADOR.<br>**RF-09**: Carga de metadata pedagógica (cupo, objetivos, requisitos).<br>**RF-10**: Módulos con duración, contenido y recursos.<br>**RF-11**: Cálculo automático de duración total.<br>**RF-12**: Validación de obligatoriedad y $\ge 1$ módulo antes de enviar.<br>**RF-13**: Envío a revisión $\rightarrow$ EN REVISIÓN. | `CU-03 Solicitar Creación de Curso`<br>`CU-05 Actualizar Curso Creado` | **Cubierto**. Respalda la creación inicial y guardados sucesivos. |
| | **RF-14**: Auditoría administrativa (aprobar, rechazar, pedir cambios).<br>**RF-15**: Publicación automática al aprobar. | `CU-04 Auditar Solicitud de Curso` | **Cubierto**. |
| | **RF-16**: Edición de cursos propios y re-auditoría de publicados. | `CU-05 Actualizar Curso Creado` | **Cubierto**. Manejo de doble puntero de revisión. |
| | **RF-17**: Pausar y reanudar curso según rol. | `CU-14 Pausar o Reanudar Curso` | **Cubierto**. |
| | **RF-18**: Finalizar curso por Administrador. | `CU-15 Finalizar Curso` | **Cubierto**. |
| | **RF-19**: Catálogo y detalle de cursos PUBLICADOS. | `CU-11 Consultar Cursos Publicados` | **Cubierto**. |
| **Inscripciones y Avance** | **RF-20**: Alumno solicita inscripción a curso publicado.<br>**RF-21**: Impedir duplicado activo o pendiente.<br>**RF-22**: Solicitud inicial en estado PENDIENTE. | `CU-06 Solicitar Inscripción a un Curso` | **Cubierto**. Reforzado por índice único en BD. |
| | **RF-23**: Aprobación / Rechazo de inscripción por Administrador. | `CU-07 Gestionar Solicitudes de Inscripción` | **Cubierto**. Control atómico de cupo en BD. |
| | **RF-24**: Acceso a contenido condicionado a inscripción APROBADA. | `CU-12 Consultar Inscripciones y Acceder`<br>`CU-16 Realizar Módulos del Curso` | **Cubierto**. |
| | **RF-25**: Cancelar inscripción $\rightarrow$ CANCELADA. | `CU-13 Cancelar Inscripción` | **Cubierto**. Libera cupo inmediatamente. |
| | **RF-26**: Alumno consulta cursos inscriptos y estado de solicitudes. | `CU-12 Consultar Inscripciones y Acceder` | **Cubierto**. |
| | **RF-27**: Progreso porcentual según módulos completados. | `CU-08 Consultar Progreso`<br>`CU-16 Realizar Módulos del Curso` | **Cubierto**. *(Hallazgo: la acción de completar módulo no tiene un RF declarativo propio)*. |
| | **RF-28**: Consulta de progreso por Alumno y por Profesor. | `CU-08 Consultar Progreso` | **Cubierto**. |
| **Paneles y Navegación** | **RF-29**: Panel personalizado según rol.<br>**RF-30**: Panel Alumno (cursos, inscripciones, avance).<br>**RF-31**: Panel Profesor (cursos, solicitudes, alumnos).<br>**RF-32**: Panel Administrador (gestión global).<br>**RF-33**: Panel Administrador con reportes básicos. | Mapeados transversalmente en `CU-02` (pasos 7 y 8) y disparadores de CU-03, CU-04, CU-07, CU-08, CU-09, CU-10, CU-11, CU-12. | **Brecha conceptual**: Requerimientos de UI/navegación tipificados erróneamente como RFs sin CU propio. |
| **Reportes** | **RF-34**: Filtro Desde / Hasta obligatorio.<br>**RF-35**: Métricas de usuarios, cursos e inscripciones. | `CU-10 Consultar Reportes` | **Cubierto**. *(Hallazgo: Cabecera técnica de CU-10 declara "No especificadas en el documento fuente")*. |

---

## 3. Orfandad y Granularidad de Casos de Uso

Todos los Casos de Uso responden a metas válidas de los usuarios de negocio. No existen casos de uso inventados fuera de la solución. Sin embargo, se identificaron defectos de granularidad y formulación:

1. **Acción Huérfana en Requerimientos (CU-16):**  
   `CU-16 Realizar Módulos o Actividades del Curso` es la interacción principal del alumno. No obstante, en la lista de requerimientos del Informe General **no existe ningún RF que exprese:** *"El sistema debe permitir que el alumno complete o marque como completado un módulo"*. Solo se enunció la fórmula del porcentaje resultante (RF-27).
2. **Caso de Uso Sobredimensionado / God-Use-Case (CU-12):**  
   `CU-12 Consultar Inscripciones y Acceder a Cursos` fusiona dos intenciones totalmente distintas:
   - Consultar el estado administrativo de sus solicitudes (RF-26).
   - Descargar e interactuar con el contenido pedagógico de un curso aprobado (RF-24).  
   La prueba arquitectónica de esta sobrecarga es que el backend necesitó dos endpoints independientes: `GET /api/enrollments/mine` y `GET /api/enrollments/{id}/course-content`.
3. **Amalgamación de Transiciones Opuestas (CU-14):**  
   `CU-14 Pausar o Reanudar Curso` unifica dos operaciones con precondiciones incompatibles en una sola ficha (`Pausar` requiere curso `Publicado`; `Reanudar` requiere curso `Pausado`).
4. **Metas Heterogéneas en un Mismo Caso de Uso (CU-08):**  
   `CU-08 Consultar Progreso` agrupa a Alumno, Profesor y Administrador. Las necesidades de información son completamente distintas (el alumno ve su propio porcentaje por curso; el profesor ve la nómina de alumnos de sus cursos; el administrador ve todo). En la API esto forzó dos contratos y controladores separados (`/enrollments/{id}/progress` vs `/courses/{courseId}/students/progress`).

---

## 4. Coherencia de Requerimientos No Funcionales (NFRs)

Existe un **desfasaje de madurez temporal** entre la redacción original del Informe General y la solución técnica final implementada:

1. **Enfoque Desactualizado en el Informe General:**  
   La sección 5.2 de `Lumen_Informe_General.md` conserva la redacción de la primera etapa (prototipo frontend). RNF-03 habla de *"navegación del prototipo fluida"* y RNF-07/RNF-08 mencionan *"sin backend productivo"*, ignorando por completo la API .NET y su base SQLite.
2. **Omisión de NFRs de Concurrencia:**  
   No existe ningún RNF sobre concurrencia en el Informe General, a pesar de que el backend implementa una arquitectura avanzada de control optimista con `ETag` / `If-Match` (RFC 6585) en `CU-05` e índices únicos parciales en `CU-06`.
3. **Omisión de NFRs Métricos de Rendimiento:**  
   No hay metas cuantitativas de latencia (percentil p95 < 300 ms, rendimiento bajo carga).
4. **Falta de Formalización de Seguridad de API:**  
   Los mecanismos reales (JWT HS256 $\ge$ 32 bytes, hashing de contraseñas con salt PBKDF2/SHA256, y Rate Limiting de 20 peticiones / 60s en `CU-02`) no están declarados en el catálogo formal de NFRs.

---

## 5. Mapeo de Actores y Stakeholders

1. **Omisión del Actor "Visitante / Usuario No Registrado":**  
   En `CU-01 Registrarse`, el actor principal figura como *"Alumno / Profesor"*. En ingeniería de requisitos esto es incorrecto: quien ejecuta el registro **aún no posee cuenta ni rol**. Debe modelarse como actor **Visitante**.
2. **El Problema del Bootstraping y el "Creador del Sistema":**  
   RF-06 menciona que los administradores son creados por *"el creador del sistema u otros administradores"*. El "Creador" no está definido como actor ni stakeholder. En `CU-09` solo un Administrador autenticado puede crear otro Administrador, dejando un vacío analítico sobre cómo se crea el primer usuario en una base limpia (resuelto fuera de banda en `DbInitializer`).
3. **Rigidez Monorrol por Cuenta:**  
   El modelo actual impide que un usuario registrado como Profesor pueda inscribirse como Alumno para cursar materias dictadas por otros profesores, forzándolo a registrarse con dos correos distintos.

---

## 6. Inventario Consolidado de Brechas e Inconsistencias

| ID Brecha | Dimensión | Severidad | Descripción del Hallazgo | Recomendación de Remediación para el Alumno |
|---|---|---|---|---|
| **GAP-01** | **Trazabilidad** | Media | Metadatos rotos en cabeceras técnicas de `CU-01` y `CU-10`: la celda *"Referencias funcionales"* declara *"No especificadas en el documento fuente"*. | Corregir las tablas de CU-01 vinculando formalmente `RF-01 a RF-04`, y en CU-10 vinculando `RF-33 a RF-35`. |
| **GAP-02** | **Requerimientos** | Media | Omisión de requerimiento de interacción directa para `CU-16`: ningún RF dice *"El sistema debe permitir al Alumno completar un módulo"*. | Agregar formalmente el **RF-27b** en la sección 5.1.3 del Informe General. |
| **GAP-03** | **Requerimientos** | Baja | Paneles y Dashboard (RF-29 a RF-33) catalogados como Requerimientos Funcionales en vez de Requisitos de UI / Navegación. | Reclasificar RF-29 a RF-33 bajo una categoría de *"Requerimientos de Navegación y Vistas"*. |
| **GAP-04** | **NFRs** | Alta | Desfasaje conceptual en RNF-01 a RNF-14: el Informe General no documenta las restricciones reales del backend (.NET 10, ETag, Rate Limiting, JWT). | Actualizar la sección 5.2 del Informe General incorporando formalmente los NFRs de Arquitectura Backend (Concurrencia, Seguridad, Rate Limit y Paginación). |
| **GAP-05** | **Actores** | Media | Actor principal erróneo en `CU-01`: se asignó *"Alumno / Profesor"* antes de que exista la cuenta. | Definir formalmente al actor **Visitante** en el catálogo de actores e imputarlo como iniciador de CU-01. |
| **GAP-06** | **Actores** | Baja | Ambigüedad en la creación del primer Administrador (*Bootstraping*). | Documentar el procedimiento de aprovisionamiento inicial como procedimiento operativo de instalación/despliegue. |
| **GAP-07** | **Granularidad** | Media | `CU-12` amalgama dos metas de usuario disjuntas (consultar estado de inscripción vs. acceder al temario pedagógico). | Dividir conceptualmente en CU-12A (*Consultar Solicitudes*) y CU-12B (*Acceder al Contenido del Curso Aprobado*). |
| **GAP-08** | **Granularidad** | Baja | `CU-14` unifica en una ficha dos transiciones de ciclo de vida con precondiciones mutuamente excluyentes (Pausar vs. Reanudar). | Separar en dos subflujos autónomos con precondición y postcondición claramente diferenciadas. |

---

## 7. Matriz de Trazabilidad Arquitectónica Definitiva (Backend vs. Requisitos)

Para facilitar la defensa técnica del proyecto por parte del alumno, se consolida la correspondencia exacta entre cada requerimiento, caso de uso, código C# y mecanismos de persistencia:

| Dominio | Requerimientos | CU | Endpoint HTTP | Servicio (`BusinessLogic`) | Persistencia (`DataAccess`) | Mecanismo de Integridad / Test |
|---|---|---|---|---|---|---|
| **Registro** | RF-01..04, VAL-002 | CU-01 | `POST /api/auth/register` | `AuthService` | `UserRepository` | Formato argentino validado; índice único `NormalizedEmail`. Pruebas en `AccountValidationIntegrationTests`. |
| **Login** | RF-05..07, SEC-001/003 | CU-02 | `POST /api/auth/login` | `AuthService` | `DemoAccounts` | `LoginRateLimit` (20 req / 60s); bloqueo de cuentas demo en Production. Pruebas en `JwtSecurityIntegrationTests`. |
| **Alta Cursos** | RF-08..13, VAL-004 | CU-03 | `POST /api/courses`<br>`POST /api/courses/{id}/submit-review` | `CourseService` | `CourseRepository` | Trigger `TR_Courses_RevisionOwner_Insert`; cálculo automático de duración. Pruebas en `CourseCreationIntegrationTests`. |
| **Auditoría** | RF-14, RF-15, BUG-001 | CU-04 | `POST /api/course-reviews/{id}/decision` | `CourseService` | `CourseReviewSnapshots` | Triggers `TR_CourseReviewSnapshots_Immutable` y `_Preserve` en SQLite. Pruebas en `CourseReviewHistoryIntegrationTests`. |
| **Edición** | RF-16, DB-004 | CU-05 | `PUT /api/courses/{id}`<br>`POST /api/courses/{id}/recover-draft` | `CourseService` | `CourseRepository` | Concurrencia optimista con header `If-Match` (ETag RFC 6585); forking de revisión. Pruebas en `CourseConcurrencyIntegrationTests`. |
| **Inscripción**| RF-20..22 | CU-06 | `POST /api/enrollments` | `EnrollmentService` | `EnrollmentRepository` | Índice único parcial `IX_Enrollments_StudentId_CourseId_Active` en SQLite. Pruebas en `EnrollmentConcurrencyRegressionTests`. |
| **Cupo** | RF-23, DOC-004 | CU-07 | `POST /api/enrollments/{id}/decision` | `EnrollmentService` | `EnrollmentRepository` | Triggers `TR_CourseRevisions_Capacity` y `TR_Courses_PublishedCapacity`. Pruebas en `ApprovedCapacityIntegrationTests`. |
| **Avance** | RF-27, RF-28 | CU-08, CU-16 | `GET /api/enrollments/{id}/progress`<br>`PUT /api/.../modules/{id}/completion` | `ProgressService` | `EnrollmentModuleCompletion` | PK compuesta `(EnrollmentId, ModuleId)`; idempotencia garantizada. Pruebas en `ModuleCompletionIntegrationTests`. |
| **Finalizar** | RF-18, DOC-004 | CU-15 | `POST /api/courses/{id}/finish` | `CourseService` | `CourseRepository` | Rechazo atómico de pendientes con `ResolutionReason` persistido. Pruebas en `CourseClosureEnrollmentIntegrationTests`. |
| **Reportes** | RF-33..35, VAL-003 | CU-10 | `GET /api/reports/summary` | `ReportService` | `ReportRepository` | Cálculo civil con zona IANA (`America/Argentina/Buenos_Aires`). Pruebas en `ReportsIntegrationTests`. |

---

## 8. Dictamen Final y Conclusiones

1. **Nivel Técnico Sobresaliente:** La implementación en `4-Codigo` exhibe una solidez arquitectónica poco frecuente en proyectos académicos (defensa en profundidad mediante disparadores en SQLite, concurrencia con ETag, inmutabilidad de snapshots, tipado estricto y más de 416 pruebas unitarias y de integración).
2. **Acciones Documentales para el Alumno:**  
   - Subsanar los metadatos de las cabeceras de CU-01 y CU-10 (GAP-01).  
   - Formalizar el actor *Visitante* en CU-01 (GAP-05).  
   - Añadir el RF-27b para cubrir explícitamente la acción de CU-16 (GAP-02).  
   - Actualizar la sección 5.2 de NFRs en el Informe General para que coincida con la infraestructura técnica real del backend (GAP-04).  
3. Con estas correcciones documentales, el proyecto LUMEN alcanzará una coherencia integral del 100% entre análisis de negocio, especificación de requerimientos, casos de uso y código fuente entregado.

