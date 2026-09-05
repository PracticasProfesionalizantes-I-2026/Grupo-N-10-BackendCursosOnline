# **LUMEN**

**Sistema de Gestión de Cursos Online (LMS)**

Informe General del Proyecto

|  |  |
| --- | --- |
| **Materia** | Práctica Profesionalizante I |
| **Integrantes** | Astore Rodrigo - Ferrino Nahuel |
| **Carrera** | Tecnicatura Superior en Desarrollo de Software |
| **Año** | 2026 |

*Documentación académica revisada - Septiembre 2026*

## 1. Requerimientos del Negocio

### 1.1 Situación actual o Propósito

Actualmente, muchas academias pequeñas, profesores independientes y emprendimientos educativos gestionan cursos online mediante herramientas separadas, como formularios, correos, mensajería o planillas. Esta dispersión dificulta mantener organizada la información de usuarios, cursos, inscripciones y progreso.

- La información de los alumnos puede quedar distribuida en distintos medios.

- No siempre existe una visión clara de qué alumnos participan en cada curso.

- Los profesores necesitan consultar de forma simple el avance de sus alumnos.

- Los alumnos necesitan un espacio centralizado para consultar cursos, inscripciones y progreso.

- Las tareas manuales de control y seguimiento aumentan el riesgo de errores e inconsistencias.

El propósito de Lumen es centralizar estos procesos en un Sistema de Gestión de Cursos Online (LMS), definiendo de forma clara qué puede hacer cada tipo de usuario y representando esos flujos mediante un prototipo frontend funcional y navegable. En la etapa actual no se considera implementado un backend productivo.

### 1.2 Oportunidad del negocio

Lumen ofrece la oportunidad de ordenar la gestión de cursos online en un único sistema conceptual, reduciendo la dispersión de información y haciendo más claros los procesos de registro, creación y auditoría de cursos, inscripción, seguimiento y administración.

- Centralizar la información relevante del funcionamiento de los cursos.

- Definir responsabilidades y permisos claros para Alumno, Profesor y Administrador.

- Reducir errores mediante validaciones y estados definidos para los procesos principales.

- Representar los flujos del sistema en una interfaz navegable que permita validar la experiencia de uso.

- Contar con una documentación consistente que sirva como base para futuras etapas de desarrollo.

### 1.3 Riesgos

**Riesgo 1 - Requerimientos mal interpretados (Alta):** Una definición incompleta o contradictoria puede producir flujos que no representen lo esperado.

**Mitigación:** Revisar periódicamente requerimientos, reglas, permisos y casos de uso con el equipo y los profesores.

**Riesgo 2 - Accesos incorrectos según el rol (Alta):** Un usuario podría visualizar o ejecutar acciones que no le corresponden.

**Mitigación:** Mantener una matriz de permisos coherente y reflejarla en los flujos del prototipo.

**Riesgo 3 - Inconsistencias en la auditoría de cursos (Alta):** Un curso o una modificación podría considerarse publicada sin la revisión administrativa requerida.

**Mitigación:** Definir estados y transiciones explícitas para creación, actualización y re-auditoría.

**Riesgo 4 - Inscripciones inconsistentes (Alta):** Podrían existir duplicados, accesos sin aprobación o estados contradictorios.

**Mitigación:** Aplicar una única lógica de solicitud, revisión y resolución de inscripciones.

**Riesgo 5 - Información incompleta en cursos (Media):** Un curso podría enviarse a revisión sin los datos o módulos mínimos.

**Mitigación:** Validar la información obligatoria y exigir al menos un módulo antes del envío.

**Riesgo 6 - Cálculo incorrecto de duración (Media):** La duración total podría no coincidir con las duraciones de los módulos.

**Mitigación:** Calcular la duración total exclusivamente a partir de la suma de los módulos.

**Riesgo 7 - Seguimiento de progreso poco claro (Media):** El avance mostrado podría no corresponder con los módulos o actividades completadas.

**Mitigación:** Definir una regla simple y uniforme para representar porcentaje y estado de progreso.

**Riesgo 8 - Prototipo no alineado con la documentación (Alta):** La interfaz podría mostrar acciones o estados que no existen en los requerimientos.

**Mitigación:** Usar los documentos como referencia funcional para cada pantalla y flujo.

**Riesgo 9 - Retrasos por organización del equipo (Media):** La falta de priorización puede afectar las entregas.

