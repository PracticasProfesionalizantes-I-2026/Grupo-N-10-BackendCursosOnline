# **LUMEN**

**Sistema de Gestión de Cursos Online (LMS)**

Actores y Casos de Uso

|  |  |
| --- | --- |
| **Materia** | Práctica Profesionalizante I |
| **Integrantes** | Astore Rodrigo - Ferrino Nahuel |
| **Carrera** | Tecnicatura Superior en Desarrollo de Software |
| **Año** | 2026 |

*Documentación académica revisada - Septiembre 2026*

## 1. Actores Identificados

**Alumno:** Usuario que consulta cursos PUBLICADOS, solicita y consulta inscripciones propias, accede a cursos con inscripción APROBADA, realiza módulos o actividades, consulta su progreso y cancela sus inscripciones APROBADAS.

**Profesor:** Usuario que crea y mantiene cursos propios, agrega módulos, envía cursos o modificaciones a auditoría, consulta sus solicitudes y observaciones, pausa o reanuda cursos propios y consulta el progreso de sus alumnos. También puede consultar cursos PUBLICADOS.

**Administrador:** Usuario con cuenta gestionada internamente que gestiona usuarios y cursos, crea y edita cursos desde su gestión, audita solicitudes, resuelve y cancela inscripciones, pausa, reanuda y finaliza cursos, consulta progreso y reportes básicos.

El Desarrollador (equipo técnico) es un interesado del proyecto, pero no un actor funcional del sistema. Las funciones del creador o de otros administradores para gestionar cuentas internas se representan mediante el rol Administrador.

Estos casos describen el comportamiento funcional de LUMEN. El prototipo frontend simula los recorridos con datos locales. La API ejecuta las reglas y guarda los resultados en SQLite. Cada caso distingue las diferencias de transporte o de alcance que afectan su recorrido.

## 2. Casos de Uso por Actor

**Alumno:**

- CU-01 - Registrarse

- CU-02 - Iniciar Sesión

- CU-06 - Solicitar Inscripción a un Curso

- CU-08 - Consultar Progreso

- CU-11 - Consultar Cursos Publicados

- CU-12 - Consultar Inscripciones y Acceder a Cursos

- CU-13 - Cancelar Inscripción

- CU-16 - Realizar Módulos o Actividades del Curso

**Profesor:**

- CU-01 - Registrarse

- CU-02 - Iniciar Sesión

- CU-03 - Solicitar Creación de Curso

- CU-05 - Actualizar Curso Creado

- CU-08 - Consultar Progreso

- CU-11 - Consultar Cursos Publicados

- CU-14 - Pausar o Reanudar Curso

**Administrador:**

- CU-02 - Iniciar Sesión

- CU-03 - Solicitar Creación de Curso

- CU-04 - Auditar Solicitud de Curso

- CU-05 - Actualizar Curso Creado

- CU-07 - Gestionar Solicitudes de Inscripción

- CU-08 - Consultar Progreso

- CU-09 - Gestionar Usuarios

- CU-10 - Consultar Reportes

- CU-11 - Consultar Cursos Publicados

- CU-13 - Cancelar Inscripción

- CU-14 - Pausar o Reanudar Curso

- CU-15 - Finalizar Curso

## 3. Especificaciones de los Casos de Uso

### CU-01 – Registrarse:

#### 1. Información General:

- **Nombre:** Registrarse

- **Id / Código:** CU-01

- **Descripción:** Permite que una persona cree una cuenta pública como Alumno o Profesor, registre sus credenciales y complete la información personal requerida.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Alumno / Profesor

- **Disparador:** El usuario selecciona la opción de registro desde la página principal.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El usuario no debe poseer una cuenta registrada con el mismo email.

- El registro público se encuentra disponible únicamente para Alumno y Profesor.

**Postcondiciones:**

- La cuenta queda registrada con el rol seleccionado.

- Los datos personales obligatorios quedan asociados a la cuenta.

- El usuario queda habilitado para iniciar sesión.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El usuario selecciona la opción de registro.

2. El sistema solicita seleccionar el tipo de cuenta: Alumno o Profesor. La API rechaza su omisión con HTTP 400 y no crea una cuenta.

3. El usuario ingresa email y contraseña.

4. El sistema valida que el email no se encuentre registrado y que los campos obligatorios estén completos.

5. El usuario completa Nombre, Apellido, DNI, Teléfono, Dirección y Código Postal.

6. El sistema valida la información personal. La API comprueba formato de email, campos obligatorios y longitudes definidas por el modelo. El alta exige los datos personales y el rol permitido. DNI y teléfono con caracteres de control internos reciben HTTP 400. Se aplican los formatos argentinos aprobados para DNI, teléfono y código postal. El correo mal formado, un campo obligatorio incompleto o una longitud superior al modelo dan HTTP 400. Si el correo ya existe, la API devuelve 409. El alta exitosa devuelve 201 con los datos de la cuenta. El prototipo conserva un derivado local de la contraseña para iniciar sesión. Si el cuerpo del alta supera el límite de transporte, la API responde HTTP 413 y no crea la cuenta. Para cuentas nuevas, el DNI tiene 7 u 8 dígitos, el teléfono argentino usa de 10 a 15 dígitos y el código postal tiene cuatro dígitos o un CPA de ocho caracteres. La contraseña admite de 8 a 128 caracteres sin exigir composición; los datos inválidos reciben HTTP 400 sin crear la cuenta.

7. El sistema muestra una revisión de los datos ingresados.

8. El usuario confirma el registro.

9. El sistema registra la cuenta y muestra una confirmación.

**Flujos Alternativos:**

**A1. Email ya registrado**

4.1. El sistema detecta que el email ya se encuentra registrado.

4.2. El sistema informa el error y solicita utilizar otro email.

**A2. Información obligatoria incompleta**

6.1. El sistema detecta campos obligatorios incompletos.

6.2. El sistema identifica los campos pendientes.

6.3. El usuario completa o corrige la información y continúa.

