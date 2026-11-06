# Auditoría y plan de correcciones de LUMEN

Auditoría original: 05/11/2026, `main` en `050a17e35c5fe08bf56c70fdea95f521726cf104`. Línea base del informe: 33 hallazgos (7 altos, 21 medios y 5 bajos), 350 pruebas aprobadas (101 unitarias y 249 de integración), build sin errores ni advertencias. El alta de inscripción, las fechas obligatorias de reportes y Bearer/OpenAPI ya estaban corregidos en esa revisión. La auditoría original distingue API, prototipo local y decisiones de negocio; este registro conserva los mismos IDs.

Las dos etapas se realizaron sobre el mismo commit y utilizó bases SQLite temporales. Cada fila contiene el último resultado comprobado. Un bloqueo afecta solo a la corrección que depende de esa decisión.

| ID | Categoría | Severidad | Evidencia y archivos de origen | Corrección prevista o decisión | Dependencias, pruebas y documentación | Estado | Validación |
|---|---|---|---|---|---|---|---|
| BUG-001 | Bug | Alto | `CourseReviewDecisionDTO` y `CourseService.ResolveReviewAsync`: el valor cero aprobaba ante `{}`. | Exigir decisión explícita sin cambiar los valores válidos. | API-003, TEST-001; HTTP, no mutación, CU-04 y OpenAPI. | COMPLETADO | HTTP 400 ante decisión ausente, nula o inválida y ante pedido de cambios sin observación. La regresión verifica que no se resuelve la auditoría ni cambia la publicación. La aprobación explícita funciona. |
| BUG-002 | Bug | Alto | `CourseService.UpdateAsync`: una revisión rechazada bloquea edición aunque siga publicada la anterior. | Acordar recuperación desde la publicación o acción de reapertura sin borrar historial. Se recomienda nuevo borrador separado si se aprueba. | DB-005; rechazo inicial/posterior, CU-04/05 y README. | BLOQUEADO | El rechazo conserva la revisión, su decisión y la publicación anterior. La edición de la revisión rechazada devuelve 409 en API y queda deshabilitada en el prototipo. Se propone un nuevo borrador, sujeto a decidir recuperación tras rechazo inicial y posterior. |
| BUG-003 | Bug | Alto | `Learning.tsx`, `courseService.ts` y `AppContext.tsx`: alumno lee módulos de trabajo. | Centralizar lectura de versión publicada y cálculo sobre sus módulos. | BUG-004/006; pruebas del prototipo, CU-12/16. | COMPLETADO | Las pantallas del alumno, incluido Mis solicitudes, y el avance para el profesor usan la publicación. Las pruebas verifican que editar un borrador no filtra módulos ni cambia el progreso publicado. |
| BUG-004 | Bug | Medio | `AppContext.tsx`: avance por alumno y curso se reutiliza al reinscribirse. | Asociar progreso a cada inscripción; preservar historial y tratar datos locales anteriores. | BUG-003; regresión cancelar/reinscribir, CU-06/08/13. | COMPLETADO | Pruebas de reinscripción y datos previos. El avance se identifica por inscripción. Los registros antiguos inequívocos se vinculan; los ambiguos se conservan separados del avance activo. |
| BUG-005 | Bug | Bajo | `Reports.tsx`: límite inclusivo a las 23:59:59.000. | Límite exclusivo del día siguiente, preservando el calendario local actual del prototipo. | VAL-003 no condiciona este borde; pruebas de medianoche, CU-10/RN-16. | COMPLETADO | Prueba de último milisegundo del día y exclusión del primer instante del día siguiente. Se mantiene el calendario local del prototipo. |
| BUG-006 | Bug | Medio | `courseService.ts`, `format.ts`, `AppContext.tsx`: versión previa equivale erróneamente a disponibilidad. | Compartir criterio: publicación vigente y curso operativo; mantener contenido aprobado durante revisión. | BUG-003; pausa/reanudación y revisión, CU-06/11/14/15. | COMPLETADO | Pruebas de catálogo con revisión y con pausa/finalización. Editar o aprobar un curso pausado conserva la pausa y la publicación hasta que corresponda reemplazarla; se requiere reanudar para admitir nuevas inscripciones. |
| BUG-007 | Bug | Medio | `CourseAndRequests.tsx`, `AppContext.tsx`: se cancela una pendiente. | Alinear UI y transición con CU-13: solo aprobadas. | BUG-004; pruebas de ambos roles y transición, CU-07/13. | COMPLETADO | El reducer acepta resolución de una pendiente y cancelación de una aprobada. La UI y el proveedor aplican acción y titularidad. La regresión rechaza cancelar solicitudes pendientes. |
| VAL-001 | Validación | Medio | `UserCreateDTO`: omitir rol crea Alumno. | Exigir presencia sin alterar roles admitidos. | VAL-002, TEST-001; registro HTTP, CU-01 y OpenAPI. | COMPLETADO | Registro sin rol o con rol no permitido responde 400 sin crear cuenta. OpenAPI requiere el rol y el alta válida conserva los roles admitidos. |
| VAL-002 | Validación | Alto | `ServiceGuard`, `AuthService`, `UserService`, `UserConfiguration`: email inválido y longitudes fuera del modelo aceptados. | Validar email evidente y límites ya configurados; conservar reglas de formato y contraseña no definidas. | VAL-001, TEST-001; alta pública e interna, CU-01/09 y RN-02. | BLOQUEADO | Parcial. Validación de correo, requeridos y longitudes de EF cubierta en alta pública e interna. El login conserva la compatibilidad con correos aceptados anteriormente. Faltan reglas aprobadas de DNI, teléfono y contraseña. |
| VAL-003 | Validación | Medio | `ReportService`: combina instantes y final del día. | Elegir fechas civiles con zona explícita (recomendado para la UI actual) o intervalo simétrico de instantes. | BUG-005, TEST-002; CU-10, RN-16, OpenAPI, Bruno. | COMPLETADO | Con timeZone la API interpreta fechas civiles IANA, verifica medianoche y devuelve el rango con zona. La regresión de un día de 23 horas verifica el último tick y el límite exclusivo. El contrato sin zona sigue disponible y el prototipo usa la zona del navegador. |
| VAL-004 | Validación | Medio | `CourseService.NormalizeList`: sin límites de tamaño de listas. | Definir límites de módulos/recursos con datos de uso; no imponer topes arbitrarios. | DB-003; límites y rechazo sin escritura, CU-03/05. | BLOQUEADO | Normalizar listas sigue sin definir un máximo. Falta medir el volumen esperado y aprobar el límite y el error de contrato. |
| SEC-001 | Seguridad | Alto | `DbInitializer`: credenciales demo siguen activas en Production con una base reutilizada. | Impedir uso de cuentas reservadas demo en entornos no demo, sin borrar filas; documentar separación y aprovisionamiento. | DB-001; base sembrada reutilizada y producción limpia, README/Bruno. | COMPLETADO | Login y tokens emitidos para cuentas demo se rechazan en Production aun reutilizando una base sembrada. La prueba conserva las cuentas y acredita el acceso con una cuenta independiente. |
| SEC-002 | Seguridad | Medio | `AppContext.tsx`, `Register.tsx`: contraseñas ingresadas en localStorage. | Eliminar persistencia en claro preservando los accesos de demostración. | DOC-001; verificación de almacenamiento, README frontend y CU-01/02. | COMPLETADO | PBKDF2-SHA256 con sal por cuenta. Regresiones para login, contraseña errónea, migración de contraseña en claro y acceso demo. El prototipo usa Web Crypto en localhost o HTTPS. |
| SEC-003 | Seguridad | Medio | `AuthService`, `Program.cs`: estado de cuenta antes de validar clave y sin limitador. | Evitar revelar el estado ante credencial inválida; no inventar umbrales/política de acceso. | VAL-002/API-002; login correcto/incorrecto e inactivo, CU-02. | COMPLETADO | Login verifica la contraseña antes de revelar inactividad. La API limita a 20 solicitudes por IP de conexión y por 60 segundos, devuelve 429 con Retry-After e ignora encabezados de IP aportados por el cliente. El despliegue distribuido requiere una política en el borde. |
| SEC-004 | Seguridad | Alto | `global.json`, runtime local 10.0.11 y aviso de Microsoft. | Actualizar en el nivel de servicio compatible y verificar entorno y referencias aplicables. | Build, suite, requisitos README/Bruno. | COMPLETADO | SDK mínimo 10.0.403, runtime 10.0.14 y referencias Microsoft aplicables actualizadas. Build sin advertencias, 367 pruebas y 0 paquetes vulnerables en el escaneo NuGet con transitivas. |
| SEC-005 | Seguridad | Bajo | `package-lock.json`: Vite 6.4.3 en rango afectado. | Actualizar dentro de Vite 6 a versión con parches compatibles y verificar lockfile. | TEST-003; build, lint y README frontend. | COMPLETADO | Vite 6.4.5 resuelve el hallazgo auditado y su aviso posterior. Build, lint y tests aprobados. El escaneo de producción devolvió cero avisos. El hallazgo nuevo de minimatch queda registrado por separado. |
| DB-001 | Base de datos | Medio | `DbInitializer`: siembra parcial y salida al encontrar usuarios. | Hacer la siembra demo atómica, sin modificar datos existentes. | SEC-001; rollback e idempotencia, README/Bruno. | COMPLETADO | La siembra usa una transacción. Una prueba induce un error intermedio, comprueba el rollback completo y vuelve a ejecutar la siembra con éxito. |
| DB-002 | Base de datos | Medio | Configuraciones EF de curso y finalización: restricciones entre versiones no completas. | Diseñar invariantes compatibles con identidad de módulo e historial; evitar cambio de esquema sin diagnóstico de datos y política de versiones. | DB-005/BUG-002; migración segura y DER. | BLOQUEADO | Parcial. Tercera migración SQLite valida los punteros de trabajo y publicación y preserva filas existentes. Integración verifica rechazo de referencia ajena y de reasignación de una revisión referenciada. Falta acordar identidad y retención de completados y el tratamiento de posibles datos históricos inconsistentes. |
| DB-003 | Base de datos | Medio | Listados en repositorios cargan grafos completos. | Reducir carga de lecturas sin cambiar contratos; paginación queda para decisión de API. | VAL-004; DTOs/orden sin cambios, tests de integración. | BLOQUEADO | Parcial. Catálogo y gestión de cursos aceptan paginación optativa; la consulta de gestión ya no carga módulos publicados duplicados. La medición sintética está registrada. Resta decidir adopción y paginación por omisión para otros listados sin romper consumidores. |
| DB-004 | Base de datos | Alto | `CourseUpdateDTO` no recibe versión esperada del editor. | Acordar precondición requerida u optativa, ETag/versionado y compatibilidad. Se recomienda ETag con transición documentada. | BUG-002/DB-005; edición tardía y concurrente, CU-05/OpenAPI. | BLOQUEADO | Parcial. GET y PUT de gestión ofrecen ETag; If-Match obsoleto devuelve 412 y conserva la primera edición. Bruno y las pruebas de integración reproducen el caso. Las llamadas heredadas sin cabecera todavía pueden sobrescribir una lectura obsoleta. Falta acordar la exigencia de la precondición. |
| DB-005 | Base de datos | Medio | `CourseReview` apunta a revisión editable después de solicitar cambios. | Acordar si se conserva solo decisión o instantánea inmutable por envío. | BUG-002/DB-002; trazabilidad CU-04/05/RNF-13. | BLOQUEADO | La auditoría refiere la revisión editable. Falta resolver si la evidencia incluye el contenido de cada envío y su retención. |
| API-001 | API | Medio | `AuthController.Register`: `Location` apunta al propio POST. | Mantener 201 y DTO sin URI inválida; no abrir lectura pública de datos. | VAL-001; comprobar header, CU-01/OpenAPI. | COMPLETADO | Registro 201 con DTO y sin Location inválido. La integración comprueba el contrato y los datos persistidos. |
| API-002 | API | Medio | `Program.cs`: excepciones inesperadas sin frontera común. | Respuesta 500 segura y trazable, manteniendo errores conocidos y binding. | SMELL-001; prueba de fallo inesperado, README/OpenAPI. | COMPLETADO | Un fallo inesperado inducido devuelve 500 application/problem+json con traceId, sin detalles de SQL ni mutación. OpenAPI publica el 500 de los endpoints afectados. |
| API-003 | API | Bajo | OpenAPI declara `observation` requerida siempre. | Alinear opcionalidad con la exigencia condicional del service. | BUG-001; pruebas OpenAPI/HTTP y CU-04. | COMPLETADO | OpenAPI exige decision y permite omitir observation. HTTP exige observación al solicitar cambios y permite aprobar sin ella. |
| SMELL-001 | Calidad | Medio | Cinco controladores duplican el mapa de excepciones. | Centralizar la traducción preservando códigos y DTOs. | API-002; regresiones de errores, README si corresponde. | COMPLETADO | Los cinco controladores comparten el mapa de errores en ApiExceptionMiddleware. La suite verifica errores conocidos, binding, permisos y la respuesta 500. |
| SMELL-002 | Calidad | Bajo | `ReportFilterDTO`, `EnrollmentUpdateDTO` sin referencias; `adminOnly` siempre falso. | Retirar código muerto tras verificar usos y OpenAPI. | Pruebas de rutas y roles; README si corresponde. | COMPLETADO | Retirados dos DTOs sin consumidores y una condición siempre falsa. Build, pruebas de autorización y OpenAPI aprobados. |
| TEST-001 | Pruebas | Medio | Suite de 350 pasa pese a omisiones sensibles. | Agregar regresiones con comprobación de no escritura. | BUG-001/VAL-001/002; trazabilidad CU-01/04/09. | COMPLETADO | Nuevas regresiones verifican auditoría y registro omitidos o inválidos sin escritura, límites admitidos, alta interna y login con datos heredados. La suite pasó de 350 a 367 casos. |
| TEST-002 | Pruebas | Medio | Bruno Reports comprueba fechas parseables y conteos no negativos. | Verificar período enviado, fixture conocida y escenarios negativos sin inventar calendario. | VAL-003; Bruno y CU-10. | COMPLETADO | Bruno CLI aprobó datos conocidos con fechas exactas, período vacío y rango invertido. Reejecución final con login: 4 requests y 6 tests aprobados. |
| TEST-003 | Pruebas | Bajo | Frontend sin suite y repo sin CI versionada. | Añadir pruebas de regresión y CI de build/lint/test sin despliegue. | BUG-003 a 007, README. | COMPLETADO | 7 regresiones del frontend aprobadas. La verificación de build, lint y pruebas está versionada para push y pull request. Se ejecutaron los comandos locales; la corrida remota queda para la publicación de los cambios. |
| DOC-001 | Documentación | Medio | Informe General Word/Markdown describe etapa anterior como actual. | Distinguir alcance histórico, API actual y prototipo no integrado, sin cambiar requisitos aprobados. | Consistencia Markdown/Word/README y trazabilidad. | COMPLETADO | Se cotejaron las versiones Word/Markdown del Informe General y del consolidado, y los 16 casos individuales. Los nuevos comportamientos se ubican en los pasos correspondientes. Se verificó render de los Word y se mantuvieron portadas, estilos, tablas y jerarquías. |
| DOC-002 | Documentación | Medio | CU-03, CU-09 y CU-11 difieren en códigos HTTP. | Ratificar separadamente creación/envío, duplicados y catálogo no disponible; no elegir autoridad arbitrariamente. | Contratos, Word/Markdown, Bruno, pruebas de aceptación. | COMPLETADO | CU-03, CU-09 y CU-11 reflejan el transporte vigente: 201 al crear y 200 al enviar, 409 ante duplicado y 404 para detalle no disponible. Se cotejaron Word, Markdown, pruebas de OpenAPI y Bruno. Las reglas funcionales aprobadas se conservaron. |
| DOC-003 | Documentación | Medio | Scalar presente, Swagger UI ausente; DER y niveles no encontrados. | Registrar evidencia y preparar DER técnico si corresponde; no afirmar equivalencia ni inventar niveles. | Modelo EF y consigna; README/Informe General. | BLOQUEADO | Parcial. Se incorporó el DER técnico del esquema EF vigente. Falta determinar con la cátedra si Scalar cumple la consigna y qué significan los niveles exigidos. |
| DOC-004 | Documentación | Medio | Cupo no limita aprobación y RN-12 solo bloquea nuevas solicitudes. | Definir cupo informativo o aplicado y acciones permitidas al pausar/finalizar. | BUG-002/006; concurrencia de última plaza si se aprueba, CU-06/07/12/14/15/16. | BLOQUEADO | Parcial. CU-06/07/12/14/15/16 documentan el cierre a nuevas solicitudes y el acceso aprobado conservado. Falta decidir si el cupo limita nuevas aprobaciones y qué debe ocurrir con pendientes ya existentes. |