**Mitigación:** Dividir el trabajo en tareas pequeñas, priorizar el MVP y realizar seguimientos periódicos.

**Riesgo 10 - Alcance técnico sobredimensionado (Media):** Documentar tecnologías o infraestructura no implementadas puede confundir el alcance académico.

**Mitigación:** Separar la especificación funcional del sistema de la implementación real del prototipo actual.

## 2. Visión de la Solución

### 2.1 Funciones principales

**1. Registro de usuarios:** Alumno y Profesor pueden crear una cuenta pública, seleccionar su tipo de usuario y completar los datos personales requeridos. El Administrador no se registra públicamente.

**2. Inicio de sesión:** Los usuarios registrados pueden acceder con sus credenciales y visualizar las funciones correspondientes a su rol.

**3. Gestión de roles y permisos:** El sistema diferencia Alumno, Profesor y Administrador y restringe las operaciones según la matriz de permisos.

**4. Creación de cursos:** El Profesor prepara un curso, puede guardarlo como BORRADOR, agrega módulos con duración y lo envía a revisión administrativa.

**5. Auditoría de cursos:** El Administrador revisa solicitudes de creación o modificación y puede aprobar, rechazar o solicitar cambios.

**6. Actualización y re-auditoría:** El Profesor puede modificar cursos propios; los cambios deben volver a revisión antes de considerarse aprobados.

**7. Consulta de cursos:** El Alumno puede consultar los cursos PUBLICADOS disponibles para inscripción.

**8. Inscripciones:** El Alumno solicita inscripción a un curso PUBLICADO. La solicitud queda PENDIENTE hasta que el Administrador la aprueba o rechaza.

**9. Seguimiento del progreso:** El sistema representa el avance del Alumno mediante porcentaje y estados asociados a módulos o actividades completadas.

**10. Administración y reportes:** El Administrador gestiona usuarios y cursos y consulta reportes básicos mediante un rango de fechas Desde/Hasta.

## 3. Contexto del Negocio

### 3.1 Perfil de los interesados (Stakeholders)

| **Stakeholders** | **Beneficio y valor percibido** | **Actitudes** | **Funciones de interés mayor** | **Restricciones** |
| --- | --- | --- | --- | --- |
| Administrador de la plataforma | Mantener control y organización sobre usuarios, cursos, auditorías e inscripciones. | Busca información clara y procesos simples. | Gestión de usuarios y cursos, auditorías, inscripciones y reportes. | Necesita evitar procesos ambiguos y estados inconsistentes. |
| Profesor | Crear y mantener cursos y consultar el avance de sus alumnos. | Espera una experiencia sencilla y directa. | Creación y actualización de cursos, módulos, alumnos y progreso. | No requiere conocimientos técnicos avanzados. |
| Alumno | Acceder a cursos, solicitar inscripción y consultar su avance. | Espera una navegación clara y sin pasos innecesarios. | Catálogo, cursos inscriptos y progreso. | Necesita comprender fácilmente el estado de sus solicitudes. |
| Desarrollador (equipo técnico) | Contar con documentación ordenada y trazable para construir el prototipo. | Trabaja de forma iterativa y académica. | Estructura funcional, reglas, permisos y flujos. | Tiempo limitado y alcance centrado en documentación y frontend. |

## 4. Alcance y limitaciones

### 4.1 Alcance inicial (MVP - Minimum Viable Product)

El MVP documenta las capacidades esenciales de Lumen y las representa mediante un prototipo frontend funcional/navegable. Los comportamientos funcionales se especifican aunque determinadas validaciones o persistencias sean simuladas en esta etapa.

- Registro público de Alumno y Profesor e inicio de sesión para los tres roles.

- Asignación y restricción de funcionalidades según rol.

- Creación de cursos por Profesor, guardado como borrador, módulos y cálculo automático de duración total.

- Auditoría administrativa de creación y de modificaciones de cursos.

- Consulta de cursos publicados y solicitud de inscripción.

- Revisión administrativa de solicitudes de inscripción.

- Consulta de cursos inscriptos y seguimiento básico del progreso.

- Gestión administrativa básica de usuarios y cursos.

- Reportes administrativos básicos con filtros Desde y Hasta.

La entrega práctica actual se centra en documentación y prototipado frontend. No se considera obligatoria la implementación de una API real, base de datos productiva, servidor backend, autenticación productiva ni mecanismos técnicos específicos.

### 4.2 Limitaciones y exclusiones (Out of Scope)