### CU-02 – Iniciar Sesión:

#### 1. Información General:

- **Nombre:** Iniciar Sesión

- **Id / Código:** CU-02

- **Descripción:** Permite que un usuario registrado acceda al sistema y sea dirigido al panel correspondiente según su rol.

- **Referencias funcionales:** RF-05 a RF-07 y RF-29 a RF-33; matriz de permisos.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Alumno / Profesor / Administrador

- **Disparador:** El usuario selecciona la opción "Iniciar Sesión".

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El usuario debe poseer una cuenta registrada y habilitada.

**Postcondiciones:**

- El usuario accede al sistema.

- El sistema identifica su rol y muestra únicamente las funcionalidades permitidas.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El usuario selecciona "Iniciar Sesión".

2. El sistema solicita email y contraseña.

3. El usuario ingresa sus credenciales.

4. El sistema valida las credenciales. La API verifica la contraseña antes de informar si la cuenta está deshabilitada. Una contraseña incorrecta recibe 401 y una válida en una cuenta inactiva recibe 403. Si una misma IP supera 20 intentos en una ventana de 60 segundos, la API responde 429 con Retry-After y el usuario debe esperar antes de reintentar. Las credenciales demo de la API solo se admiten en entornos de demostración. Un correo inexistente también pasa por una verificación de contraseña de costo comparable y recibe HTTP 401. Si el cuerpo supera el límite de transporte, la API devuelve HTTP 413 sin iniciar sesión. Las cuentas existentes siguen autenticándose con su contraseña anterior; la política de alta nueva no se aplica al login.

5. El sistema identifica el rol asociado a la cuenta.

6. El sistema habilita las funcionalidades correspondientes.

7. El sistema redirige al Panel de Alumno, Profesor o Administrador según corresponda y muestra sus accesos permitidos.

8. El Alumno puede elegir cursos disponibles (CU-11), solicitudes e inscripciones (CU-12) o progreso (CU-08). El Profesor puede elegir sus cursos y solicitudes de auditoría (CU-03 y CU-05) o seguimiento de alumnos (CU-08). El Administrador puede elegir usuarios, cursos, auditorías, inscripciones y reportes (CU-09, CU-03, CU-05, CU-04, CU-07 y CU-10).

9. El usuario selecciona una función y el sistema inicia el caso de uso correspondiente, manteniendo las restricciones de su rol.

**Flujos Alternativos:**

**A1. Credenciales incorrectas**

4.1. El sistema detecta que las credenciales no son válidas.

4.2. El sistema informa el error.

4.3. El usuario puede volver a ingresar sus credenciales.

**A2. Campos vacíos**

4.1. El sistema detecta campos obligatorios sin completar.

4.2. El sistema informa qué información falta y no permite continuar.

### CU-03 – Solicitar Creación de Curso:

#### 1. Información General:

- **Nombre:** Solicitar Creación de Curso

- **Id / Código:** CU-03

- **Descripción:** Permite que un Profesor o Administrador prepare un curso, lo guarde como borrador o lo envíe a revisión administrativa una vez completada la información requerida y sus módulos.

- **Referencias funcionales:** RF-08 a RF-13; RN-03 a RN-06 y RN-15; permisos de creación, borrador y envío del Administrador.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Profesor / Administrador

- **Actores secundarios:** Administrador, en la auditoría posterior del CU-04.

- **Disparador:** El Profesor o Administrador selecciona "Crear Curso" desde su panel.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El Profesor o Administrador debe haber iniciado sesión.

**Postcondiciones:**

- Si guarda, el curso queda en BORRADOR.

- Si envía correctamente, el curso queda EN REVISIÓN y se genera una solicitud de auditoría.

- La duración total queda calculada a partir de los módulos.

- Si el Profesor crea el curso, queda identificado como su propietario. El Administrador actúa desde la gestión administrativa prevista en la matriz de permisos.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Profesor o Administrador selecciona "Crear Curso".

2. El sistema muestra el formulario del curso.

3. El Profesor o Administrador completa título, descripción, categoría, nivel, modalidad, cupo máximo, objetivos de aprendizaje y requisitos previos sugeridos.

4. El Profesor o Administrador agrega al menos un módulo.

5. Para cada módulo, completa nombre, descripción y duración y agrega el contenido o recursos previstos.

6. El sistema calcula la duración total como suma de las duraciones de los módulos.

7. El Profesor o Administrador selecciona "Enviar a revisión".

8. El sistema valida la información obligatoria y la existencia de al menos un módulo. Si el cuerpo de creación supera el límite de transporte, la API responde HTTP 413 y no crea el borrador. El cupo es positivo. Se admiten como máximo 100 módulos, 30 objetivos, 30 requisitos, 50 recursos por módulo y los máximos de texto aprobados; una entrada excedida recibe HTTP 400 sin guardar el curso.

9. El sistema cambia el curso a EN REVISIÓN y registra la solicitud de auditoría. En la API el guardado crea primero un BORRADOR con POST /api/courses y responde 201. El envío posterior mediante POST /api/courses/{id}/submit-review responde 200 y genera la auditoría. Si el envío falla, la creación ya confirmada se conserva como borrador. El prototipo representa el alta como Profesor. El alta administrativa está disponible en la API.

10. El sistema informa que el curso fue enviado a revisión.

**Flujos Alternativos:**

**A1. Guardar curso como borrador**

7.1. El Profesor o Administrador decide no enviar el curso.

7.2. Selecciona "Guardar como borrador".

7.3. El sistema conserva la información y deja el curso en BORRADOR.

7.4. El usuario puede retomar posteriormente el BORRADOR mediante CU-05; guardarlo no publica el curso ni inicia una auditoría.

**A2. Curso sin módulos**

8.1. El sistema detecta que el curso no posee módulos.

8.2. El sistema informa que debe existir al menos un módulo y mantiene el curso sin enviar.

**A3. Información obligatoria incompleta**

8.1. El sistema detecta información obligatoria faltante.

8.2. El sistema identifica los datos pendientes.