## Cierre de la segunda etapa del 05/11/2026

Se retomaron los 33 hallazgos sobre `main` en `050a17e` y se preservaron los cambios de la etapa anterior. El cierre anterior figura en el historial.

| Estado | Hallazgos |
|---|---:|
| COMPLETADO | 24 |
| BLOQUEADO | 9 |
| NO REPRODUCIBLE | 0 |
| DESCARTADO CON JUSTIFICACIÓN | 0 |
| PENDIENTE / EN PROCESO | 0 |

No se modificaron los enunciados aprobados de RF, RNF ni RN. Los casos explicitan las condiciones técnicas compatibles, separan las acciones de interfaz de las respuestas HTTP y describen el resultado de los flujos cuyo contrato cambió.

## Historial de intervenciones

1. **Línea base.** Se constató `main` limpio en `050a17e`, mismo commit que la auditoría. Compilación con 0 advertencias y 0 errores. Pasaron las 101 pruebas unitarias y las 249 de integración existentes.
2. **Integridad y contratos.** Se exigieron decisión y rol explícitos, se alinearon alta y OpenAPI y se consolidó la traducción de excepciones. Se bloqueó el uso de identidades demo en Production y se volvió atómica la siembra. Primera comprobación dirigida del backend con 80 pruebas aprobadas.
3. **Prototipo.** Se separaron publicación, versión editable y estado operativo, se corrigieron la reinscripción, la cancelación y el límite de fechas, y se migraron las credenciales locales en claro. La revisión detectó una regresión posible al editar un curso pausado. Se preservó la pausa y se agregó el caso a las pruebas. Pasaron los 7 casos, lint y build.
4. **Dependencias y automatización.** Se actualizó Vite dentro de la rama 6 y se aplicaron actualizaciones npm compatibles. Se fijó el SDK .NET 10.0.403 y el runtime 10.0.14, probados con distribución oficial portátil y suma verificada. Una fixture de logging dependía de autenticarse con una identidad demo en Production. Se le asignó una cuenta independiente conservando sus aserciones. Con el runtime actualizado pasaron 18 casos dirigidos y luego 366 pruebas en total, con 0 errores ni advertencias de compilación. Se versionó la CI para backend y frontend.
5. **API y SQLite.** Las dos migraciones existentes se aplicaron sobre bases temporales, incluyendo las instancias de Bruno. Las pruebas de contrato revisaron OpenAPI servido. Bruno CLI 4.3.0 aprobó 19 requests y 54 tests de exposición, y 33 requests y 65 tests generales y de reportes. Luego de precisar el reporte positivo con los datos de semilla, se repitieron login y reportes, con 4 requests y 6 tests aprobados. La doble edición HTTP verificó la sobrescritura obsoleta de DB-004.
6. **Documentación y revisión.** Se sincronizaron Markdown y Word, y se agregó el DER técnico. Los dos Word se exportaron con Microsoft Word, con revisión visual de las páginas cambiadas y cotejo estructural del contenido ajeno a la edición. La paginación se mantuvo en 9 y 21 páginas. Se revisaron el diff y los archivos nuevos, y se contrastaron las pruebas con los comportamientos documentados.
7. **Segunda pasada y cierre.** Se verificó que «Mis solicitudes» leyera el título publicado. Se mantuvo en login la normalización anterior para usuarios ya registrados y se agregó una regresión de integración. La ejecución final de `dotnet build Lumen.slnx` dio 0 advertencias y 0 errores. Pasaron 10 casos dirigidos de cuentas y luego la suite completa de 367 casos (101 unitarios y 266 de integración). La repetición de `npm test`, `npm run lint` y `npm run build` fue satisfactoria. Se verificaron los 33 IDs, el inventario y `git diff --check`.
8. **Línea base de la segunda etapa.** Se revisaron los 21 completados y 12 bloqueados, el Informe General y consolidado en Word/Markdown, los 16 casos, reglas, endpoints y pruebas. Se preservó el árbol previo.
9. **Contratos e integridad.** Edición condicional, límite local de login, paginación optativa, modo de reportes con zona IANA y tercera migración no destructiva. La primera corrida dirigida aprobó 61 pruebas.
10. **Trazabilidad y coherencia.** Se reutilizó el formulario de edición para Administrador, se protegió el flujo local de decisiones y se actualizó cada caso de uso. Las notas nuevas coinciden en los 16 Markdown y en el Word consolidado.
11. **Validación de la segunda etapa.** Build del backend con 0 advertencias y 0 errores. Pasaron 374 pruebas .NET (101 unitarias y 273 de integración), ocho pruebas del frontend, lint y build. Se verificaron OpenAPI y las tres migraciones sobre SQLite temporal. Bruno CLI 4.3.0 aprobó 54 requests y 123 tests en dos instancias. NuGet directo y transitivo y npm de producción no devolvieron avisos. npm informó siete entradas moderadas de desarrollo del aviso de minimatch.
12. **Control documental.** El Informe General mantuvo 9 páginas. El consolidado pasó de 21 a 22 con las explicaciones incorporadas. Se revisaron las páginas renderizadas y se verificó que las partes de Word ajenas al cuerpo conservaran sus bytes.