- Sistemas de pago, tarjetas o billeteras virtuales.

- Clases en vivo o videollamadas dentro de la plataforma.

- Envío automático de correos electrónicos.

- Reportes avanzados, analítica compleja o estadísticas predictivas.

- Backend productivo, API real, endpoints reales y persistencia productiva en esta etapa.

- Implementación obligatoria de JWT, BCrypt, Swagger/OpenAPI o una arquitectura backend específica.

- Backups productivos e infraestructura de despliegue backend como requisito de la entrega actual.

## 5. Requerimientos

### 5.1 Requerimientos Funcionales

#### 5.1.1 Gestión de Usuarios:

- **RF-01:** El sistema debe permitir el registro público de usuarios con tipo ALUMNO o PROFESOR mediante email y contraseña.

- **RF-02:** El sistema debe validar que los datos obligatorios del registro hayan sido completados y que el email no se encuentre registrado.

- **RF-03:** El sistema debe permitir seleccionar el tipo de usuario ALUMNO o PROFESOR durante el registro público.

- **RF-04:** El sistema debe solicitar Nombre, Apellido, DNI, Teléfono, Dirección y Código Postal antes de completar el registro.

- **RF-05:** El sistema debe permitir que los usuarios registrados inicien sesión mediante sus credenciales.

- **RF-06:** El sistema debe asociar un rol a cada cuenta. Las cuentas de Administrador no pueden crearse mediante el registro público y son gestionadas internamente por el creador del sistema u otros administradores.

- **RF-07:** El sistema debe restringir las funcionalidades visibles y disponibles de acuerdo con el rol del usuario.

#### 5.1.2 Gestión de Cursos

- **RF-08:** El sistema debe permitir que un Profesor cree un curso y lo guarde en estado BORRADOR antes de enviarlo a revisión.

- **RF-09:** El sistema debe permitir que el Profesor ingrese la información del curso respaldada por el proyecto: título, descripción, categoría, nivel, modalidad, cupo máximo, objetivos de aprendizaje y requisitos previos sugeridos.

- **RF-10:** El sistema debe permitir que el Profesor agregue uno o más módulos, indicando para cada módulo nombre, descripción y duración, y asociando su contenido o recursos.

- **RF-11:** El sistema debe calcular automáticamente la duración total del curso como suma de las duraciones de sus módulos.

- **RF-12:** El sistema debe validar la información obligatoria y la existencia de al menos un módulo antes de permitir el envío a revisión.

- **RF-13:** El sistema debe permitir que el Profesor envíe un curso a revisión, cambiando su estado a EN REVISIÓN.

- **RF-14:** El sistema debe permitir que el Administrador audite solicitudes de creación o modificación y decida aprobar, rechazar o solicitar cambios.

- **RF-15:** El sistema debe publicar un curso cuando la auditoría sea aprobada, dejándolo disponible para consulta e inscripción.

- **RF-16:** El sistema debe permitir que el Profesor modifique únicamente cursos propios. La edición de un curso BORRADOR conserva ese estado hasta que el Profesor solicita su envío a revisión. Las modificaciones de un curso PUBLICADO y las correcciones de un curso en CAMBIOS SOLICITADOS deben generar una nueva revisión administrativa al confirmarse, antes de considerarse aprobadas.

- **RF-17:** El sistema debe permitir que Profesor y Administrador pausen cursos PUBLICADOS y reanuden cursos PAUSADOS según sus permisos. El Profesor solo puede hacerlo sobre cursos propios.

- **RF-18:** El sistema debe permitir que el Administrador finalice un curso cuando corresponda.

- **RF-19:** El sistema debe mostrar a los alumnos el listado y detalle de cursos en estado PUBLICADO.

#### 5.1.3 Inscripciones y Seguimiento:

- **RF-20:** El sistema debe permitir que un Alumno solicite inscripción a un curso PUBLICADO.

- **RF-21:** El sistema debe impedir que un Alumno genere más de una inscripción activa o pendiente para el mismo curso.

- **RF-22:** El sistema debe registrar una nueva solicitud de inscripción en estado PENDIENTE.

- **RF-23:** El sistema debe permitir que el Administrador apruebe o rechace solicitudes de inscripción pendientes.

- **RF-24:** El sistema debe otorgar acceso al curso al Alumno únicamente cuando la inscripción se encuentre APROBADA.

- **RF-25:** El sistema debe permitir cancelar una inscripción y reflejar el estado CANCELADA cuando corresponda.