8.3. El Profesor o Administrador corrige la información antes de volver a enviar.

### CU-04 – Auditar Solicitud de Curso:

#### 1. Información General:

- **Nombre:** Auditar Solicitud de Curso

- **Id / Código:** CU-04

- **Descripción:** Permite que un Administrador revise una solicitud de creación o modificación de curso y determine su resultado.

- **Referencias funcionales:** RF-14 y RF-15; RN-06 y RN-07; estados de cursos.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Administrador

- **Actores secundarios:** Profesor o Administrador que preparó el curso o sus modificaciones.

- **Disparador:** El Administrador selecciona una solicitud de auditoría de un curso en estado EN REVISIÓN desde su panel.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El Administrador debe haber iniciado sesión.

- Debe existir una solicitud de auditoría cuyo curso esté EN REVISIÓN.

**Postcondiciones:**

- La auditoría queda resuelta.

- El curso cambia a PUBLICADO, CAMBIOS SOLICITADOS o RECHAZADO según la decisión.

- El usuario que preparó el curso o sus modificaciones puede visualizar el estado resultante en sus solicitudes de auditoría.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Administrador accede a la sección de auditorías.

2. El sistema muestra las solicitudes de auditoría cuyos cursos están EN REVISIÓN.

3. El Administrador selecciona una solicitud.

4. El sistema muestra el contenido guardado al enviar la solicitud, incluidos los módulos. En la API el Administrador o Profesor solicitante puede consultar ese envío por su identificador. Las auditorías antiguas sin copia muestran su decisión sin atribuirles contenido. Las listas de auditorías admiten paginación optativa.

5. El Administrador revisa la información.

6. El Administrador selecciona "Aprobar".

7. El sistema registra la decisión y cambia el curso a PUBLICADO. La API exige una decisión explícita; si falta o es inválida, responde HTTP 400 y conserva el estado anterior. La observación es obligatoria al solicitar cambios. La aprobación reemplaza la publicación vigente. Si se rechaza la revisión o se solicitan cambios, se conserva la publicación anterior si existe. La revisión rechazada y su decisión quedan registradas. El autor puede recuperar el contenido como un nuevo borrador mediante CU-05. La revisión rechazada y su decisión se conservan. Si el cuerpo de la decisión supera el límite de transporte, la API responde HTTP 413 sin resolver la auditoría. Al aprobar una nueva publicación, el cupo no puede quedar por debajo de los alumnos aprobados; el conflicto conserva la publicación previa.

8. El sistema informa que la auditoría fue aprobada.

**Flujos Alternativos:**

**A1. Rechazar solicitud**

6.1. El Administrador selecciona "Rechazar".

6.2. El sistema registra la decisión.

6.3. La revisión queda RECHAZADA. Si ya había una publicación, sigue disponible según el estado operativo. El solicitante puede iniciar la recuperación de CU-05.

**A2. Solicitar cambios**

6.1. El Administrador detecta información que debe corregirse.

6.2. Selecciona "Solicitar cambios" e indica la observación correspondiente.

6.3. El sistema cambia el curso a CAMBIOS SOLICITADOS.

6.4. El usuario que preparó el curso puede consultar la observación, corregirlo y volver a enviarlo mediante CU-05, respetando los permisos de su rol.

**A3. La solicitud ya no corresponde a un curso EN REVISIÓN**

6.1. Antes de registrar la decisión, el sistema detecta que el curso ya no está EN REVISIÓN.

6.2. El sistema muestra el estado actual y no aplica una nueva decisión sobre esa solicitud.

### CU-05 – Actualizar Curso Creado:

#### 1. Información General:

- **Nombre:** Actualizar Curso Creado

- **Id / Código:** CU-05

- **Descripción:** Permite retomar un BORRADOR o modificar un curso PUBLICADO o con CAMBIOS SOLICITADOS. El Profesor actúa únicamente sobre cursos propios y el Administrador desde la gestión general de cursos. Guardar un BORRADOR y enviar modificaciones a auditoría son resultados diferentes.

- **Referencias funcionales:** RF-09 a RF-16 y RF-31; RN-03 a RN-07 y RN-15; permiso de edición del Administrador.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Profesor / Administrador

- **Actores secundarios:** Administrador, en la auditoría posterior del CU-04.

- **Disparador:** El Profesor accede a sus cursos o solicitudes de auditoría, o el Administrador a la gestión de cursos, y selecciona "Editar Curso".

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El usuario debe haber iniciado sesión como Profesor o Administrador.

- El curso debe existir. Si actúa un Profesor, debe pertenecerle.

- Para este flujo, el curso está PUBLICADO o en CAMBIOS SOLICITADOS; la edición de un BORRADOR se describe en A3.

**Postcondiciones:**

- Al confirmar modificaciones de un curso PUBLICADO o correcciones de CAMBIOS SOLICITADOS, el curso queda EN REVISIÓN y se genera una solicitud de auditoría.

- Los cambios enviados no se consideran aprobados hasta que se resuelva CU-04.

- Si solo se guarda la edición de un BORRADOR, el curso conserva BORRADOR y no se inicia auditoría.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Profesor accede a sus cursos o a sus solicitudes de auditoría; el Administrador accede a la gestión de cursos.

2. El sistema muestra los cursos accesibles según el rol y, cuando corresponda, el estado de la auditoría y las observaciones recibidas.

3. El usuario selecciona el curso y solicita editarlo. El sistema verifica el rol y la propiedad cuando actúa un Profesor.

4. El sistema muestra los datos actuales y los módulos del curso. En la API la consulta de gestión entrega ETag. Guardar o recuperar requiere el ETag específico en If-Match. Una etiqueta ausente o el comodín devuelven 428. Una etiqueta obsoleta devuelve 412 sin escribir, incluso si otra edición ganó durante la persistencia. Una cabecera mal formada devuelve 400. Editar una revisión en EN REVISIÓN o RECHAZADA devuelve 409 con una etiqueta vigente. El Administrador edita la versión de trabajo mediante el mismo flujo de auditoría.