## Decisiones del equipo pendientes

| IDs | Decisión y alternativa recomendada |
|---|---|
| BUG-002, DB-005 | Aprobar el flujo después de un rechazo inicial y de uno sobre una publicación previa. Recomendación: crear un nuevo borrador y conservar la revisión rechazada. Definir si cada envío requiere una instantánea inmutable, qué campos se guardan y por cuánto tiempo. La decisión ya se registra. |
| DB-002 | Acordar si el avance debe seguir el ID del módulo a través de revisiones y qué ocurre si cambia el contenido. Inspeccionar filas históricas con el diagnóstico del DER antes de reparar cualquier inconsistencia. |
| DB-003, VAL-004 | Decidir qué consumidores deben paginar y cuándo sería obligatorio. Fijar máximos de módulos, recursos y texto de listas a partir de datos de uso, con tratamiento explícito de cursos existentes. |
| DB-004 | Acordar la transición a `If-Match` obligatorio y qué clientes necesitan conservar edición incondicional. La recomendación es incorporar la cabecera a los clientes y luego definir la respuesta al omitirla. |
| VAL-002 | Definir países y formatos aceptados para DNI y teléfono, y la política de contraseñas de cuentas nuevas. Conservar la compatibilidad de las existentes. |
| DOC-003 | Obtener consigna o rúbrica de cátedra. Scalar y OpenAPI están verificados técnicamente. No se encontró base para afirmar equivalencia académica con Swagger UI ni para declarar niveles de complejidad. |
| DOC-004 | Definir si el cupo es informativo o restringe aprobaciones, y si las pendientes conservan su flujo después de pausar o finalizar. El acceso de las inscripciones ya aprobadas se preserva. Si se aplica cupo, probar la última plaza concurrente. |