- **RF-26:** El sistema debe permitir que el Alumno consulte los cursos asociados a inscripciones aprobadas y el estado de sus solicitudes.

- **RF-27:** El sistema debe registrar el progreso del Alumno dentro de un curso aprobado mediante un porcentaje relacionado con módulos o actividades completadas.

- **RF-28:** El sistema debe permitir que el Alumno consulte su propio progreso y que el Profesor consulte el progreso de los alumnos de sus cursos.

#### 5.1.4 Paneles y Navegación

- **RF-29:** El sistema debe mostrar un panel personalizado según el rol del usuario.

- **RF-30:** El panel del Alumno debe permitir acceder a cursos disponibles, solicitudes/inscripciones y progreso.

- **RF-31:** El panel del Profesor debe permitir acceder a sus cursos, solicitudes de auditoría y seguimiento de alumnos.

- **RF-32:** El panel del Administrador debe permitir la gestión general de usuarios, cursos, auditorías e inscripciones.

- **RF-33:** El panel del Administrador debe incluir una sección de reportes básicos.

- **RF-34:** El sistema debe permitir seleccionar una fecha inicial y una fecha final mediante los campos "Desde" y "Hasta".

- **RF-35:** El sistema debe mostrar, para el rango seleccionado, información básica sobre usuarios registrados, cursos creados e inscripciones realizadas.

### 5.2 Requerimientos No Funcionales

#### 5.2.1 Seguridad:

- **RNF-01:** La interfaz debe evitar exponer a usuarios funciones que no correspondan a su rol.

- **RNF-02:** Los formularios de acceso y registro deben comunicar de forma clara los errores de validación sin mostrar información innecesaria.

#### 5.2.2 Rendimiento:

- **RNF-03:** La navegación del prototipo debe mantener una respuesta fluida en los recorridos principales y evitar esperas artificiales innecesarias.

#### 5.2.3 Arquitectura y Escalabilidad:

- **RNF-04:** La organización del prototipo debe permitir incorporar nuevas pantallas o módulos sin alterar de forma innecesaria los flujos existentes.

- **RNF-05:** Los componentes y responsabilidades de interfaz deben mantenerse organizados para facilitar futuras ampliaciones.

#### 5.2.4 Documentación e Integración:

- **RNF-06:** La documentación funcional, los actores, permisos, estados, reglas y casos de uso deben mantener trazabilidad y terminología consistente.

- **RNF-07:** El prototipo debe representar de forma coherente los flujos definidos en la documentación, sin depender de una integración backend productiva.

#### 5.2.5 Disponibilidad y Despliegue:

- **RNF-08:** El prototipo frontend debe poder ejecutarse en un entorno accesible para su revisión académica cuando se realice la etapa de prototipado.

#### 5.2.6 Usabilidad y Experiencia de Usuario:

- **RNF-09:** Los formularios y pantallas deben presentar información organizada, clara y comprensible para usuarios no técnicos.

- **RNF-10:** Los procesos principales deben guiar al usuario mediante pasos secuenciales y mensajes de estado comprensibles.

- **RNF-11:** La navegación debe mantener nombres, acciones y estados consistentes entre las distintas vistas.

#### 5.2.7 Integridad y Auditoría:

- **RNF-12:** La representación funcional debe conservar coherencia entre usuarios, cursos, módulos, inscripciones y progreso.

- **RNF-13:** Las decisiones de auditoría y los cambios de estado relevantes deben quedar visibles de forma consistente dentro del flujo simulado.

- **RNF-14:** La interfaz debe validar la información obligatoria antes de permitir avanzar en procesos como registro, envío de cursos e inscripciones.

### 5.3 Estados del Sistema:

**Estados de Cursos:**

- **BORRADOR:** Curso en preparación. No es visible para alumnos y puede ser editado por su Profesor propietario.

- **EN REVISIÓN:** Curso nuevo o modificado enviado a auditoría administrativa. Los cambios aún no se consideran aprobados.

- **CAMBIOS SOLICITADOS:** La auditoría detectó correcciones necesarias. El Profesor puede ajustar el curso y volver a enviarlo.

- **RECHAZADO:** La solicitud fue rechazada por el Administrador y no se publica.

- **PUBLICADO:** Curso aprobado y disponible para consulta e inscripción de alumnos.

- **PAUSADO:** Curso temporalmente deshabilitado; no admite nuevas inscripciones mientras permanezca pausado.