5. El usuario modifica los datos del curso o sus módulos, con nombre, descripción, duración y contenido o recursos.

6. El sistema valida los datos obligatorios y la existencia de al menos un módulo, y recalcula la duración total como suma de sus duraciones. Si el cuerpo del guardado supera el límite de transporte, la API responde HTTP 413 y conserva la revisión vigente. La edición permite reducir gradualmente colecciones o textos históricos que superan los topes nuevos, sin aumentar un exceso. El cupo no puede reducirse por debajo de las inscripciones aprobadas.

7. El usuario confirma el envío de los cambios a revisión.

8. El sistema registra las modificaciones, cambia el curso a EN REVISIÓN y genera la solicitud de auditoría del CU-04.

9. El sistema informa el resultado y lo muestra en las solicitudes de auditoría del usuario.

**Flujos Alternativos:**

**A1. Datos obligatorios incompletos o curso sin módulos**

6.1. El sistema identifica los datos pendientes o la ausencia de módulos.

6.2. No envía los cambios a revisión; el usuario corrige la información y vuelve al paso 6.

**A2. Corregir CAMBIOS SOLICITADOS**

4.1. El usuario consulta las observaciones de la auditoría y corrige la información indicada.

4.2. Continúa desde el paso 6; al confirmar el envío, el curso vuelve a EN REVISIÓN.

**A3. Retomar un BORRADOR**

3.1. El usuario selecciona un curso BORRADOR y actualiza sus datos o módulos.

3.2. Si elige "Guardar como borrador", el sistema conserva la información, recalcula la duración y mantiene BORRADOR.

3.3. Si elige "Enviar a revisión", continúa desde el paso 6; solo un envío válido cambia BORRADOR a EN REVISIÓN.

**A4. Profesor intenta editar un curso ajeno**

3.1. El sistema detecta que el curso no pertenece al Profesor.

3.2. Restringe la edición y conserva la información y el estado del curso.

**A5. Precondición de edición ausente u obsoleta**

- El editor recarga la revisión. La API devuelve 428 sin etiqueta específica y 412 si la etiqueta quedó obsoleta. El guardado no se aplica.

**A6. Recuperar una revisión rechazada**

- El Profesor propietario o Administrador confirma la recuperación. La API crea un BORRADOR nuevo a partir de la revisión rechazada y devuelve 200 con otro ETag. Conserva la decisión y, si existía, la publicación. Un estado que no sea RECHAZADO devuelve 409 con una etiqueta vigente.

### CU-06 – Solicitar Inscripción a un Curso:

#### 1. Información General:

- **Nombre:** Solicitar Inscripción a un Curso

- **Id / Código:** CU-06

- **Descripción:** Permite que un Alumno solicite su inscripción a un curso publicado. La solicitud requiere resolución administrativa antes de otorgar acceso.

- **Referencias funcionales:** RF-19 a RF-22; RN-08 a RN-10 y RN-12.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Alumno

- **Actores secundarios:** Administrador

- **Disparador:** El Alumno selecciona "Inscribirse" en el detalle de un curso PUBLICADO.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El Alumno debe haber iniciado sesión.

- El curso debe estar PUBLICADO.

- No debe existir una inscripción PENDIENTE o APROBADA del Alumno para ese curso.

**Postcondiciones:**

- La solicitud queda registrada en estado PENDIENTE.

- El Alumno puede consultar el estado de la solicitud mediante CU-12.

- Todavía no obtiene acceso al curso hasta su aprobación.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Alumno accede al catálogo de cursos.

2. El sistema muestra los cursos PUBLICADOS.

3. El Alumno selecciona un curso.

4. El sistema muestra el detalle del curso.

5. El Alumno selecciona "Inscribirse".

6. El sistema verifica que no exista una inscripción duplicada y que el curso admita inscripciones. Un borrador en revisión no cambia la disponibilidad de una publicación vigente. La pausa o finalización impide nuevas solicitudes. En la API, un conflicto de disponibilidad o una inscripción activa duplicada devuelve 409 sin registrar otra solicitud. Si el cuerpo supera el límite de transporte, la API responde HTTP 413 sin registrar la solicitud. La solicitud PENDIENTE no reserva plaza; el cupo se comprueba cuando el Administrador intenta aprobarla.

7. El sistema registra la solicitud en estado PENDIENTE.

8. El sistema informa que la solicitud fue enviada y queda a la espera de revisión.

**Flujos Alternativos:**

**A1. Inscripción duplicada**

6.1. El sistema detecta una solicitud PENDIENTE o inscripción APROBADA para el mismo curso.

6.2. El sistema informa que no puede generarse una nueva solicitud.

**A2. El Alumno decide no continuar**

5.1. El Alumno vuelve al catálogo o al detalle sin confirmar la inscripción.

5.2. El sistema no registra ninguna solicitud.

**A3. El curso no admite nuevas inscripciones**

6.1. El sistema detecta que el curso ya no está PUBLICADO, por ejemplo porque pasó a PAUSADO o FINALIZADO.

6.2. Informa que no admite nuevas solicitudes y no registra la inscripción.

### CU-07 – Gestionar Solicitudes de Inscripción:

#### 1. Información General:

- **Nombre:** Gestionar Solicitudes de Inscripción

- **Id / Código:** CU-07

- **Descripción:** Permite que un Administrador revise solicitudes de inscripción pendientes y las apruebe o rechace.

- **Referencias funcionales:** RF-23 y RF-24; RN-10 y RN-11.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Administrador

- **Actores secundarios:** Alumno

- **Disparador:** El Administrador accede a las solicitudes de inscripción pendientes.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El Administrador debe haber iniciado sesión.

- Para resolver una solicitud, la inscripción seleccionada debe estar PENDIENTE.

**Postcondiciones:**

- La solicitud queda APROBADA o RECHAZADA.

- Si es APROBADA, el Alumno obtiene acceso al curso.