## Hallazgos nuevos y límites de verificación

- La edición administrativa del prototipo ahora usa el formulario común de CU-05. El guardado de cambios conserva la publicación hasta su aprobación. La auditoría local rechaza decisiones duplicadas y pide observación al solicitar cambios.
- El prototipo conserva su alcance local. La creación de cursos como Administrador (CU-03), el alta interna (CU-09) y la consulta administrativa global de progreso (CU-08) se verifican en la API. En el prototipo faltan esas vistas administrativas. El catálogo público del prototipo puede consultarse sin sesión; el contrato de la API exige JWT. Estos contrastes se indican por caso.
- El límite de login es local por IP. Un proxy puede agrupar clientes; varias instancias multiplican el cupo. El límite agregado requiere configurar una capa compartida. La verificación de la contraseña antes de evaluar el estado evita revelar inactividad a quien no presenta una credencial válida. [Documentación de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0).
- La paginación optativa conserva la respuesta histórica. En una lista que cambia entre consultas, el desplazamiento puede mover elementos entre páginas. La medición de SQL sintético local con 400 cursos, 12 módulos por revisión, dos revisiones por curso y siete repeticiones arroja 57.600 filas y 121,18 ms de mediana antes, 4.800 filas y 8,21 ms con la consulta reducida, y 1.200 filas y 1,63 ms para una página de 100. Son filas intermedias y tiempo de SQLite en memoria, sin HTTP ni inferencia sobre producción.
- Las siete entradas moderadas de npm siguen derivando de un aviso de `minimatch` en herramientas de desarrollo. El parche requiere una versión mayor fuera del rango admitido por la dependencia. [Aviso de minimatch](https://github.com/advisories/GHSA-g3hv-75wg-m249).
- El SDK portátil 10.0.403 ejecuta runtime 10.0.14; el SDK del sistema sigue en 10.0.400. La CI está verificada por sus comandos locales. La ejecución remota queda pendiente.
- Las pruebas de escritura y migración utilizaron SQLite temporal. El árbol de trabajo permanece sobre el mismo commit de `main`.

## Matriz de trazabilidad

`CU` identifica el caso individual y su sección homóloga en el consolidado. Las referencias de prueba son suites reales salvo cuando se indica que la decisión o la ejecución está pendiente.

| Hallazgo | Requerimiento | Regla de negocio | Caso de uso | Código | Prueba | Markdown | Word | Estado |
|---|---|---|---|---|---|---|---|---|
| BUG-001 | RF-14/15 | RN-06/07 | CU-04 | CourseReviewDecisionDTO; CourseService | CourseCreationIntegrationTests; OpenApiContractTests | CU-04 | Consolidado CU-04 | COMPLETADO |
| BUG-002 | RF-14 a RF-16 | RN-06/07 | CU-04/CU-05 | CourseService; courseService.ts | CourseUpdateIntegrationTests; demo.test.mjs | CU-04/CU-05 | Consolidado CU-04/CU-05 | BLOQUEADO |
| BUG-003 | RF-15/24/26/27 | RN-06/11/13 | CU-08/CU-11/CU-12/CU-16 | courseService.ts; Learning.tsx; CourseAndRequests.tsx | demo.test.mjs | CU-08/CU-11/CU-12/CU-16 | Consolidado CU-08/CU-11/CU-12/CU-16 | COMPLETADO |
| BUG-004 | RF-24 a RF-28 | RN-09/11/13 | CU-06/CU-08/CU-12/CU-13 | AppContext; appReducer; courseService.ts | demo.test.mjs; EnrollmentCreationIntegrationTests | CU-06/CU-08/CU-12/CU-13 | Consolidado CU-06/CU-08/CU-12/CU-13 | COMPLETADO |
| BUG-005 | RF-33 a RF-35 | RN-16 | CU-10 | Reports.tsx; format.ts | demo.test.mjs | CU-10 | Consolidado CU-10 | COMPLETADO |
| BUG-006 | RF-15/17 a RF-20 | RN-08/12 | CU-06/CU-11/CU-14/CU-15 | courseService.ts; appReducer; AppContext | demo.test.mjs; EnrollmentCreationIntegrationTests | CU-06/CU-11/CU-14/CU-15 | Consolidado CU-06/CU-11/CU-14/CU-15 | COMPLETADO |
| BUG-007 | RF-25 | RN-11 | CU-13 | CourseAndRequests.tsx; appReducer | demo.test.mjs; Enrollments/07 | CU-13 | Consolidado CU-13 | COMPLETADO |
| VAL-001 | RF-01/03 | RN-01 | CU-01 | UserCreateDTO; AuthService | AccountValidationIntegrationTests; OpenApiContractTests; Auth/04 | CU-01 | Consolidado CU-01 | COMPLETADO |
| VAL-002 | RF-02/04/06 | RN-01/02 | CU-01/CU-02/CU-09 | ServiceGuard; UserFieldLimits; UserService | AccountValidationIntegrationTests | CU-01/CU-02/CU-09 | Consolidado CU-01/CU-02/CU-09 | BLOQUEADO |
| VAL-003 | RF-33 a RF-35 | RN-16 | CU-10 | ReportsController; ReportService; ReportDTOs | ReportsIntegrationTests; TraceabilityContractIntegrationTests; Reports/04 | CU-10 | Consolidado CU-10 | COMPLETADO |
| VAL-004 | RF-09/10/12 | RN-04/15 | CU-03/CU-05 | CourseService.NormalizeList | CourseCreationIntegrationTests cubre reglas existentes; límites pendientes | CU-03/CU-05 | Consolidado CU-03/CU-05 | BLOQUEADO |
| SEC-001 | RF-05/07 | Permisos por rol | CU-02 | DemoAccounts; JwtTokenService; Program | DemoSeedingIntegrationTests; JwtSecurityIntegrationTests | CU-02 | Consolidado CU-02 | COMPLETADO |
| SEC-002 | RF-01/05; RNF-07 | RN-01 | CU-01/CU-02 | demoCredentials; AppContext; Register | demo.test.mjs | CU-01/CU-02 | Consolidado CU-01/CU-02 | COMPLETADO |
| SEC-003 | RF-05/07 | Permisos por rol | CU-02 | AuthService; LoginRateLimit; AuthController | AccountValidationIntegrationTests; TraceabilityContractIntegrationTests; OpenApiContractTests | CU-02 | Consolidado CU-02 | COMPLETADO |
| SEC-004 | RNF-04/05 | No aplica | CU-02 | global.json; proyectos .NET | Build, suite completa y auditoría NuGet | README/registro | Actualización de servicio sin cambio del flujo funcional | COMPLETADO |
| SEC-005 | RNF-04/08 | No aplica | Transversal | package.json; package-lock.json | npm audit, lint, build y demo.test.mjs | README/registro | Parche de herramientas sin cambio del flujo funcional | COMPLETADO |
| DB-001 | RNF-07/12 | No aplica | 02 (semilla) | DbInitializer; CourseRepository | DemoSeedingIntegrationTests | README/registro | Atomicidad interna de semilla; mantiene el flujo de la demo | COMPLETADO |
| DB-002 | RF-10/24/27; RNF-12 | RN-11/13/15 | CU-03/CU-05/CU-08/CU-12/CU-16 | EnforceCourseRevisionOwnership; EnrollmentRepository | TraceabilityContractIntegrationTests; EnrollmentPersistenceIntegrationTests | CU-03/CU-05/CU-08/CU-12/CU-16 | Consolidado CU-03/CU-05/CU-08/CU-12/CU-16 | BLOQUEADO |
| DB-003 | RF-19/23/26/32; RNF-03 | RN-08/10/12 | CU-05/CU-07/CU-09/CU-11/CU-12 | CourseRepository; EnrollmentRepository; CoursesController | TraceabilityContractIntegrationTests; medición SQLite sintética | CU-05/CU-07/CU-09/CU-11/CU-12 | Consolidado CU-05/CU-07/CU-09/CU-11/CU-12 | BLOQUEADO |
| DB-004 | RF-16; RNF-12/13 | RN-06 | CU-05 | CourseEditTag; CourseService; CoursesController | TraceabilityContractIntegrationTests; CourseConcurrencyIntegrationTests; Courses/12 | CU-05 | Consolidado CU-05 | BLOQUEADO |
| DB-005 | RF-14/16; RNF-13 | RN-06/07 | CU-04/CU-05 | CourseReview; CourseRevision; CourseRepository | CourseUpdateIntegrationTests verifica decisión y reenvío; instantánea pendiente | CU-04/CU-05 | Consolidado CU-04/CU-05 | BLOQUEADO |
| API-001 | RF-01 a RF-04 | RN-01/02 | CU-01 | AuthController.Register | AccountValidationIntegrationTests; OpenApiContractTests | CU-01 | Consolidado CU-01 | COMPLETADO |
| API-002 | RNF-06/10 | No aplica | Transversal | ApiExceptionMiddleware; OpenAPI | ConcurrencyPersistenceFailureIntegrationTests; OpenApiContractTests | Contratos comunes en README y OpenAPI | Sin cambio de flujo funcional; error técnico transversal | COMPLETADO |
| API-003 | RF-14/15 | RN-06/07 | CU-04 | EnumContractSchemaTransformer; CourseReviewDecisionDTO | OpenApiContractTests; CourseCreationIntegrationTests | CU-04 | Consolidado CU-04 | COMPLETADO |
| SMELL-001 | RNF-05/06 | No aplica | Transversal | Controladores; ApiExceptionMiddleware | Suite HTTP y pruebas de OpenAPI | README/registro | Refactor conserva códigos de negocio; el 500 se documenta en API-002 | COMPLETADO |
| SMELL-002 | RNF-04/05 | No aplica | Transversal | DTOs de reportes/inscripciones; CourseService | Build, OpenAPI y autorización de cursos | README/registro | Código sin consumidores; no cambia el flujo | COMPLETADO |
| TEST-001 | RF-01 a RF-04/14/15 | RN-01/02/06/07 | CU-01/CU-04/CU-09 | DTOs de alta y decisión; servicios | AccountValidationIntegrationTests; CourseCreationIntegrationTests | CU-01, CU-04 y CU-09 | Consolidado CU-01, CU-04 y CU-09 | COMPLETADO |
| TEST-002 | RF-33 a RF-35 | RN-16 | CU-10 | Colección Reports | Bruno Reports/01–04; ReportsIntegrationTests | CU-10 | Consolidado CU-10 | COMPLETADO |
| TEST-003 | RNF-04 a RNF-08 | No aplica | Transversal | verification.yml; demo.test.mjs | Comandos de CI locales; ejecución remota pendiente | README/registro | Automatización interna sin cambio funcional | COMPLETADO |
| DOC-001 | RNF-06/07 | RN-01/02/06/07 | 01 a 16 | Informe General; consolidado; casos individuales | Cotejo de contenido y render Word | Informe General y CU individuales | Informe General y consolidado | COMPLETADO |
| DOC-002 | RF-06/08/13/19; RNF-06 | RN-01/06/08 | CU-03/CU-09/CU-11 | CoursesController; UsersController; catálogo | OpenApiContractTests; Bruno Courses/03,05, Users/03 y Catalog/02 | CU-03/CU-09/CU-11 | Consolidado CU-03/CU-09/CU-11 | COMPLETADO |
| DOC-003 | RNF-06 | Consigna de cátedra | Transversal | Program; MODELO_DE_DATOS | ApiIntegrationTests valida Scalar y OpenAPI; rúbrica ausente | MODELO_DE_DATOS y README | Informe General distingue alcance inicial | BLOQUEADO |
| DOC-004 | RF-09/17/18/20/23/24 | RN-10 a RN-12 | CU-06/CU-07/CU-12/CU-14/CU-15/CU-16 | CourseService; EnrollmentService; appReducer | EnrollmentCreationIntegrationTests; demo.test.mjs | CU-06/CU-07/CU-12/CU-14/CU-15/CU-16 | Consolidado CU-06/CU-07/CU-12/CU-14/CU-15/CU-16 | BLOQUEADO |

## Auditoría funcional de los 16 casos de uso

Se cotejaron actores, objetivos, disparadores, precondiciones, flujos, excepciones, postcondiciones y permisos de cada especificación con la API y el prototipo. La tabla resume la comprobación por caso. La columna Bruno indica recorridos disponibles; las pruebas automatizadas cubren errores adicionales.

| Caso y actor | RF | RN | Contrato | Prueba automatizada | Bruno | Estado y alcance |
|---|---|---|---|---|---|---|
| CU-01 · Registro Alumno/Profesor | RF-01 a RF-04 | RN-01/02 | `POST /api/auth/register` | AccountValidationIntegrationTests; RegistrationConcurrencyRegressionTests | Auth/04 | Rol y datos explícitos. 201; 400 o 409 conservan el padrón. |
| CU-02 · Login de los tres roles | RF-05/07/29 | Permisos por rol | `POST /api/auth/login` | AccountValidationIntegrationTests; TraceabilityContractIntegrationTests; JwtSecurityIntegrationTests | Auth/01–03 | 200 con token; 400, 401, 403 o 429 según el flujo. Los tokens demo solo sirven en entornos demo. |
| CU-03 · Creación y envío por Profesor/Admin | RF-08 a RF-13 | RN-03 a RN-06/15 | `POST /api/courses; POST /api/courses/{id}/submit-review` | CourseCreationIntegrationTests; CourseServiceTests; OpenApiContractTests | Courses/03,05; Exposición/04–05 | 201 crea borrador; 200 crea solicitud. El alta administrativa está en la API; el prototipo crea por Profesor. |
| CU-04 · Auditoría del Administrador | RF-14/15 | RN-06/07 | `GET /api/course-reviews; POST /api/course-reviews/{id}/decision` | CourseCreationIntegrationTests; CourseConcurrencyIntegrationTests; demo.test.mjs | Courses/09–11; Exposición/06–07 | 200 aplica decisión; 400/409 preservan estado. El rechazo conserva la publicación. Recuperación e instantáneas pendientes. |
| CU-05 · Edición Profesor propietario/Admin | RF-09 a RF-16/31 | RN-03 a RN-07/15 | `GET y PUT /api/courses/{id}; POST /api/courses/{id}/submit-review` | CourseUpdateIntegrationTests; TraceabilityContractIntegrationTests; demo.test.mjs | Courses/02,04,12,05 | Guardado y envío separados. 403 por curso ajeno, 409 por estado, 412 por etiqueta obsoleta. La publicación anterior sigue vigente. |
| CU-06 · Solicitud del Alumno | RF-19 a RF-22 | RN-08 a RN-10/12 | `POST /api/enrollments` | EnrollmentCreationServiceTests; EnrollmentCreationIntegrationTests; EnrollmentPersistenceIntegrationTests | Enrollments/01; Exposición/08 | 201 registra PENDIENTE. 409 por duplicado o cierre. Abandonar el formulario no muta. |
| CU-07 · Resolución por Administrador | RF-23/24 | RN-10/11 | `GET /api/enrollments?status=Pendiente; POST /api/enrollments/{id}/decision` | EnrollmentResolutionServiceTests; EnrollmentResolutionConcurrencyIntegrationTests | Enrollments/03,06; Exposición/09–10 | 200 resuelve; 409 si dejó de estar pendiente. Cupo y tratamiento de pendientes siguen en DOC-004. |
| CU-08 · Progreso Alumno/Profesor/Admin | RF-27/28/32 | RN-11/13/14 | `GET /api/enrollments/{id}/progress; GET /api/courses/{id}/students/progress` | ProgressServiceTests; ProgressIntegrationTests; demo.test.mjs | Enrollments/08,10; Exposición/12,14,18–19 | 200 calcula sin escribir; 403 por alcance y 409 por inscripción no aprobada. La vista global Admin está en la API. |
| CU-09 · Usuarios por Administrador | RF-06/32 | RN-01/02 | `GET /api/users; GET /api/users/{id}; POST /api/users/administrators` | AccountValidationIntegrationTests; OpenApiContractTests; RegistrationConcurrencyRegressionTests | Users/01–03 | 201 en alta, 400 en validación y 409 por correo duplicado. El alta interna está en la API; el prototipo consulta y simula el estado. |
| CU-10 · Reportes por Administrador | RF-33 a RF-35 | RN-16 | `GET /api/reports/summary` | ReportServiceTests; ReportsIntegrationTests; TraceabilityContractIntegrationTests | Reports/01–04 | 200 con conteos o cero; 400 por límites o fechas inválidos. Fechas civiles con zona optativa y prueba de cambio horario. |
| CU-11 · Catálogo de los tres roles | RF-19 | RN-08/12 | `GET /api/catalog/courses; GET /api/catalog/courses/{id}` | ApiIntegrationTests; TraceabilityContractIntegrationTests; demo.test.mjs | Catalog/01–02 | 200 arreglo o detalle, 404 si ya no está disponible y 400 por paginación inválida. Solo Alumno solicita inscripción. |
| CU-12 · Contenido para Alumno titular | RF-24/26/30 | RN-11/12 | `GET /api/enrollments/mine; GET /api/enrollments/{id}/course-content` | EnrollmentCreationIntegrationTests; ProgressIntegrationTests; demo.test.mjs | Enrollments/02,04–05; Exposición/11 | 200 consulta o acceso a publicación, 403 ajena y 409 si no está aprobada. Cerrar el catálogo no revoca acceso. |
| CU-13 · Cancelación Alumno titular/Admin | RF-25 | RN-11 | `POST /api/enrollments/{id}/cancel` | EnrollmentAndProgressServiceTests; EnrollmentConcurrencyRegressionTests; demo.test.mjs | Enrollments/07 | 200 cancela y conserva historial; 403 por rol/titularidad y 409 si no estaba aprobada. |
| CU-14 · Pausa y reanudación Profesor propietario/Admin | RF-17 | RN-08/12 | `POST /api/courses/{id}/pause y /resume` | EnrollmentCreationIntegrationTests; OpenApiContractTests; demo.test.mjs | Courses/06–07 | 200 en transición válida. 403 por alcance o 409 por estado. Pausar conserva la publicación y cierra nuevas solicitudes. |
| CU-15 · Finalización Administrador | RF-18 | RN-12/13 | `POST /api/courses/{id}/finish` | EnrollmentCreationIntegrationTests; OpenApiContractTests; demo.test.mjs | Courses/08 | 200 finaliza una publicación activa o pausada. No revoca inscripciones aprobadas ni cambia el progreso. 403/409 ante rechazo. |
| CU-16 · Módulos del Alumno titular | RF-24/27 | RN-11/13/15 | `PUT /api/enrollments/{id}/modules/{moduleId}/completion` | ModuleCompletionServiceTests; ModuleCompletionIntegrationTests; EnrollmentConcurrencyRegressionTests | Enrollments/09; Exposición/13,15–17 | 200 registra una vez; 403 por inscripción ajena y 409 por estado o módulo incorrecto. El prototipo representa avance local. |

## Archivos modificados

102 archivos entre ambas etapas en el árbol de trabajo.

- `.github/workflows/verification.yml`
- `1-Entrega Personal/1 - Informe/Lumen_Informe_General.docx`
- `1-Entrega Personal/1 - Informe/Lumen_Informe_General.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/Lumen_Actores_CasosDeUso.docx`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/Lumen_Actores_CasosDeUso.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-01-registrarse.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-02-iniciar-sesion.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-03-solicitar-creacion-de-curso.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-04-auditar-solicitud-de-curso.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-05-actualizar-curso-creado.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-06-solicitar-inscripcion-a-un-curso.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-07-gestionar-solicitudes-de-inscripcion.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-08-consultar-progreso.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-09-gestionar-usuarios.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-10-consultar-reportes.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-11-consultar-cursos-publicados.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-12-consultar-inscripciones-y-acceder-a-cursos.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-13-cancelar-inscripcion.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-14-pausar-o-reanudar-curso.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-15-finalizar-curso.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-16-realizar-modulos-o-actividades-del-curso.md`
- `3-Interfaz/README.md`
- `3-Interfaz/package-lock.json`
- `3-Interfaz/package.json`
- `3-Interfaz/src/App.tsx`
- `3-Interfaz/src/context/AppContext.tsx`
- `3-Interfaz/src/context/appReducer.ts`
- `3-Interfaz/src/data/seed.ts`
- `3-Interfaz/src/pages/admin/Reports.tsx`
- `3-Interfaz/src/pages/admin/UsersAndCourses.tsx`
- `3-Interfaz/src/pages/public/Login.tsx`
- `3-Interfaz/src/pages/public/Register.tsx`
- `3-Interfaz/src/pages/student/CourseAndRequests.tsx`
- `3-Interfaz/src/pages/student/Dashboard.tsx`
- `3-Interfaz/src/pages/student/Learning.tsx`
- `3-Interfaz/src/pages/teacher/DashboardAndCourses.tsx`
- `3-Interfaz/src/pages/teacher/Students.tsx`
- `3-Interfaz/src/services/courseService.ts`
- `3-Interfaz/src/services/demoCredentials.ts`
- `3-Interfaz/src/types/index.ts`
- `3-Interfaz/src/utils/format.ts`
- `3-Interfaz/tests/demo.test.mjs`
- `3-Interfaz/tsconfig.app.json`
- `4-Codigo/API/Controllers/AuthController.cs`
- `4-Codigo/API/Controllers/CoursesController.cs`
- `4-Codigo/API/Controllers/EnrollmentsController.cs`
- `4-Codigo/API/Controllers/ReportsController.cs`
- `4-Codigo/API/Controllers/UsersController.cs`
- `4-Codigo/API/Errors/ApiExceptionMiddleware.cs`
- `4-Codigo/API/Http/CourseEditTag.cs`
- `4-Codigo/API/Lumen.API.csproj`
- `4-Codigo/API/OpenApi/EndpointContractOperationTransformer.cs`
- `4-Codigo/API/OpenApi/EnumContractSchemaTransformer.cs`
- `4-Codigo/API/Program.cs`
- `4-Codigo/API/Security/JwtTokenService.cs`
- `4-Codigo/API/Security/LoginRateLimit.cs`
- `4-Codigo/BusinessLogic/Interfaces/ICourseService.cs`
- `4-Codigo/BusinessLogic/Interfaces/IReportService.cs`
- `4-Codigo/BusinessLogic/Lumen.BusinessLogic.csproj`
- `4-Codigo/BusinessLogic/Services/AuthService.cs`
- `4-Codigo/BusinessLogic/Services/CourseService.cs`
- `4-Codigo/BusinessLogic/Services/ReportService.cs`
- `4-Codigo/BusinessLogic/Services/ServiceGuard.cs`
- `4-Codigo/BusinessLogic/Services/UserService.cs`
- `4-Codigo/DataAccess/Configurations/UserConfiguration.cs`
- `4-Codigo/DataAccess/DbInitializer.cs`
- `4-Codigo/DataAccess/DemoAccounts.cs`
- `4-Codigo/DataAccess/Lumen.DataAccess.csproj`
- `4-Codigo/DataAccess/Repositories/CourseRepository.cs`
- `4-Codigo/DataAccess/Repositories/EnrollmentRepository.cs`
- `4-Codigo/DataAccess/Repositories/ICourseRepository.cs`
- `4-Codigo/MODELO_DE_DATOS.md`
- `4-Codigo/Migrations/DatabaseMigrations/20261105230000_EnforceCourseRevisionOwnership.cs`
- `4-Codigo/Migrations/Lumen.Migrations.csproj`
- `4-Codigo/README.md`
- `4-Codigo/Shared/DTOs/CourseDTOs.cs`
- `4-Codigo/Shared/DTOs/CoursePage.cs`
- `4-Codigo/Shared/DTOs/EnrollmentDTOs.cs`
- `4-Codigo/Shared/DTOs/ReportDTOs.cs`
- `4-Codigo/Shared/DTOs/UserDTOs.cs`
- `4-Codigo/Shared/Exceptions/CourseVersionMismatchException.cs`
- `4-Codigo/Shared/Validation/UserFieldLimits.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/AccountValidationIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/ConcurrencyPersistenceFailureIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/CourseConcurrencyIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/CourseCreationIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/DemoSeedingIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/LoggingIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/Lumen.API.IntegrationTests.csproj`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/OpenApiContractTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/TraceabilityContractIntegrationTests.cs`
- `4-Codigo/bruno/Courses/02-detail.bru`
- `4-Codigo/bruno/Courses/04-update.bru`
- `4-Codigo/bruno/Courses/12-stale-edit.bru`
- `4-Codigo/bruno/README.md`
- `4-Codigo/bruno/Reports/01-summary.bru`
- `4-Codigo/bruno/Reports/02-empty-period.bru`
- `4-Codigo/bruno/Reports/03-inverted-period.bru`
- `4-Codigo/bruno/Reports/04-civil-zone.bru`
- `4-Codigo/global.json`
- `AUDITORIA_Y_PLAN_DE_CORRECCIONES.md`
- `README.md`