- **FINALIZADO:** Curso cuyo ciclo fue cerrado y ya no admite nuevas inscripciones.

**Estados de Inscripción:**

- **PENDIENTE:** Solicitud registrada y a la espera de decisión administrativa.

- **APROBADA:** Solicitud aceptada; el Alumno obtiene acceso al curso.

- **RECHAZADA:** Solicitud denegada; el Alumno no obtiene acceso.

- **CANCELADA:** Inscripción anulada posteriormente y sin acceso activo al curso.

**Estados de Progreso:**

- **NO INICIADO:** La inscripción está aprobada, pero todavía no se registró avance.

- **EN PROGRESO:** El Alumno comenzó el curso y posee avance parcial.

- **COMPLETADO:** El Alumno alcanzó el 100 % del progreso definido para el curso.

Transiciones principales: BORRADOR -> EN REVISIÓN -> PUBLICADO / CAMBIOS SOLICITADOS / RECHAZADO; CAMBIOS SOLICITADOS -> EN REVISIÓN; PUBLICADO -> EN REVISIÓN cuando el Profesor confirma modificaciones; PUBLICADO -> PAUSADO -> PUBLICADO; PUBLICADO o PAUSADO -> FINALIZADO. Inscripción: PENDIENTE -> APROBADA / RECHAZADA; APROBADA -> CANCELADA.

### 5.4 Matriz de Permisos:

| **Funcionalidad** | **Alumno** | **Profesor** | **Admin** |
| --- | --- | --- | --- |
| Registrarse públicamente | Sí | Sí | No |
| Iniciar sesión | Sí | Sí | Sí |
| Consultar cursos publicados | Sí | Sí | Sí |
| Crear curso | No | Sí | Sí, como gestión administrativa |
| Guardar curso como borrador | No | Sí, propio | Sí |
| Editar curso | No | Sí, únicamente propio y sujeto a nueva auditoría | Sí |
| Enviar curso a revisión | No | Sí, propio | Sí |
| Auditar curso | No | No | Sí |
| Pausar / reanudar curso | No | Sí, propio | Sí |
| Finalizar curso | No | No | Sí |
| Solicitar inscripción | Sí | No | No |
| Resolver solicitudes de inscripción | No | No | Sí |
| Cancelar inscripción | Sí, propia | No | Sí |
| Ver progreso | Sí, propio | Sí, alumnos de sus cursos | Sí |
| Gestionar usuarios | No | No | Sí |
| Consultar reportes | No | No | Sí |

### 5.5 Reglas de Negocio:

- **RN-01:** Solo ALUMNO y PROFESOR pueden registrarse mediante el registro público; las cuentas ADMINISTRADOR se gestionan internamente.

- **RN-02:** Un usuario debe completar la información personal obligatoria para finalizar su registro.

- **RN-03:** Un Profesor solo puede modificar cursos creados por él mismo.

- **RN-04:** Un curso debe poseer al menos un módulo antes de ser enviado a revisión.

- **RN-05:** La duración total de un curso es la suma de las duraciones de sus módulos y no se carga manualmente.

- **RN-06:** Un curso creado o modificado por un Profesor debe superar auditoría administrativa antes de considerarse PUBLICADO.

- **RN-07:** Cuando una auditoría solicita cambios, el Profesor debe corregir el curso y volver a enviarlo a revisión.

- **RN-08:** Solo los cursos PUBLICADOS admiten nuevas solicitudes de inscripción.

- **RN-09:** Un Alumno no puede mantener más de una inscripción activa o pendiente para el mismo curso.

- **RN-10:** Toda nueva solicitud de inscripción comienza en estado PENDIENTE y requiere resolución administrativa.

- **RN-11:** El Alumno obtiene acceso al curso únicamente cuando su inscripción se encuentra APROBADA.

- **RN-12:** Un curso PAUSADO o FINALIZADO no admite nuevas solicitudes de inscripción.

- **RN-13:** El progreso se expresa mediante porcentaje asociado a módulos o actividades completadas: 0 % corresponde a NO INICIADO, entre 1 % y 99 % a EN PROGRESO y 100 % a COMPLETADO.

- **RN-14:** El Profesor puede consultar únicamente el progreso de alumnos asociados a sus propios cursos.

- **RN-15:** Los módulos deben pertenecer a un curso existente.

- **RN-16:** Los reportes administrativos básicos deben respetar el rango de fechas definido mediante Desde y Hasta.