- Si es RECHAZADA, el Alumno no obtiene acceso. El estado resultante puede consultarse mediante CU-12.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Administrador accede a la gestión de inscripciones.

2. El sistema muestra las solicitudes PENDIENTES.

3. El Administrador selecciona una solicitud.

4. El sistema muestra el Alumno y el curso relacionado.

5. El Administrador selecciona "Aprobar".

6. El sistema cambia la inscripción a APROBADA. Pausar conserva las pendientes y permite resolverlas. Finalizar las rechaza automáticamente con motivo. En la API se puede paginar el listado filtrado de pendientes. Si el cuerpo de la decisión supera el límite de transporte, la API devuelve HTTP 413 sin cambiar la inscripción. La aprobación ocupa una plaza solo si queda cupo, incluso ante decisiones simultáneas. El curso PAUSADO permite resolver pendientes; FINALIZADO rechaza sus pendientes con motivo y no permite aprobarlos. Cupo completo o decisión tardía dan HTTP 409 sin nueva aprobación.

7. El sistema habilita el curso dentro de los cursos del Alumno.

8. El sistema informa que la solicitud fue aprobada.

**Flujos Alternativos:**

**A1. Rechazar inscripción**

5.1. El Administrador selecciona "Rechazar".

5.2. El sistema cambia la inscripción a RECHAZADA.

5.3. El Alumno no obtiene acceso al curso.

**A2. No hay solicitudes PENDIENTES**

2.1. El sistema informa que no hay solicitudes pendientes para resolver.

2.2. El caso finaliza sin modificar inscripciones.

**A3. La solicitud ya fue resuelta**

5.1. Antes de aplicar la decisión, el sistema detecta que la inscripción ya no está PENDIENTE.

5.2. Muestra el estado actual y no vuelve a aprobarla o rechazarla. Si está APROBADA y debe cancelarse, corresponde CU-13.

### CU-08 – Consultar Progreso:

#### 1. Información General:

- **Nombre:** Consultar Progreso

- **Id / Código:** CU-08

- **Descripción:** Permite consultar el avance de un Alumno en un curso mediante porcentaje y estado de progreso.

- **Referencias funcionales:** RF-28; RN-13 y RN-14; permiso del Administrador para ver progreso.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Alumno / Profesor / Administrador

- **Disparador:** El usuario accede a la sección de progreso correspondiente.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El usuario debe haber iniciado sesión. El Alumno consulta solo su propio progreso; el Administrador puede consultar el progreso desde su gestión general.

- El Alumno debe poseer una inscripción APROBADA para consultar su progreso.

- El Profesor solo puede consultar alumnos de cursos propios.

**Postcondiciones:**

- El sistema muestra el porcentaje y el estado NO INICIADO, EN PROGRESO o COMPLETADO según corresponda.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Alumno accede a sus cursos, el Profesor al seguimiento de alumnos de sus cursos y el Administrador a la consulta de progreso desde su gestión general.

2. El usuario selecciona el curso o Alumno correspondiente. El sistema verifica que el Alumno consulte su propio progreso y que el Profesor consulte únicamente alumnos de cursos propios.

3. El sistema consulta el avance registrado al realizar módulos o actividades del curso (CU-16); esta consulta no modifica el progreso. El progreso se calcula para la inscripción aprobada a partir de los módulos de la publicación vigente. Una inscripción nueva tiene su propio avance y no reutiliza el de una cancelada. Al republicar, solo los módulos sin cambios conservan sus completados. Cambiar el orden no reinicia el avance. Un módulo nuevo o modificado empieza pendiente y los completados de módulos retirados quedan en el historial. La API permite paginar el listado de avance de inscripciones aprobadas.

4. El sistema muestra el porcentaje de avance.

5. El sistema muestra el estado asociado: 0 % NO INICIADO, 1-99 % EN PROGRESO o 100 % COMPLETADO.

**Flujos Alternativos:**

**A1. Sin inscripción aprobada**

3.1. El sistema detecta que el Alumno no posee una inscripción APROBADA.

3.2. El sistema no muestra progreso del curso.

**A2. Profesor intenta consultar un curso ajeno**

3.1. El sistema detecta que el curso no pertenece al Profesor.

3.2. El sistema restringe la consulta.

**A3. Alumno intenta consultar progreso ajeno**

2.1. El sistema detecta que el progreso solicitado corresponde a otro Alumno.

2.2. Restringe la consulta y mantiene disponible únicamente el progreso propio.

### CU-09 – Gestionar Usuarios:

#### 1. Información General:

- **Nombre:** Gestionar Usuarios

- **Id / Código:** CU-09

- **Descripción:** Permite que un Administrador consulte usuarios y gestione internamente cuentas de Administrador. La gestión de cursos se desarrolla en CU-03, CU-05, CU-14 y CU-15; su auditoría corresponde al CU-04.

- **Referencias funcionales:** RF-06 y RF-32; RN-01; permiso de gestión de usuarios.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Administrador

- **Disparador:** El Administrador selecciona la gestión de usuarios desde su panel.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El usuario debe haber iniciado sesión como Administrador.

- La gestión de cuentas de Administrador se realiza internamente y no está disponible en el registro público.

**Postcondiciones:**

- El Administrador consulta la información y el rol del usuario seleccionado.

- Si crea una cuenta de Administrador mediante A1, la cuenta queda asociada a ese rol y puede utilizar CU-02.

- Si abandona la operación o faltan los datos de acceso requeridos, no se crea la cuenta.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Administrador accede a su panel y selecciona la gestión de usuarios.

2. El sistema muestra los usuarios registrados y sus roles.

3. El Administrador selecciona un usuario para consultar su información.

4. El sistema muestra los datos de la cuenta y el rol asociado.

5. El Administrador revisa la información y vuelve al listado al terminar.

**Flujos Alternativos:**

**A1. Crear una cuenta de Administrador por gestión interna**

3.1. El Administrador selecciona la creación interna de una cuenta de Administrador.

3.2. El sistema solicita los datos de identificación y acceso de la cuenta, incluidos email y contraseña, y presenta el rol Administrador.

3.3. El Administrador completa los datos y confirma la creación.

3.4. El sistema verifica los datos obligatorios y que el email no esté registrado. Si el cuerpo del alta interna supera el límite de transporte, la API responde HTTP 413 sin crear la cuenta. El alta interna aplica los mismos formatos argentinos y la contraseña de 8 a 128 caracteres que el registro público; un dato inválido da HTTP 400 sin crear al Administrador.

3.5. El sistema registra la cuenta con rol Administrador y muestra una confirmación.

**A2. Datos incompletos o email ya registrado en la creación interna**

3.1. Durante A1, el sistema identifica los datos faltantes o el email ya registrado. La API valida el correo, los campos obligatorios y las longitudes. También rechaza caracteres de control internos en DNI o teléfono. El listado de usuarios admite paginación optativa. Responde 400 ante datos inválidos y 409 ante correo duplicado. En ambos casos conserva los usuarios existentes. El alta interna se ofrece en la API. El prototipo simula la consulta y el estado de cuentas.

3.2. No crea la cuenta; el Administrador corrige los datos y vuelve a confirmar.

**A3. Acceso con un rol sin permiso**

1.1. El sistema detecta que el usuario no es Administrador.

1.2. Restringe el acceso a la gestión de usuarios.

### CU-10 – Consultar Reportes:

#### 1. Información General:

- **Nombre:** Consultar Reportes

- **Id / Código:** CU-10

- **Descripción:** Permite que un Administrador consulte información básica del sistema aplicando un rango de fechas mediante los campos Desde y Hasta.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Administrador

- **Disparador:** El Administrador selecciona la sección "Reportes".

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El Administrador debe haber iniciado sesión.

**Postcondiciones:**

- El sistema muestra información básica correspondiente al rango de fechas seleccionado.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Administrador accede a "Reportes".

2. El sistema muestra los campos "Desde" y "Hasta".

3. El Administrador selecciona una fecha inicial y una fecha final.

4. El Administrador confirma el filtro.

5. El sistema valida que el rango de fechas sea válido. El prototipo toma fechas civiles en la zona del navegador. La API acepta una zona IANA mediante timeZone con fechas AAAA-MM-DD. Cuenta desde la medianoche de Desde hasta la siguiente a Hasta, excluida. Devuelve las medianoches de las fechas seleccionadas en UTC y la zona. Zona o límites inválidos devuelven 400. Las consultas sin zona conservan su contrato.

6. El sistema muestra información básica de usuarios registrados, cursos creados e inscripciones realizadas dentro del rango seleccionado.

**Flujos Alternativos:**

**A1. Rango de fechas inválido**

5.1. El sistema detecta que la fecha Desde es posterior a la fecha Hasta.

5.2. El sistema informa el error y solicita corregir el rango.

**A2. Sin resultados**

6.1. No existen registros para el rango seleccionado.

6.2. El sistema informa que no hay datos para mostrar.

### CU-11 – Consultar Cursos Publicados:

#### 1. Información General:

- **Nombre:** Consultar Cursos Publicados

- **Id / Código:** CU-11

- **Descripción:** Permite consultar el listado y el detalle de cursos PUBLICADOS sin tener que iniciar una solicitud de inscripción.

- **Referencias funcionales:** RF-19 y RF-30; matriz de consulta de cursos publicados.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Alumno / Profesor / Administrador

- **Disparador:** El usuario selecciona la consulta de cursos publicados desde su panel.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El usuario debe haber iniciado sesión como Alumno, Profesor o Administrador.

**Postcondiciones:**

- El usuario conoce la información de los cursos PUBLICADOS disponibles.

- La consulta no crea una inscripción, no otorga acceso a contenido reservado a inscripciones APROBADAS y no modifica cursos.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El usuario accede a la consulta de cursos publicados.

2. El sistema muestra el listado de cursos en estado PUBLICADO. La API devuelve la publicación vigente mientras el curso esté operativo, incluso durante la revisión de cambios. La API exige JWT. El prototipo ofrece también un catálogo público de demostración. El listado admite limit de 1 a 100 y offset desde cero. Sin esos parámetros devuelve el arreglo completo. El detalle de un curso que ya no está disponible devuelve 404.

3. El usuario selecciona un curso.

4. El sistema muestra su detalle y la información documentada del curso, incluidos sus módulos y duración total.

5. El usuario revisa la información y puede volver al listado. Si es Alumno y desea inscribirse, inicia CU-06.

**Flujos Alternativos:**

**A1. No hay cursos PUBLICADOS**

2.1. El sistema informa que no hay cursos disponibles para consultar.

2.2. El usuario puede volver a su panel.

**A2. El curso dejó de estar PUBLICADO**

4.1. El sistema informa que el curso seleccionado ya no está disponible en el catálogo.

4.2. Actualiza la consulta sin permitir una nueva solicitud de inscripción a ese curso.

**A3. Consulta como Profesor o Administrador**

5.1. El usuario puede consultar el curso, pero no dispone de la acción de solicitar inscripción.

### CU-12 – Consultar Inscripciones y Acceder a Cursos:

#### 1. Información General:

- **Nombre:** Consultar Inscripciones y Acceder a Cursos

- **Id / Código:** CU-12

- **Descripción:** Permite que el Alumno consulte sus solicitudes, identifique sus cursos con inscripción APROBADA y acceda al curso cuando corresponda.

- **Referencias funcionales:** RF-24, RF-26 y RF-30; RN-11; estados de inscripción.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Alumno

- **Disparador:** El Alumno selecciona solicitudes e inscripciones o sus cursos desde el panel.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El usuario debe haber iniciado sesión como Alumno.

- Solo puede consultar sus propias inscripciones.

**Postcondiciones:**

- El Alumno visualiza el estado PENDIENTE, APROBADA, RECHAZADA o CANCELADA de sus inscripciones, según corresponda.

- El acceso al curso se habilita únicamente cuando la inscripción está APROBADA. La consulta no modifica estados.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Alumno accede a sus solicitudes e inscripciones.

2. El sistema muestra sus solicitudes y los cursos asociados, con el estado de cada inscripción.

3. El Alumno consulta el estado y selecciona un curso con inscripción APROBADA.

4. El sistema verifica que la inscripción pertenezca al Alumno y permanezca APROBADA. La finalización conserva el acceso de inscripciones APROBADAS y su progreso.

5. El sistema permite acceder al curso y a sus módulos o actividades. Los nombres, el contenido y los módulos del alumno provienen de la publicación. La API admite paginación de las inscripciones propias. La pausa o finalización no revoca el acceso de una inscripción aprobada.

6. El Alumno puede consultar su progreso (CU-08), realizar módulos o actividades (CU-16) o solicitar cancelar la inscripción (CU-13).

**Flujos Alternativos:**

**A1. No existen solicitudes o inscripciones propias**

2.1. El sistema informa que no hay inscripciones para mostrar.

2.2. El Alumno puede consultar cursos PUBLICADOS mediante CU-11.

**A2. Inscripción PENDIENTE, RECHAZADA o CANCELADA**

4.1. El sistema muestra el estado correspondiente y no permite acceso al curso.

4.2. Si la inscripción está PENDIENTE, permanece a la espera de resolución en CU-07.

**A3. La inscripción pertenece a otro Alumno**

4.1. El sistema restringe la consulta y el acceso.

### CU-13 – Cancelar Inscripción:

#### 1. Información General:

- **Nombre:** Cancelar Inscripción

- **Id / Código:** CU-13

- **Descripción:** Permite cancelar una inscripción APROBADA. El Alumno solo puede cancelar una inscripción propia y el Administrador puede hacerlo desde la gestión de inscripciones.

- **Referencias funcionales:** RF-25; RN-11; permisos de cancelación y transición APROBADA -> CANCELADA.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Alumno / Administrador

- **Disparador:** El Alumno selecciona cancelar una inscripción propia o el Administrador selecciona cancelar una inscripción desde su gestión.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El usuario debe haber iniciado sesión como Alumno o Administrador.

- La inscripción seleccionada debe estar APROBADA. Si actúa el Alumno, debe pertenecerle.

**Postcondiciones:**

- La inscripción cambia de APROBADA a CANCELADA y se retira el acceso activo al curso.

- Si la operación no se confirma o no cumple las precondiciones, conserva su estado.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Alumno accede a sus inscripciones o el Administrador a la gestión de inscripciones.

2. El sistema muestra las inscripciones que el actor puede gestionar y su estado.

3. El actor selecciona una inscripción APROBADA y solicita cancelarla.

4. El sistema verifica el rol, la propiedad cuando actúa el Alumno y el estado APROBADA.

5. El sistema informa que la cancelación retira el acceso activo y solicita confirmación.

6. El actor confirma la cancelación.

7. El sistema cambia la inscripción a CANCELADA, retira el acceso activo y muestra la confirmación. La cancelación conserva el historial de esa inscripción. Una futura inscripción empieza con avance propio al aprobarse. Sin confirmación no se envía una mutación HTTP. Cancelar una inscripción aprobada libera una plaza para otra solicitud pendiente.

**Flujos Alternativos:**

**A1. El actor decide no cancelar**

6.1. El actor abandona la confirmación.

6.2. El sistema mantiene la inscripción APROBADA y el acceso activo.

**A2. Inscripción que no está APROBADA**

4.1. El sistema muestra el estado actual y no ejecuta la cancelación.

4.2. Este flujo no cambia una solicitud PENDIENTE ni una inscripción RECHAZADA o ya CANCELADA.

**A3. Rol sin permiso o inscripción ajena al Alumno**

4.1. El sistema restringe la operación y no modifica la inscripción.

### CU-14 – Pausar o Reanudar Curso:

#### 1. Información General:

- **Nombre:** Pausar o Reanudar Curso

- **Id / Código:** CU-14

- **Descripción:** Permite pausar un curso PUBLICADO o reanudar un curso PAUSADO. El Profesor actúa solo sobre cursos propios y el Administrador desde la gestión general.

- **Referencias funcionales:** RF-17; RN-03 y RN-12; permisos de pausa/reanudación y estados de cursos.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Profesor / Administrador

- **Disparador:** El Profesor o Administrador selecciona "Pausar" o "Reanudar" sobre un curso que puede gestionar.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El usuario debe haber iniciado sesión como Profesor o Administrador.

- El curso debe pertenecer al Profesor cuando ese sea el actor.

- Para pausar, el curso está PUBLICADO; para reanudar, está PAUSADO.

**Postcondiciones:**

- Al pausar, el curso pasa de PUBLICADO a PAUSADO y deja de admitir nuevas solicitudes de inscripción.

- Al reanudar, el curso pasa de PAUSADO a PUBLICADO y vuelve a admitir nuevas solicitudes.

- La operación cambia la disponibilidad del curso; no aprueba inscripciones ni sustituye la auditoría de modificaciones.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Profesor accede a sus cursos o el Administrador a la gestión de cursos.

2. El sistema muestra los cursos que puede gestionar y sus estados.

3. El actor selecciona un curso PUBLICADO y solicita pausarlo.

4. El sistema verifica los permisos y el estado del curso.

5. El actor confirma la pausa.

6. El sistema cambia el curso a PAUSADO e informa que no admite nuevas solicitudes de inscripción. La pausa conserva la publicación y las inscripciones ya registradas. Enviar o decidir una revisión no reanuda el curso. Para volver a admitir solicitudes debe ejecutarse la reanudación. La pausa conserva las solicitudes anteriores y el Administrador puede aprobarlas si queda cupo o rechazarlas.

**Flujos Alternativos:**

**A1. Reanudar un curso PAUSADO**

3.1. El actor selecciona un curso PAUSADO y solicita reanudarlo.

3.2. El sistema verifica los permisos, la propiedad cuando actúa el Profesor y el estado PAUSADO.

3.3. El actor confirma la reanudación.

3.4. El sistema cambia el curso a PUBLICADO y vuelve a habilitar nuevas solicitudes de inscripción.

**A2. Rol sin permiso o curso ajeno al Profesor**

4.1. El sistema restringe la operación y conserva el estado del curso.

**A3. Estado incompatible con la operación**

4.1. El sistema detecta que el curso no está PUBLICADO para pausar o PAUSADO para reanudar.

4.2. Muestra el estado actual y no realiza la transición.

**A4. No confirmar la operación**

5.1. El actor abandona la confirmación y el curso conserva su estado.

### CU-15 – Finalizar Curso:

#### 1. Información General:

- **Nombre:** Finalizar Curso

- **Id / Código:** CU-15

- **Descripción:** Permite que el Administrador cierre el ciclo de un curso PUBLICADO o PAUSADO.

- **Referencias funcionales:** RF-18; RN-12; permiso de finalización y estados de cursos.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Administrador

- **Disparador:** El Administrador selecciona "Finalizar" desde la gestión de cursos.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El usuario debe haber iniciado sesión como Administrador.

- El curso debe estar PUBLICADO o PAUSADO.

**Postcondiciones:**

- El curso pasa a FINALIZADO y deja de admitir nuevas solicitudes de inscripción.

- La finalización del curso no equivale a marcar COMPLETADO el progreso del Alumno ni a cancelar su inscripción.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Administrador accede a la gestión de cursos.

2. Selecciona un curso PUBLICADO o PAUSADO.

3. Solicita finalizar el curso.

4. El sistema muestra la operación y solicita confirmación.

5. El Administrador confirma la finalización.

6. El sistema verifica el permiso y el estado, cambia el curso a FINALIZADO e informa el cierre del ciclo y que no admite nuevas solicitudes de inscripción. La operación cierra el curso para nuevas solicitudes, conserva el acceso aprobado y no altera el avance y rechaza automáticamente las solicitudes pendientes con motivo. En la misma operación se rechazan todas las solicitudes PENDIENTES con el motivo «Curso finalizado.». No se alteran los accesos ni avances ya aprobados.

**Flujos Alternativos:**

**A1. El Administrador decide no finalizar**

5.1. Abandona la confirmación y el sistema conserva el estado del curso.

**A2. Estado incompatible**

6.1. El sistema detecta que el curso no está PUBLICADO ni PAUSADO.

6.2. Muestra el estado actual y no aplica la finalización.

**A3. Actor sin permiso**

3.1. El sistema detecta que el usuario no es Administrador.

3.2. Restringe la operación sin cambiar el curso.

### CU-16 – Realizar Módulos o Actividades del Curso:

#### 1. Información General:

- **Nombre:** Realizar Módulos o Actividades del Curso

- **Id / Código:** CU-16

- **Descripción:** Permite que el Alumno avance en un curso realizando sus módulos o actividades y que el sistema represente ese avance como porcentaje y estado de progreso.

- **Referencias funcionales:** RF-24 y RF-27; RN-11, RN-13 y RN-15; estados de progreso.

- **Autores / Fecha:** Astore Rodrigo, Ferrino Nahuel (Septiembre, 2026)

#### 2. Participantes e Inicio:

- **Actor principal:** Alumno

- **Disparador:** El Alumno accede a uno de sus cursos con inscripción APROBADA y selecciona un módulo o actividad.

#### 3. Condiciones Previas y Posteriores:

**Precondiciones:**

- El usuario debe haber iniciado sesión como Alumno y actuar sobre su propia inscripción APROBADA.

- El módulo o actividad debe corresponder al curso al que el Alumno tiene acceso.

**Postcondiciones:**

- El avance asociado a los módulos o actividades completadas queda representado en el progreso propio del Alumno.

- El porcentaje se corresponde con NO INICIADO (0 %), EN PROGRESO (1 % a 99 %) o COMPLETADO (100 %), conforme a RN-13.

- El progreso queda disponible para su consulta según permisos mediante CU-08, sin cambiar por sí mismo el estado del curso o de la inscripción.

#### 4. Flujo de Eventos:

**Flujo principal:**

1. El Alumno accede a un curso con inscripción APROBADA mediante CU-12.

2. El sistema muestra los módulos o actividades y su contenido o recursos.

3. El Alumno selecciona un módulo o actividad del curso.

4. El sistema verifica que la inscripción propia continúe APROBADA y que el módulo o actividad pertenezca a ese curso. La API registra únicamente módulos de la publicación vigente. Repetir la confirmación del mismo módulo conserva un solo registro y el mismo avance. Si se republica, se conserva el avance de módulos idénticos, aunque se reordenen. Los cambios de contenido, nombre, descripción, duración o recursos generan otro identificador. Los completados anteriores se preservan en el historial. Finalizar el curso conserva la finalización de módulos por alumnos con inscripción aprobada.

5. El Alumno realiza el módulo o actividad. Al completarlo, el sistema refleja el avance correspondiente dentro de su progreso. En el prototipo, el alumno puede volver a marcar el módulo como pendiente en sus datos locales. La API ofrece la finalización idempotente.

6. El sistema actualiza el porcentaje asociado a los módulos o actividades completadas y el estado definido en RN-13.

7. El Alumno consulta el avance resultante y puede continuar con el curso.

**Flujos Alternativos:**

**A1. El Alumno todavía no completa el módulo o actividad**

5.1. No se incorpora ese módulo o actividad como completado al porcentaje.

5.2. Se mantiene el progreso correspondiente a lo completado; si no existe avance, se muestra 0 % y NO INICIADO.

**A2. Inscripción sin acceso activo**

4.1. El sistema detecta que la inscripción ya no está APROBADA.

4.2. Restringe el acceso y no registra nuevo avance.

**A3. Módulo o actividad fuera del curso autorizado**

4.1. El sistema restringe la operación y no modifica el progreso.

**A4. Se alcanza el total del progreso definido para el curso**

6.1. El sistema muestra 100 % y COMPLETADO.
