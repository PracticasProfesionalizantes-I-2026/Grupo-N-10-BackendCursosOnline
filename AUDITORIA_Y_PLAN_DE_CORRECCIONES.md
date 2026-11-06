# Auditoría y plan de correcciones de LUMEN

Auditoría original: 05/11/2026, `main` en `050a17e35c5fe08bf56c70fdea95f521726cf104`. Línea base del informe: 33 hallazgos (7 altos, 21 medios y 5 bajos), 350 pruebas aprobadas (101 unitarias y 249 de integración), build sin errores ni advertencias. El alta de inscripción, las fechas obligatorias de reportes y Bearer/OpenAPI ya estaban corregidos en esa revisión. La auditoría original distingue API, prototipo local y decisiones de negocio; este registro conserva los mismos IDs.

Las cinco rondas se realizaron sin modificar bases de producción. Esta quinta ronda continúa en `fix/auditoria-lumen` sobre `4e0ded1`, sin commit, push ni merge. Cada fila contiene el último resultado comprobado. Un bloqueo afecta solo a la corrección que depende de esa decisión.

| ID | Categoría | Severidad | Evidencia y archivos de origen | Corrección prevista o decisión | Dependencias, pruebas y documentación | Estado | Validación |
|---|---|---|---|---|---|---|---|
| BUG-001 | Bug | Alto | `CourseReviewDecisionDTO` y `CourseService.ResolveReviewAsync`: el valor cero aprobaba ante `{}`. | Exigir decisión explícita sin cambiar los valores válidos. | API-003, TEST-001; HTTP, no mutación, CU-04 y OpenAPI. | COMPLETADO | HTTP 400 ante decisión ausente, nula o inválida y ante pedido de cambios sin observación. La regresión verifica que no se resuelve la auditoría ni cambia la publicación. La aprobación explícita funciona. |
| BUG-002 | Bug | Alto | `CourseService.UpdateAsync`: una revisión rechazada bloquea edición aunque siga publicada la anterior. | Acordar recuperación desde la publicación o acción de reapertura sin borrar historial. Se recomienda nuevo borrador separado si se aprueba. | DB-005; rechazo inicial/posterior, CU-04/05 y README. | COMPLETADO | Profesor propietario o Administrador crea un borrador con versión nueva mediante recover-draft. Se conserva el contenido rechazado, su decisión y la publicación anterior si existía. La API protege la operación con If-Match y el prototipo ofrece la acción local. |
| BUG-003 | Bug | Alto | `Learning.tsx`, `courseService.ts` y `AppContext.tsx`: alumno lee módulos de trabajo. | Centralizar lectura de versión publicada y cálculo sobre sus módulos. | BUG-004/006; pruebas del prototipo, CU-12/16. | COMPLETADO | Las pantallas del alumno, incluido Mis solicitudes, y el avance para el profesor usan la publicación. Las pruebas verifican que editar un borrador no filtra módulos ni cambia el progreso publicado. |
| BUG-004 | Bug | Medio | `AppContext.tsx`: avance por alumno y curso se reutiliza al reinscribirse. | Asociar progreso a cada inscripción; preservar historial y tratar datos locales anteriores. | BUG-003; regresión cancelar/reinscribir, CU-06/08/13. | COMPLETADO | Pruebas de reinscripción y datos previos. El avance se identifica por inscripción. Los registros antiguos inequívocos se vinculan; los ambiguos se conservan separados del avance activo. |
| BUG-005 | Bug | Bajo | `Reports.tsx`: límite inclusivo a las 23:59:59.000. | Límite exclusivo del día siguiente, preservando el calendario local actual del prototipo. | VAL-003 no condiciona este borde; pruebas de medianoche, CU-10/RN-16. | COMPLETADO | Prueba de último milisegundo del día y exclusión del primer instante del día siguiente. Se mantiene el calendario local del prototipo. |
| BUG-006 | Bug | Medio | `courseService.ts`, `format.ts`, `AppContext.tsx`: versión previa equivale erróneamente a disponibilidad. | Compartir criterio: publicación vigente y curso operativo; mantener contenido aprobado durante revisión. | BUG-003; pausa/reanudación y revisión, CU-06/11/14/15. | COMPLETADO | Pruebas de catálogo con revisión y con pausa/finalización. Editar o aprobar un curso pausado conserva la pausa y la publicación hasta que corresponda reemplazarla; se requiere reanudar para admitir nuevas inscripciones. |
| BUG-007 | Bug | Medio | `CourseAndRequests.tsx`, `AppContext.tsx`: se cancela una pendiente. | Alinear UI y transición con CU-13: solo aprobadas. | BUG-004; pruebas de ambos roles y transición, CU-07/13. | COMPLETADO | El reducer acepta resolución de una pendiente y cancelación de una aprobada. La UI y el proveedor aplican acción y titularidad. La regresión rechaza cancelar solicitudes pendientes. |
| VAL-001 | Validación | Medio | `UserCreateDTO`: omitir rol crea Alumno. | Exigir presencia sin alterar roles admitidos. | VAL-002, TEST-001; registro HTTP, CU-01 y OpenAPI. | COMPLETADO | Registro sin rol o con rol no permitido responde 400 sin crear cuenta. OpenAPI requiere el rol y el alta válida conserva los roles admitidos. |
| VAL-002 | Validación | Alto | `ServiceGuard`, `AuthService`, `UserService`, `UserConfiguration`: email inválido y longitudes fuera del modelo aceptados. | Aplicar la política argentina aprobada a altas nuevas públicas e internas; conservar el login histórico. | VAL-001, TEST-001; CU-01/02/09, OpenAPI y prototipo. | COMPLETADO | DNI de 7 u 8 dígitos, teléfono nacional o +54 de 10 a 15 dígitos, código postal de cuatro cifras o CPA, y contraseña íntegra de 8 a 128 caracteres. Se mantiene la normalización del correo y se guarda CPA en mayúsculas. Regresiones válidas, límites y rechazo sin escritura; las cuentas anteriores conservan acceso. |
| VAL-003 | Validación | Medio | `ReportService`: combina instantes y final del día. | Elegir fechas civiles con zona explícita (recomendado para la UI actual) o intervalo simétrico de instantes. | BUG-005, TEST-002; CU-10, RN-16, OpenAPI, Bruno. | COMPLETADO | Con timeZone la API interpreta fechas civiles IANA, verifica medianoche y devuelve el rango con zona. La regresión de un día de 23 horas verifica el último tick y el límite exclusivo. El contrato sin zona sigue disponible y el prototipo usa la zona del navegador. |
| VAL-004 | Validación | Medio | `CourseService.NormalizeList`: sin límites de tamaño de listas. | Aplicar topes aprobados y permitir reducción gradual de excesos históricos. | DB-003; CU-03/05, OpenAPI, frontend y Bruno. | COMPLETADO | Máximos de 100 módulos, 30 objetivos, 30 requisitos, 50 recursos por módulo y textos de 500/500/2.000 caracteres, además de los límites escalares existentes. Se verificaron bordes, envío y edición de históricos; una entrada inválida no se guarda. |
| SEC-001 | Seguridad | Alto | `DbInitializer`: credenciales demo siguen activas en Production con una base reutilizada. | Impedir uso de cuentas reservadas demo en entornos no demo, sin borrar filas; documentar separación y aprovisionamiento. | DB-001; base sembrada reutilizada y producción limpia, README/Bruno. | COMPLETADO | Login y tokens emitidos para cuentas demo se rechazan en Production aun reutilizando una base sembrada. La prueba conserva las cuentas y acredita el acceso con una cuenta independiente. |
| SEC-002 | Seguridad | Medio | `AppContext.tsx`, `Register.tsx`: contraseñas ingresadas en localStorage. | Eliminar persistencia en claro preservando los accesos de demostración. | DOC-001; verificación de almacenamiento, README frontend y CU-01/02. | COMPLETADO | PBKDF2-SHA256 con sal por cuenta. Regresiones para login, contraseña errónea, migración de contraseña en claro y acceso demo. El prototipo usa Web Crypto en localhost o HTTPS. |
| SEC-003 | Seguridad | Medio | `AuthService`, `Program.cs`: estado de cuenta antes de validar clave y sin limitador. | Evitar revelar el estado ante credencial inválida; no inventar umbrales/política de acceso. | VAL-002/API-002; login correcto/incorrecto e inactivo, CU-02. | COMPLETADO | Login verifica la contraseña antes de revelar inactividad. La API limita a 20 solicitudes por IP de conexión y por 60 segundos, devuelve 429 con Retry-After e ignora encabezados de IP aportados por el cliente. El despliegue distribuido requiere una política en el borde. |
| SEC-004 | Seguridad | Alto | `global.json`, runtime local 10.0.11 y aviso de Microsoft. | Actualizar en el nivel de servicio compatible y verificar entorno y referencias aplicables. | Build, suite, requisitos README/Bruno. | COMPLETADO | SDK mínimo 10.0.403, runtime 10.0.14 y referencias Microsoft aplicables actualizadas. Build sin advertencias, 367 pruebas y 0 paquetes vulnerables en el escaneo NuGet con transitivas. |
| SEC-005 | Seguridad | Bajo | `package-lock.json`: Vite 6.4.3 en rango afectado. | Actualizar dentro de Vite 6 a versión con parches compatibles y verificar lockfile. | TEST-003; build, lint y README frontend. | COMPLETADO | Vite 6.4.5 resuelve el hallazgo auditado y su aviso posterior. Build, lint y tests aprobados. El escaneo de producción devolvió cero avisos. El hallazgo nuevo de minimatch queda registrado por separado. |
| DB-001 | Base de datos | Medio | `DbInitializer`: siembra parcial y salida al encontrar usuarios. | Hacer la siembra demo atómica, sin modificar datos existentes. | SEC-001; rollback e idempotencia, README/Bruno. | COMPLETADO | La siembra usa una transacción. Una prueba induce un error intermedio, comprueba el rollback completo y vuelve a ejecutar la siembra con éxito. |
| DB-002 | Base de datos | Medio | Configuraciones EF de curso y finalización: restricciones entre versiones no completas. | Diseñar invariantes compatibles con identidad de módulo e historial; evitar cambio de esquema sin diagnóstico de datos y política de versiones. | DB-005/BUG-002; migración segura y DER. | COMPLETADO | Módulos idénticos conservan su identidad incluso al reordenar. Los nuevos y modificados reciben otro ID; los retirados dejan de computar y sus completados permanecen guardados. Lecturas y escrituras protegen contra punteros ajenos, incluso históricos. |
| DB-003 | Base de datos | Medio | Listados en repositorios cargan grafos completos. | Reducir carga de lecturas sin cambiar contratos; paginación queda para decisión de API. | VAL-004; DTOs/orden sin cambios, tests de integración. | COMPLETADO | Paginación SQL optativa de todos los listados HTTP. Mantiene los arreglos históricos, desempata por ID y aplica filtros antes de recortar. Se verificaron el límite de 100, valores inválidos, páginas vacías y compatibilidad. |
| DB-004 | Base de datos | Alto | `CourseUpdateDTO` no recibe versión esperada del editor. | Acordar precondición requerida u optativa, ETag/versionado y compatibilidad. Se recomienda ETag con transición documentada. | BUG-002/DB-005; edición tardía y concurrente, CU-05/OpenAPI. | COMPLETADO | La API exige If-Match específico en PUT y recuperación. HTTP 428 ante ausencia o comodín, 400 ante sintaxis inválida y 412 ante etiqueta no coincidente o cambio concurrente. Las carreras preservan la primera escritura; Bruno cubre 428, 412 y el guardado válido. |
| DB-005 | Base de datos | Medio | `CourseReview` apunta a revisión editable después de solicitar cambios. | Acordar si se conserva solo decisión o instantánea inmutable por envío. | BUG-002/DB-002; trazabilidad CU-04/05/RNF-13. | COMPLETADO | Cada nuevo envío guarda una copia independiente e inmutable. El detalle autorizado distingue fecha, decisión, observación, contenido y publicación al enviar. Una auditoría previa conserva sus metadatos con submission nulo. La migración es aditiva. |
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
| DOC-003 | Documentación | Medio | Scalar presente, Swagger UI ausente; DER y niveles no encontrados. | Mantener OpenAPI como contrato, Scalar como explorador y Bruno como prueba HTTP; registrar el límite académico. | Modelo EF y consigna; README/Informe General. | BLOQUEADO | Decisión técnica implementada. OpenAPI y Scalar se verificaron y el DER refleja la sexta migración. Falta confirmación oficial de equivalencia académica de Scalar y rúbrica de niveles de complejidad; no se afirma ese cumplimiento. |
| DOC-004 | Documentación | Medio | Cupo no limita aprobación y RN-12 solo bloquea nuevas solicitudes. | Aplicar cupo a aprobadas activas; resolver pendientes durante pausa y rechazarlas al finalizar. | BUG-002/006; concurrencia de última plaza, CU-06/07/12/13/14/15/16. | COMPLETADO | Una aprobación atómica en SQLite ocupa la plaza disponible; una decisión concurrente perdedora recibe 409. La cancelación libera cupo, no puede reducirse bajo ocupación, la pausa permite resolver pendientes y finalizar las rechaza con motivo en una transacción. Se conservan acceso, progreso e historial aprobados. |

## Cierre de la quinta ronda del 06/11/2026

Las decisiones funcionales aprobadas cerraron VAL-002, VAL-004 y DOC-004. El balance actual de los 33 IDs originales es **32 COMPLETADOS y 1 BLOQUEADO** (DOC-003, solo por validación académica). No se abrió ningún defecto nuevo. Los recuentos anteriores de cada ronda permanecen en el historial y no sustituyen estos resultados.

| ID | Decisión e implementación | Verificación y documentación | Estado y límite restante |
|---|---|---|---|
| VAL-002 | Política argentina en `ServiceGuard`, altas pública e interna, CPA normalizado y formulario de registro; login de cuentas antiguas intacto. | `AccountValidationIntegrationTests`, `AuthServiceTests`, OpenAPI, Bruno `01-Politicas`; CU-01/02/09 individual y consolidado, Informe General y README. | COMPLETADO. El DNI solo se valida por formato, sin consulta oficial. |
| VAL-004 | `CourseService` valida cantidades y longitudes, distingue borrador de envío y permite reducir excesos históricos; `CourseForm` muestra topes. | `ApprovedCourseLimitsIntegrationTests`, OpenAPI, Bruno `01-Politicas`; CU-03/04/05, consolidado, Informe General y README. | COMPLETADO. Los cursos históricos excedidos son consultables y deben reducir excesos al editarlos. |
| DOC-004 | Cupo efectivo con decisión atómica, control de reducción mediante servicio y triggers SQLite, finalización y rechazos transaccionales con motivo persistido. | `ApprovedCapacityIntegrationTests`, `CourseClosureEnrollmentIntegrationTests`, `EnrollmentResolutionConcurrencyIntegrationTests`, Bruno `01-Politicas` y prototipo; CU-06/07/12/13/14/15/16, consolidado y modelo de datos. | COMPLETADO. Los datos históricos con ocupación anterior superior al cupo no se eliminan; nuevas aprobaciones quedan bloqueadas hasta recuperar capacidad. |
| DOC-003 | OpenAPI documenta, Scalar explora y Bruno ejecuta HTTP; DER y migraciones actualizados. | `OpenApiContractTests`, ejecución Bruno, README, modelo de datos e Informe General. | BLOQUEADO únicamente en aceptación académica de Scalar y niveles de complejidad; requiere criterio oficial de la cátedra. |

La verificación ejecutada comprende build .NET, **102 pruebas unitarias y 314 de integración**, **12 pruebas del prototipo** más lint y build, y **91 solicitudes Bruno** aprobadas entre los 27 escenarios nuevos y los recorridos existentes de 19, 37 y 8 solicitudes. OpenAPI y Scalar se comprobaron en la suite HTTP. La migración aditiva `20261106230000_EnforceCourseCapacity` se aplicó sobre SQLite temporal, incluida la convivencia con las cinco anteriores. NuGet informó cero paquetes vulnerables en los siete proyectos. npm informó cero avisos en dependencias de producción y siete moderados en herramientas de desarrollo ligadas a `minimatch`, ya documentados en SEC-005. El consolidado Word y Markdown conserva 16 casos y 217 pasos concordantes; el Informe General conserva los 65 enunciados originales. Ambos Word se abrieron y renderizaron (23 y 10 páginas).

La revisión para `main` está preparada en el árbol de trabajo y requiere el control habitual del equipo sobre el diff y la ejecución de CI remota. No se hizo commit, push ni merge, y la migración no se aplicó a una base productiva.

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

## Cierre de la tercera ronda del 06/11/2026

| Estado | Hallazgos originales |
|---|---:|
| COMPLETADO | 29 |
| BLOQUEADO | 4 |

La rama inicial fue `fix/auditoria-lumen` en `3e1e875`, con árbol limpio. La línea base aprobó 374 pruebas .NET, ocho del frontend y lint.

1. **DB-004.** Se localizaron consumidores en Bruno y pruebas de integración. El frontend local no usa el endpoint. Las 91 pruebas dirigidas del lote aprobaron el contrato obligatorio. Los clientes externos que aún guarden sin etiqueta recibirán 428. La transición consiste en anunciar el contrato, consultar el curso, usar el ETag y volver a comparar los cambios al recibir 412.
2. **BUG-002 y DB-005.** Se agregó recuperación separada y evidencia de cada envío nuevo. Se verificaron rechazo inicial, rechazo con publicación previa, reintento, reenvío tras cambios y ausencia de copia histórica. La migración preserva el historial y los disparadores protegen la evidencia.
3. **DB-002.** Se separó identidad del orden. En una primera prueba, una fixture sustituía todos los contenidos y esperaba conservar todo el progreso. Se probaron por separado los completados de módulos idénticos y los reemplazos, y se protegieron las lecturas y escrituras frente a punteros ajenos.
4. **DB-003, VAL-004 y VAL-002.** Se extendió la paginación compatible a usuarios, auditorías, inscripciones y progreso. Se añadieron validaciones de caracteres de control sin adoptar formatos nacionales. Los límites de listas siguen sujetos a datos representativos.
5. **DOC-004 y DOC-003.** Se comprobaron pausa, finalización, acceso aprobado y resolución de solicitudes anteriores. OpenAPI y Scalar se verificaron en la API y se conservó el DER. Las decisiones funcionales y académicas permanecen separadas.
6. **Regresión final.** Build .NET con 0 errores y 0 advertencias. Aprobadas 386 pruebas (101 unitarias y 285 de integración), incluidas las carreras y cuatro migraciones en SQLite temporal. Frontend con 10 pruebas aprobadas, lint y build correctos. Bruno CLI 4.3.0 aprobó 58 requests distintos, 64 ejecuciones y 139 tests en tres instancias temporales. Se verificaron 31 operaciones OpenAPI y la interfaz Scalar.
7. **Trazabilidad.** Se cotejaron CU-01 a CU-16, sus actores y reglas con el código, los recorridos, el Markdown y el Word. La lista por caso distingue las operaciones disponibles solo en la API. Los 65 enunciados RF, RNF y RN se conservaron. Se revisaron los dos Word renderizados. El Informe General mantuvo nueve páginas y el consolidado pasó de 22 a 23.

## Hallazgos adicionales de la tercera ronda

- **R3-N01 · Resuelto.** Una base previa a los disparadores podía conservar punteros hacia revisiones ajenas. Ahora esas filas se preservan, pero no se presentan en gestión o catálogo ni se usan para entregar contenido, aceptar nuevas inscripciones o completar módulos. La regresión reproduce el estado antiguo exclusivamente en SQLite temporal. Este hallazgo no altera el total de 33.
- **Límite histórico.** Envíos previos a la cuarta migración pueden carecer de una copia verificable. Los IDs antiguos de módulos ya compartidos se conservan. La migración no atribuye contenido ni recalcula historias con evidencia insuficiente.
- **Alcance del prototipo.** Es local. Las precondiciones HTTP, los listados paginados y la evidencia persistida en SQLite pertenecen a la API. El prototipo recupera borradores locales y muestra copias locales. Mantiene la restricción preexistente de edición de cursos finalizados.
- **Seguridad.** NuGet no informó avisos en dependencias directas ni transitivas. npm devolvió cero avisos de producción y siete entradas moderadas de desarrollo. Las precondiciones fallidas no escriben y las respuestas 428 excluyen caché.

## Cierre de la cuarta ronda del 06/11/2026

Se repitió la validación sobre `fix/auditoria-lumen` en `3e1e875`, con `main` y el ancestro común en `050a17e`. El árbol inicial incluía 77 rutas de las rondas anteriores. Se preservó ese trabajo y se revisó el diff acumulado contra `main`, incluidas las altas todavía sin seguimiento. La línea base de esta ronda pasó 386 pruebas .NET (101 unitarias y 285 de integración) y 10 pruebas del prototipo. El saldo de la auditoría original se mantiene en **29 COMPLETADOS y 4 BLOQUEADOS**. Los cuatro hallazgos nuevos, corregidos en esta ronda, se cuentan aparte.

### Evidencia independiente por hallazgo original

| ID | Estado | Comprobación de la cuarta ronda |
|---|---|---|
| BUG-001 | COMPLETADO | `ReviewDecision_RejectsInvalidInputWithoutPublishing` ejercitó el DTO y la validación de la decisión. El valor ausente o inválido da 400 y conserva estado y publicación. OpenAPI exige la decisión explícita. |
| BUG-002 | COMPLETADO | `Recovery_CopiesRejectedWorkWithoutChangingTheReviewOrPublication`, Bruno Courses/15–16 y la prueba local de recuperación comprobaron que la revisión rechazada se copia a un borrador nuevo. Se preservan la decisión y la publicación previa, incluso si no había publicación. |
| BUG-003 | COMPLETADO | La prueba del prototipo comprobó el aislamiento de catálogo y contenido frente al borrador. En la API, `CourseUpdateIntegrationTests` verificó la publicación anterior hasta aprobar la nueva y el rechazo de decisiones tardías. |
| BUG-004 | COMPLETADO | Las pruebas del prototipo reprodujeron cancelación y nueva aprobación con progreso inicial. Las pruebas de creación/persistencia de inscripciones de la API comprobaron que la solicitud usa la identidad JWT y crea un registro independiente sin reutilizar la inscripción anterior. |
| BUG-005 | COMPLETADO | La prueba de fechas locales de `demo.test.mjs` comprobó el último milisegundo de Hasta y excluyó la medianoche siguiente. El reporte sigue usando el día civil seleccionado. |
| BUG-006 | COMPLETADO | Las pruebas locales comprobaron que editar en pausa no reabre el catálogo. `CourseClosureEnrollmentIntegrationTests` comprobó que pausar o finalizar impide nuevas solicitudes y conserva el acceso aprobado y la resolución de las pendientes anteriores. |
| BUG-007 | COMPLETADO | La prueba local de cancelación restringe la transición a APROBADA. Bruno Enrollments/07 y `EnrollmentConcurrencyRegressionTests` verificaron la cancelación autorizada y la conservación del estado y del historial frente a operaciones incompatibles. |
| VAL-001 | COMPLETADO | `PublicRegistrationRejectsInvalidInputWithoutCreatingUser` verificó 400 sin alta para rol omitido o inadmisible y datos inválidos. CU-01, Auth/04 y el esquema OpenAPI conservan el alta pública de Alumno o Profesor. |
| VAL-002 | BLOQUEADO | `LoginPreservesAccessForAnAddressAcceptedBeforeRegistrationValidation` verificó compatibilidad histórica. Las pruebas de alta rechazan caracteres de control y longitudes inválidas y admiten datos internacionales. La política de identidad y contraseñas sigue pendiente. |
| VAL-003 | COMPLETADO | `Reports_UseTheSelectedCivilDayIncludingItsLastTick` cubrió un día civil de 23 h. `ReportsIntegrationTests` y Bruno Reports/01–04 verificaron filtros válidos, vacíos y entradas inválidas. |
| VAL-004 | BLOQUEADO | Un curso de 180 módulos con 226.015 bytes de JSON completó alta, envío y edición. Se corrigieron el fallo de transporte y la multiplicación de filas. Los topes funcionales siguen pendientes. |
| SEC-001 | COMPLETADO | `RestrictedEnvironment_RejectsDemoLoginAndPreviouslyIssuedTokenWithoutChangingUsers` verificó que Production y Staging migran sin sembrar cuentas demo, rechazan sus credenciales y tokens anteriores y conservan los usuarios existentes. |
| SEC-002 | COMPLETADO | La prueba `demo credential migration preserves access without serializing the password` verificó acceso con el derivado salado en `demoCredentials.ts` y migración local de credenciales anteriores. |
| SEC-003 | COMPLETADO | `LoginLimit_UsesConnectionAddressAndReturnsRetryAfter` verificó 429 por IP de conexión y `Retry-After`. `LoginRevealsInactiveStateOnlyAfterCorrectPassword` verificó 403 solo después de la clave válida. Se reforzó el camino de credenciales inválidas con R4-N04. |
| SEC-004 | COMPLETADO | Build y suite final sobre SDK 10.0.403 y runtime 10.0.14. NuGet informó cero avisos directos y transitivos en los siete proyectos. |
| SEC-005 | COMPLETADO | Vite 6.4.5 compiló el frontend y npm informó cero avisos de producción. Las siete entradas de desarrollo corresponden al aviso distinto de minimatch. |
| DB-001 | COMPLETADO | `FailedDemoSeedRollsBackAndCanBeRetried` reprodujo una semilla parcial fallida, verificó su reversión y reintentó con éxito. La suite también comprobó idempotencia de la semilla y conservación de cuentas existentes. |
| DB-002 | COMPLETADO | `RevisionOwnershipMigration_RejectsForeignPointersWithoutChangingExistingRows` comprobó integridad. `LegacyForeignPointers_DoNotExposeAnotherCourseOrEraseTheStoredRows` verificó aislamiento de datos anteriores y `Republication_CountsUnchangedModulesAndKeepsOldCompletionsAsHistory` cubrió identidad y progreso. |
| DB-003 | COMPLETADO | `Lists_PageStableFilteredResults_AndKeepTheExistingArrayWhenOmitted` cubrió formato histórico, filtros, orden estable, límites inválidos y páginas vacías. R4-N02 comprobó la consulta de detalle y edición por separado. |
| DB-004 | COMPLETADO | `CourseOperations_WithOverlappingReads_RejectStaleWrite` forzó lecturas concurrentes. Bruno Courses/12–13 y pruebas de contrato verificaron 412 con etiqueta obsoleta y 428 sin etiqueta específica, incluido comodín. Solo persiste la escritura ganadora. |
| DB-005 | COMPLETADO | `History_StoresEachSubmissionAndProtectsItFromChangesAndUnrelatedReaders` verificó instantáneas por envío, lectura autorizada e inmutabilidad, incluidas sustitución y conservación de datos al retroceder de la quinta migración a la cuarta y volver a aplicar la quinta. Bruno Courses/14 comprobó la lectura. |
| API-001 | COMPLETADO | `RegistrationAcceptsBoundaryAndDoesNotAdvertiseUnusableLocation` comprobó 201 sin un `Location` inutilizable. `Create_PersistsOnlyEnrollmentUsesJwtIdentityAndReturnsReadableLocation` comprobó el 201 de inscripciones y su ubicación accesible. |
| API-002 | COMPLETADO | `ConcurrencyPersistenceFailureIntegrationTests` ejercitó fallas de persistencia y el 500 genérico sin exponer detalles. R4-N01 verificó que el exceso de tamaño sale como 413 en el servidor HTTP real. |
| API-003 | COMPLETADO | `CourseReviewRequest_RequiresDecisionButNotObservation` comprobó el contrato del DTO. La observación se exige al solicitar cambios y los valores de decisión inválidos devuelven 400 sin mutar. |
| SMELL-001 | COMPLETADO | Se revisó el tratamiento común de errores y el flujo de controladores. `Operation_HasItsActualResponseContract` y las regresiones HTTP confirmaron los códigos y contratos de negocio. |
| SMELL-002 | COMPLETADO | `ReportFilterDTO` y `EnrollmentUpdateDTO` carecen de referencias en el código actual. Build, los recorridos de reportes e inscripciones y el inventario OpenAPI conservaron los contratos vigentes. |
| TEST-001 | COMPLETADO | Las suites de alta pública, alta interna y decisión de curso comprobaron límites y campos requeridos, incluido que el rechazo de entradas inválidas no crea cuentas ni publica cursos. |
| TEST-002 | COMPLETADO | Bruno Reports/01–04 y `ReportsIntegrationTests` pasaron el resumen con datos, sin datos y los rechazos previstos, incluidos límites y fechas civiles. |
| TEST-003 | COMPLETADO | Se revisó `.github/workflows/verification.yml` y se ejecutó localmente la verificación de ambos proyectos. El resultado remoto del flujo de GitHub Actions está pendiente. |
| DOC-001 | COMPLETADO | Se cotejaron Informe General, consolidado y 16 casos individuales. Los 65 enunciados aprobados coinciden entre Markdown, Word y la línea base de Markdown de `main`. Los nueve ajustes quedaron en el paso correspondiente y se revisó el render de ambos Word. |
| DOC-002 | COMPLETADO | OpenAPI, Bruno y los casos acordaron 201 para crear un borrador seguido de 200 al enviarlo, 409 ante alta interna duplicada y 404 ante detalle de catálogo no disponible. |
| DOC-003 | BLOQUEADO | `OpenApiAndScalar_AreAvailable`, 31 operaciones comprobadas y cotejo del DER y las migraciones. Faltan la consigna sobre herramienta de visualización y la rúbrica de niveles de complejidad. |
| DOC-004 | BLOQUEADO | `Closure_KeepsExistingApprovedAccessAndPendingDecision` y la suite de altas verificaron admisión, pendientes y acceso aprobado con pausa y finalización. Una comprobación adicional en Kestrel y SQLite temporal configuró cupo 1. Dos alumnos distintos recibieron 201 al solicitar, 200 al aprobar y 200 al acceder al contenido. El cupo actual es informativo y su semántica sigue pendiente. |

### Hallazgos nuevos y correcciones

| ID | Evidencia antes y después | Cambio y regresión |
|---|---|---|
| R4-N01 · Resuelto | En Kestrel local, un cuerpo de 30.000.001 bytes devolvía 500. Ahora devuelve 413 con `application/problem+json`, sin detalle interno. | `ApiExceptionMiddleware` traduce el exceso de tamaño y OpenAPI declara el 413 en las ocho operaciones con cuerpo JSON. `OversizeBody_OnKestrelReturns413WithoutWritingOrDisclosingDetails` usa Kestrel real, límite de 1.024 bytes y cuerpo de 1.025 bytes. Verifica que la creación no escribe. El límite se conserva. |
| R4-N02 · Resuelto | Con 180 módulos, la consulta cruzaba las colecciones de módulos de dos revisiones. En las ejecuciones locales, el GET de gestión antes de crear el borrador pasó de 2.493 a 228 ms, el GET del borrador de 1.986 a 60 ms y el PUT de 2.452 a 374 ms. El pico del proceso pasó de 199,9 a 156,0 MiB. | `CourseRepository` carga la colección de trabajo una vez. Para editar, consulta por separado la revisión publicada exacta solo cuando difiere de la de trabajo. La regresión SQL `CourseManagement_ReadsPublishedAndWorkingModulesWithoutMultiplyingTheRows` verifica que ningún SELECT une dos veces la tabla de módulos y que se conservan los IDs. [Documentación de EF Core sobre consultas relacionadas](https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries). |
| R4-N03 · Resuelto | Con `recursive_triggers=OFF`, SQLite aceptaba `INSERT OR REPLACE` sobre una instantánea aun con disparadores de UPDATE y DELETE. Ahora se rechaza y el JSON original permanece. | La quinta migración incorpora un disparador BEFORE INSERT cuando ya existe la instantánea. La regresión ejecuta 5→4→5 sobre datos existentes y vuelve a probar la sustitución. [Documentación de SQLite sobre ON CONFLICT](https://www.sqlite.org/lang_conflict.html). |
| R4-N04 · Resuelto | Siete pares alternados de intentos 401 por ejecución dieron medianas de 150,6 ms con correo existente y 9,1 ms con uno inexistente. Tras el cambio, fueron 277,5 y 276,7 ms. | `AuthService` ejecuta una comprobación de contraseña de costo comparable cuando no encuentra el correo y devuelve 401. `LoginAsync_ForMissingAccountVerifiesAPasswordHashAndNeverIssuesAToken` fuerza un resultado positivo del verificador y comprueba que no se emite un token. |

Las mediciones se hicieron en loopback con SQLite temporal y datos de prueba. Para el curso grande se tomó una muestra por operación. La creación y el envío midieron 259 y 423 ms antes y 940 y 1.074 ms después. Esa muestra no permite atribuir los cambios de latencia al código ni extrapolar a producción. La regresión determinista de R4-N02 es la forma de las consultas y la conservación del contenido e identidad de los módulos. El costo de login puede variar por entorno y por parámetros de hashes históricos. La mitigación se revisa si cambia la configuración del verificador.

### Casos de uso y alcance verificado

La [matriz funcional](#auditoría-funcional-de-los-16-casos-de-uso) conserva por caso los RF y RN, el endpoint, las pruebas y Bruno. Se cotejó con los RNF pertinentes, actores, precondiciones, flujos principal y alternativos, transiciones, permisos, DTO, persistencia y alcance de la interfaz. Los pasos de interfaz se contrastaron con código y con las pruebas locales disponibles. Bruno ejecuta la API, y el prototipo guarda y consulta su propio estado local.

RNF-06 y RNF-07 se cotejaron en los 16 casos mediante trazabilidad y separación del prototipo local. La tabla identifica otros RNF directamente vinculados a cada flujo. Los RNF de estructura, extensibilidad y disponibilidad se revisaron de forma transversal mediante código, lint y build. El cotejo de interfaz no certifica todas las pantallas mediante navegación automatizada.

| Caso | RNF vinculados | Comprobación final y diferencia relevante |
|---|---|---|
| CU-01 | 01/02/09/14 | Registro público con `UserCreateDTO` para Alumno o Profesor, 201 sin ubicación artificial, 400/409/413 sin alta. El registro local persiste un derivado de la contraseña. |
| CU-02 | 01/02/10 | Login de los tres roles con token de la API. 401 para credenciales inválidas, 403 de cuenta inactiva con contraseña válida, 429 con demora y 413 si excede el transporte. Sesión del prototipo local. |
| CU-03 | 01/10/12/14 | Profesor/Admin en API, creación 201 con revisión de trabajo y envío 200 que genera auditoría. Módulos válidos y total calculado. 413 al crear sin dejar borrador. El prototipo crea como Profesor. |
| CU-04 | 01/12/13 | Solo Admin decide. DTO de decisión explícita, observación obligatoria para cambios, 400/409/413 sin mutación. Lectura de copia por envío con alcance de rol y titularidad. La decisión no reemplaza una publicación anterior salvo al aprobar. |
| CU-05 | 01/12/13/14 | Profesor propietario/Admin. Guardado y recuperación con `If-Match` específico, 412 ante etiqueta obsoleta y 428 si falta. Recuperar copia el rechazo en un borrador separado. Guardado excesivo da 413. Paginación de gestión e historial en API. |
| CU-06 | 01/10/12/14 | Solo Alumno solicita. Identidad del token y persistencia PENDIENTE con 201. 409 para duplicado o cierre y 413 sin registrar. El borrador en revisión no modifica la admisión de una publicación vigente. |
| CU-07 | 01/12/13 | Admin filtra pendientes, con paginación optativa en API, y decide una vez. 409 ante decisión repetida y 413 sin cambiar la inscripción. Los pendientes anteriores al cierre pueden resolverse. |
| CU-08 | 01/09/11/12 | Alumno propio, Profesor de sus cursos y Admin. Progreso computado contra módulos de la publicación vigente y conservación de completados históricos. Consulta global de Admin y paginación en API. |
| CU-09 | 01/09/11 | Admin consulta y crea cuentas internas por API. 201, 400/409/413 y paginación optativa de listados. El prototipo ofrece consulta y simulación local del estado. |
| CU-10 | 01/09/11 | Admin consulta conteos por rango civil con zona opcional, incluidos bordes, valores cero y 400 por filtro inválido. El prototipo usa la zona local y Bruno comprobó los cuatro recorridos. |
| CU-11 | 01/03/09/11 | Catálogo API con JWT para los tres roles, arreglo histórico o página y 404 para detalle no disponible. El prototipo permite consulta pública local. Inscribirse requiere Alumno. |
| CU-12 | 01/11/12 | Listado paginable de inscripciones propias y contenido solo de la publicación para el titular APROBADO. 403 para inscripción ajena, 409 para estado incompatible. Cerrar la admisión conserva el acceso. |
| CU-13 | 01/12/13 | Titular o Admin cancela una APROBADA. 200, 403 por alcance y 409 por estado. Se conserva historial, se retira el acceso y una inscripción posterior tiene progreso propio. |
| CU-14 | 01/11/13 | Profesor propietario/Admin pausa o reanuda dentro de las transiciones válidas. 403/409 sin cambio. Conserva la publicación y las inscripciones. La pausa cierra nuevas solicitudes y reanudar las habilita. |
| CU-15 | 01/11/13 | Solo Admin finaliza un curso publicado o pausado, con 403/409 fuera de permiso o estado. Cierra nuevas solicitudes y conserva pendientes, acceso aprobado y progreso. |
| CU-16 | 01/10/12 | Titular aprobado completa un módulo vigente con un PUT idempotente. Los cambios de identidad no heredan avances y los completados históricos se conservan. El prototipo permite marcar un módulo como pendiente en sus propios datos. |

### Decisiones pendientes y recomendación

| ID | Alternativas y consecuencias | Recomendación para el equipo |
|---|---|---|
| VAL-002 | Mantener DNI y teléfono internacionales evita excluir formatos válidos. Validar por jurisdicción exige identificar jurisdicción, excepciones y datos que requieren revisión. Para cuentas nuevas se puede mantener la validación actual o adoptar una política de contraseñas que incluya bloqueo de claves comunes y comprometidas. Reaplicar reglas nuevas al login puede bloquear cuentas existentes. | Mantener la compatibilidad y definir reglas nacionales solo ante un requisito. Para nuevas altas y cambios de contraseña con autenticación de un solo factor, proponer un mínimo de 15 caracteres, admitir al menos 64 y aceptar Unicode, pegado y gestores. Usar una lista de claves comunes/comprometidas, evitar composición obligatoria y rotación periódica, y versionar cualquier normalización nueva para preservar el login anterior. La propuesta requiere acuerdo y pruebas antes de activarse. [NIST SP 800-63B-4](https://pages.nist.gov/800-63-4/sp800-63b.html#passwords). |
| VAL-004 | Los topes por colección son simples de explicar, pero pueden rechazar cursos históricos editables. Un presupuesto combinado cubre cargas de distinto tipo y necesita definir tamaños, mensajes y tratamiento de ediciones históricas. El límite de transporte depende del servidor o intermediario. | Conservar límites escalares actuales y medir cursos representativos, latencia, memoria y concurrencia antes de elegir un máximo de dominio. Acordar cómo editar históricos por encima de cualquier tope. [Configuración de límites de Kestrel](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options?view=aspnetcore-10.0). |
| DOC-004 | Con cupo informativo se conserva la resolución de pendientes y puede aprobarse por encima del valor registrado. Un cupo efectivo en la aprobación debe decidir de forma atómica quién ocupa la última plaza, qué estado conserva quien queda sin cupo y qué pasa al cancelar. El bloqueo de aprobaciones en pausa o finalización evita admisiones tras el cierre y requiere decidir si los pendientes se reintentan, se rechazan explícitamente o quedan sin resolver. Conservar su resolución permite cerrar la cola anterior. | Conservar las reglas observadas mientras se decide, por separado, si el cupo es obligatorio y qué ocurre con solicitudes pendientes al pausar o finalizar. Si se aprueba un cupo efectivo, cubrir dos aprobaciones concurrentes por la última plaza, cancelación y reapertura. |
| DOC-003 | Si la cátedra acepta un visor OpenAPI equivalente, Scalar cubre la navegación del contrato comprobado. Si exige Swagger UI por nombre o evidencia específica, incorporar ese entregable. El esquema y el DER permiten cotejar persistencia, pero la clasificación académica depende de la rúbrica. | Solicitar el criterio oficial de herramienta, los niveles de complejidad y sus evidencias exigidas antes de certificar el cumplimiento. |

### Verificación y preparación para integrar

- **Backend.** `dotnet build Lumen.slnx` finalizó con 0 advertencias y 0 errores. `dotnet test` aprobó 389 pruebas (102 unitarias, 287 de integración), incluidas las cinco migraciones en SQLite temporal y el ciclo de la quinta migración con datos. Se verificaron 31 operaciones OpenAPI, referencias de esquemas, metadatos Bearer, encabezados condicionales y disponibilidad de Scalar.
- **Frontend y Bruno.** El prototipo aprobó 10 pruebas, lint y build. Bruno CLI 4.3.0 aprobó 19 requests y 54 tests de exposición, 37 y 71 del recorrido general, y 8 y 14 del recorrido de rechazo/recuperación. Son 58 requests distintos, 64 ejecuciones y 139 tests en tres instancias temporales.
- **Dependencias.** NuGet directo y transitivo y npm de producción informaron cero avisos. npm completo informó siete entradas moderadas de desarrollo que corresponden a un aviso de minimatch. [Aviso de GitHub](https://github.com/advisories/GHSA-g3hv-75wg-m249).
- **Documentos.** Los 35 RF, 14 RNF y 16 RN del Informe General coinciden con Word y con el Markdown de `main`. Coinciden los 217 pasos numerados del consolidado Word y Markdown. Los cambios de transporte (CU-01 paso 6, CU-02 paso 4, CU-03 paso 8, CU-04 paso 7, CU-05 paso 6, CU-06 paso 6, CU-07 paso 6 y CU-09 alternativo A1 paso 3.4), login (CU-02 paso 4) y desmarcado local (CU-16 paso 5) quedaron dentro del flujo correspondiente. Se revisaron los 16 Markdown, los README y el modelo de datos. Se renderizaron y revisaron 23 páginas del consolidado y 9 del Informe General. El consolidado conserva su número de páginas, estilos, encabezados, tablas y estructura. En el archivo Word se cambiaron los textos de nueve párrafos.
- **Diff.** `git diff --check` pasó contra `HEAD` y contra `main`. Los dos archivos nuevos de esta ronda también se revisaron por espacios finales.
- **Riesgos residuales.** La paginación por desplazamiento puede mover resultados entre consultas concurrentes. El límite de login es por IP de conexión e instancia y la latencia depende de los hashes históricos. Si un intermediario rechaza un cuerpo antes de llegar a la API, puede emitir su propio formato de 413. Algunos envíos históricos pueden carecer de instantánea verificable. El prototipo y la API conservan los alcances diferenciados registrados por caso.
- **Integración.** La rama queda preparada para revisión e integración técnica, condicionada a que GitHub Actions pase en la plataforma remota y a planificar la aplicación de las migraciones sobre el entorno objetivo. Comunicar la obligatoriedad de `If-Match` en guardado y recuperación antes de actualizar clientes existentes. Las cuatro decisiones deben permanecer abiertas hasta que existan criterios aprobados. No se hicieron commits, pushes ni merges.

### Archivos intervenidos en la cuarta ronda

El inventario de las rondas anteriores permanece en el registro. La cuarta ronda intervino 25 rutas. El estado acumulado pasó de 77 a 84 rutas respecto de `3e1e875`.

| Área | Rutas de esta ronda |
|---|---|
| HTTP | `4-Codigo/API/Errors/ApiExceptionMiddleware.cs`, `4-Codigo/API/OpenApi/EndpointContractOperationTransformer.cs` |
| Servicios y persistencia | `4-Codigo/BusinessLogic/Services/AuthService.cs`, `4-Codigo/DataAccess/Repositories/CourseRepository.cs`, `4-Codigo/Migrations/DatabaseMigrations/20261106220000_PreventReviewSnapshotReplacement.cs` |
| Regresiones | `4-Codigo/Tests/Lumen.API.IntegrationTests/OpenApiContractTests.cs`, `CourseReviewHistoryIntegrationTests.cs` y `RequestBoundaryIntegrationTests.cs` en la misma carpeta, más `4-Codigo/Tests/Lumen.BusinessLogic.Tests/AuthServiceTests.cs` |
| Casos de uso | En `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/`, el consolidado `Lumen_Actores_CasosDeUso.docx` y `Lumen_Actores_CasosDeUso.md`, más los Markdown individuales CU-01, CU-02, CU-03, CU-04, CU-05, CU-06, CU-07, CU-09 y CU-16 |
| Documentación técnica y registro | `README.md`, `4-Codigo/README.md`, `4-Codigo/bruno/README.md`, `4-Codigo/MODELO_DE_DATOS.md`, `AUDITORIA_Y_PLAN_DE_CORRECCIONES.md` |

## Decisiones consideradas antes de la quinta ronda

Las filas VAL-004, VAL-002 y DOC-004 de esta tabla describen el análisis previo; sus decisiones aprobadas y su implementación vigente constan en el cierre de la quinta ronda. Solo permanece pendiente la confirmación académica de DOC-003.

| ID | Decisión, alternativas y consecuencias | Archivos y pruebas que dependen |
|---|---|---|
| VAL-004 | Elegir topes por cantidad de módulos, recursos y textos o un presupuesto combinado por solicitud. Se recomienda medir tamaños de cursos y clientes, y acordar si los históricos que superen el límite se editan gradualmente. Un tope pequeño puede bloquear la edición de un curso válido. La duración ya debe ser positiva por módulo y el total no supera Int32. Se conservan límites escalares de 200 caracteres para títulos y nombres de módulo, 4.000 para descripción de curso, 2.000 para descripción de módulo y 8.000 para contenido del módulo. Los ejemplos demo de dos o tres módulos no justifican un máximo. | CourseService.BuildRevision y NormalizeList, CourseForm, OpenAPI, CU-03/05. Pruebas al límite, excedido sin escritura y edición de un curso histórico por encima del límite. |
| VAL-002 | Elegir si DNI/teléfono requieren reglas por jurisdicción y si las cuentas nuevas deben cumplir una política de contraseña. Se recomienda conservar el formato internacional y definir reglas solo si hay una necesidad funcional. La validación de alta no se reaplica en login. El mínimo de seis caracteres ya existente en el formulario de demostración sigue circunscripto al prototipo. | ServiceGuard, UserFieldLimits, Register, CU-01/02/09. Regresiones de valores internacionales, cuentas históricas y política futura cuando se apruebe. |
| DOC-004 | Elegir cupo informativo o restricción efectiva, y decidir por separado si las solicitudes anteriores pueden resolverse con el curso cerrado. Se recomienda conservar el comportamiento verificado mientras se define la política. Restringir requiere decidir reintentos, estado al agotarse plazas y qué ocurre con pendientes ya creadas. | CourseService, EnrollmentService, EnrollmentRepository, CU-06/07/12/14/15/16. Si se impone el cupo, probar dos aprobaciones simultáneas por la última plaza y ambas transiciones operativas. |
| DOC-003 | La cátedra debe confirmar si acepta Scalar y qué rúbrica define los niveles de complejidad. Incorporar Swagger UI solo si el requisito exige esa herramienta. | Program, README, DER e Informe General. OpenApiAndScalar_AreAvailable y pruebas de contrato; la equivalencia académica requiere documentación oficial de la materia. |

## Hallazgos de la segunda etapa


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
| BUG-002 | RF-14 a RF-16 | RN-06/07 | CU-04/CU-05 | CourseService.RecoverRejectedDraftAsync; CoursesController; courseService.ts | CourseReviewHistoryIntegrationTests; demo.test.mjs; Courses/15–16 | CU-04/CU-05 | Consolidado CU-04/CU-05 | COMPLETADO |
| BUG-003 | RF-15/24/26/27 | RN-06/11/13 | CU-08/CU-11/CU-12/CU-16 | courseService.ts; Learning.tsx; CourseAndRequests.tsx | demo.test.mjs | CU-08/CU-11/CU-12/CU-16 | Consolidado CU-08/CU-11/CU-12/CU-16 | COMPLETADO |
| BUG-004 | RF-24 a RF-28 | RN-09/11/13 | CU-06/CU-08/CU-12/CU-13 | AppContext; appReducer; courseService.ts | demo.test.mjs; EnrollmentCreationIntegrationTests | CU-06/CU-08/CU-12/CU-13 | Consolidado CU-06/CU-08/CU-12/CU-13 | COMPLETADO |
| BUG-005 | RF-33 a RF-35 | RN-16 | CU-10 | Reports.tsx; format.ts | demo.test.mjs | CU-10 | Consolidado CU-10 | COMPLETADO |
| BUG-006 | RF-15/17 a RF-20 | RN-08/12 | CU-06/CU-11/CU-14/CU-15 | courseService.ts; appReducer; AppContext | demo.test.mjs; EnrollmentCreationIntegrationTests | CU-06/CU-11/CU-14/CU-15 | Consolidado CU-06/CU-11/CU-14/CU-15 | COMPLETADO |
| BUG-007 | RF-25 | RN-11 | CU-13 | CourseAndRequests.tsx; appReducer | demo.test.mjs; Enrollments/07 | CU-13 | Consolidado CU-13 | COMPLETADO |
| VAL-001 | RF-01/03 | RN-01 | CU-01 | UserCreateDTO; AuthService | AccountValidationIntegrationTests; OpenApiContractTests; Auth/04 | CU-01 | Consolidado CU-01 | COMPLETADO |
| VAL-002 | RF-02/04/06 | RN-01/02 | CU-01/CU-02/CU-09 | ServiceGuard; AuthService; UserService; Register.tsx | AccountValidationIntegrationTests; AuthServiceTests; Bruno 01-Politicas | CU-01/CU-02/CU-09 | Consolidado CU-01/CU-02/CU-09 e Informe General 5.6 | COMPLETADO |
| VAL-003 | RF-33 a RF-35 | RN-16 | CU-10 | ReportsController; ReportService; ReportDTOs | ReportsIntegrationTests; TraceabilityContractIntegrationTests; Reports/04 | CU-10 | Consolidado CU-10 | COMPLETADO |
| VAL-004 | RF-09/10/12 | RN-04/15 | CU-03/CU-04/CU-05 | CourseService.BuildRevision; CourseForm | ApprovedCourseLimitsIntegrationTests; Bruno 01-Politicas | CU-03/CU-04/CU-05 | Consolidado CU-03/CU-04/CU-05 e Informe General 5.6 | COMPLETADO |
| SEC-001 | RF-05/07 | Permisos por rol | CU-02 | DemoAccounts; JwtTokenService; Program | DemoSeedingIntegrationTests; JwtSecurityIntegrationTests | CU-02 | Consolidado CU-02 | COMPLETADO |
| SEC-002 | RF-01/05; RNF-07 | RN-01 | CU-01/CU-02 | demoCredentials; AppContext; Register | demo.test.mjs | CU-01/CU-02 | Consolidado CU-01/CU-02 | COMPLETADO |
| SEC-003 | RF-05/07 | Permisos por rol | CU-02 | AuthService; LoginRateLimit; AuthController | AccountValidationIntegrationTests; TraceabilityContractIntegrationTests; OpenApiContractTests | CU-02 | Consolidado CU-02 | COMPLETADO |
| SEC-004 | RNF-04/05 | No aplica | CU-02 | global.json; proyectos .NET | Build, suite completa y auditoría NuGet | README/registro | Actualización de servicio sin cambio del flujo funcional | COMPLETADO |
| SEC-005 | RNF-04/08 | No aplica | Transversal | package.json; package-lock.json | npm audit, lint, build y demo.test.mjs | README/registro | Parche de herramientas sin cambio del flujo funcional | COMPLETADO |
| DB-001 | RNF-07/12 | No aplica | 02 (semilla) | DbInitializer; CourseRepository | DemoSeedingIntegrationTests | README/registro | Atomicidad interna de semilla; mantiene el flujo de la demo | COMPLETADO |
| DB-002 | RF-10/24/27; RNF-12 | RN-11/13/15 | CU-03/CU-05/CU-08/CU-12/CU-16 | CourseService; CourseRepository; EnrollmentRepository; ProgressService | CourseReviewHistoryIntegrationTests; EnrollmentPersistenceIntegrationTests; demo.test.mjs | CU-03/CU-05/CU-08/CU-12/CU-16 | Consolidado CU-03/CU-05/CU-08/CU-12/CU-16 | COMPLETADO |
| DB-003 | RF-19/23/26/32; RNF-03 | RN-08/10/12 | CU-05/CU-07/CU-09/CU-11/CU-12 | ListPaging; PagedQuery; repositorios y controladores de listados | PagingIntegrationTests; OpenApiContractTests; medición sintética de segunda etapa | CU-05/CU-07/CU-09/CU-11/CU-12 | Consolidado CU-05/CU-07/CU-09/CU-11/CU-12 | COMPLETADO |
| DB-004 | RF-16; RNF-12/13 | RN-06 | CU-05 | CourseEditTag; CoursesController; CourseService | CourseConcurrencyIntegrationTests; TraceabilityContractIntegrationTests; Courses/12–13 | CU-05 | Consolidado CU-05 | COMPLETADO |
| DB-005 | RF-14/16; RNF-13 | RN-06/07 | CU-04/CU-05 | CourseReviewSnapshots; PreserveReviewSnapshots; GetReviewSnapshot | CourseReviewHistoryIntegrationTests; OpenApiContractTests; Courses/14 | CU-04/CU-05 | Consolidado CU-04/CU-05 | COMPLETADO |
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
| DOC-003 | RNF-06 | Consigna de cátedra | Transversal | Program; MODELO_DE_DATOS | OpenApiContractTests valida Scalar y OpenAPI; rúbrica ausente | MODELO_DE_DATOS y README | Informe General 5.6 registra el límite académico | BLOQUEADO |
| DOC-004 | RF-09/17/18/20/23/24 | RN-10 a RN-12 | CU-06/CU-07/CU-12/CU-13/CU-14/CU-15/CU-16 | CourseService; EnrollmentService; EnrollmentRepository; CourseRepository; appReducer | ApprovedCapacityIntegrationTests; CourseClosureEnrollmentIntegrationTests; Bruno 01-Politicas; demo.test.mjs | CU-06/CU-07/CU-12/CU-13/CU-14/CU-15/CU-16 | Consolidado de esos CU e Informe General 5.6 | COMPLETADO |

## Auditoría funcional de los 16 casos de uso


Se cotejaron actores, objetivos, disparadores, precondiciones, flujos, excepciones, postcondiciones y permisos de cada especificación con la API y el prototipo. La tabla resume la comprobación por caso. La columna Bruno indica recorridos disponibles; las pruebas automatizadas cubren errores adicionales.

| Caso y actor | RF | RN | Contrato | Prueba automatizada | Bruno | Estado y alcance |
|---|---|---|---|---|---|---|
| CU-01 · Registro Alumno/Profesor | RF-01 a RF-04 | RN-01/02 | `POST /api/auth/register` | AccountValidationIntegrationTests; RegistrationConcurrencyRegressionTests | Auth/04; Politicas/01–08 | Rol y datos explícitos. 201; 400 por DNI, teléfono, CPA o contraseña fuera de política sin crear cuenta; 409 por duplicado. |
| CU-02 · Login de los tres roles | RF-05/07/29 | Permisos por rol | `POST /api/auth/login` | AccountValidationIntegrationTests; TraceabilityContractIntegrationTests; JwtSecurityIntegrationTests | Auth/01–03 | 200 con token; 400, 401, 403 o 429 según el flujo. Los tokens demo solo sirven en entornos demo. |
| CU-03 · Creación y envío por Profesor/Admin | RF-08 a RF-13 | RN-03 a RN-06/15 | `POST /api/courses; POST /api/courses/{id}/submit-review` | CourseCreationIntegrationTests; ApprovedCourseLimitsIntegrationTests; OpenApiContractTests | Courses/03,05; Exposición/04–05; Politicas | 201 crea borrador; 200 crea solicitud. Los topes aprobados se validan sin guardar entradas excedidas. El alta administrativa está en la API; el prototipo crea por Profesor. |
| CU-04 · Auditoría del Administrador | RF-14/15 | RN-06/07 | `GET /api/course-reviews y /api/course-reviews/{id}; POST /api/course-reviews/{id}/decision` | CourseReviewHistoryIntegrationTests; CourseConcurrencyIntegrationTests | Courses/09–11,14–15 | 200 aplica decisión. El historial conserva la copia de cada nuevo envío y los metadatos anteriores. El rechazo conserva la publicación anterior. Recuperación mediante CU-05. |
| CU-05 · Edición Profesor propietario/Admin | RF-09 a RF-16/31 | RN-03 a RN-07/15 | `GET y PUT /api/courses/{id}; POST /api/courses/{id}/recover-draft y /submit-review` | CourseReviewHistoryIntegrationTests; ApprovedCourseLimitsIntegrationTests; ApprovedCapacityIntegrationTests | Courses/02,04,12–13,15–16; Politicas | Guardado y recuperación requieren If-Match. 403 por alcance, 409 por estado/cupo, 412 por etiqueta obsoleta y 428 por falta de etiqueta. Excesos históricos solo pueden reducirse gradualmente. |
| CU-06 · Solicitud del Alumno | RF-19 a RF-22 | RN-08 a RN-10/12 | `POST /api/enrollments` | EnrollmentCreationServiceTests; EnrollmentCreationIntegrationTests; EnrollmentPersistenceIntegrationTests | Enrollments/01; Exposición/08; Politicas | 201 registra PENDIENTE sin reservar plaza. 409 por duplicado o cierre. Abandonar el formulario no muta. |
| CU-07 · Resolución por Administrador | RF-23/24 | RN-10/11 | `GET /api/enrollments?status=Pendiente; POST /api/enrollments/{id}/decision` | EnrollmentResolutionConcurrencyIntegrationTests; ApprovedCapacityIntegrationTests; PagingIntegrationTests | Enrollments/03,06; Exposición/09–10; Politicas | 200 resuelve pendientes; cupo completo o decisión tardía responde 409. La pausa permite resolver pendientes; finalizar las rechaza con motivo. La última plaza se asigna atómicamente. |
| CU-08 · Progreso Alumno/Profesor/Admin | RF-27/28/32 | RN-11/13/14 | `GET /api/enrollments/{id}/progress; GET /api/courses/{id}/students/progress` |ProgressServiceTests; ProgressIntegrationTests; demo.test.mjs; PagingIntegrationTests | Enrollments/08,10; Exposición/12,14,18–19 | El progreso cuenta módulos sin cambios en la publicación vigente. Los completados históricos permanecen almacenados. La API pagina el avance de inscripciones aprobadas. |
| CU-09 · Usuarios por Administrador | RF-06/32 | RN-01/02 | `GET /api/users; GET /api/users/{id}; POST /api/users/administrators` | AccountValidationIntegrationTests; OpenApiContractTests; RegistrationConcurrencyRegressionTests; PagingIntegrationTests | Users/01–03; Politicas | 201 alta, 400 por política argentina o paginación y 409 correo duplicado. La API ofrece alta interna; el prototipo consulta y simula el estado. |
| CU-10 · Reportes por Administrador | RF-33 a RF-35 | RN-16 | `GET /api/reports/summary` | ReportServiceTests; ReportsIntegrationTests; TraceabilityContractIntegrationTests | Reports/01–04 | 200 con conteos o cero; 400 por límites o fechas inválidos. Fechas civiles con zona optativa y prueba de cambio horario. |
| CU-11 · Catálogo de los tres roles | RF-19 | RN-08/12 | `GET /api/catalog/courses; GET /api/catalog/courses/{id}` | ApiIntegrationTests; TraceabilityContractIntegrationTests; demo.test.mjs | Catalog/01–02 | 200 arreglo o detalle, 404 si ya no está disponible y 400 por paginación inválida. Solo Alumno solicita inscripción. |
| CU-12 · Contenido para Alumno titular | RF-24/26/30 | RN-11/12 | `GET /api/enrollments/mine; GET /api/enrollments/{id}/course-content` |EnrollmentCreationIntegrationTests; ProgressIntegrationTests; demo.test.mjs; PagingIntegrationTests | Enrollments/02,04–05; Exposición/11 | 200 consulta o acceso a publicación. 403 inscripción ajena, 409 estado incompatible. Listado propio paginable; cierre del catálogo conserva el acceso. |
| CU-13 · Cancelación Alumno titular/Admin | RF-25 | RN-11 | `POST /api/enrollments/{id}/cancel` | EnrollmentAndProgressServiceTests; ApprovedCapacityIntegrationTests; demo.test.mjs | Enrollments/07; Politicas | 200 cancela, libera plaza y conserva historial; 403 por rol/titularidad y 409 si no estaba aprobada. |
| CU-14 · Pausa y reanudación Profesor propietario/Admin | RF-17 | RN-08/12 | `POST /api/courses/{id}/pause y /resume` | CourseClosureEnrollmentIntegrationTests; OpenApiContractTests; demo.test.mjs | Courses/06–07; Politicas | 200 en transición válida. La pausa cierra nuevas solicitudes y permite resolver las anteriores si hay plaza; 403 por alcance o 409 por estado. |
| CU-15 · Finalización Administrador | RF-18 | RN-12/13 | `POST /api/courses/{id}/finish` | CourseClosureEnrollmentIntegrationTests; ApprovedCapacityIntegrationTests; demo.test.mjs | Courses/08; Politicas | 200 finaliza curso y rechaza todas las pendientes con motivo en la misma transacción. Conserva acceso y progreso aprobados. 403/409 ante rechazo. |
| CU-16 · Módulos del Alumno titular | RF-24/27 | RN-11/13/15 | `PUT /api/enrollments/{id}/modules/{moduleId}/completion` | ModuleCompletionServiceTests; ModuleCompletionIntegrationTests; EnrollmentConcurrencyRegressionTests | Enrollments/09; Exposición/13,15–17 | 200 registra una vez. Módulo modificado, retirado o ajeno no recibe progreso por su identidad anterior. Los módulos nuevos empiezan sin avance. |

## Inventario de la segunda etapa

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

## Archivos modificados en esta ronda

77 rutas respecto de `3e1e875`. El cambio de nombre de CoursePage a ListPage se cuenta como baja y alta en el inventario.

- `1-Entrega Personal/1 - Informe/Lumen_Informe_General.docx`
- `1-Entrega Personal/1 - Informe/Lumen_Informe_General.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/Lumen_Actores_CasosDeUso.docx`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/Lumen_Actores_CasosDeUso.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-01-registrarse.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-04-auditar-solicitud-de-curso.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-05-actualizar-curso-creado.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-07-gestionar-solicitudes-de-inscripcion.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-08-consultar-progreso.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-09-gestionar-usuarios.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-12-consultar-inscripciones-y-acceder-a-cursos.md`
- `1-Entrega Personal/2 - Requerimientos/1 - Casos de Uso/cu-16-realizar-modulos-o-actividades-del-curso.md`
- `3-Interfaz/README.md`
- `3-Interfaz/src/context/AppContext.tsx`
- `3-Interfaz/src/pages/admin/DashboardAndAudits.tsx`
- `3-Interfaz/src/pages/admin/UsersAndCourses.tsx`
- `3-Interfaz/src/pages/public/Register.tsx`
- `3-Interfaz/src/pages/teacher/DashboardAndCourses.tsx`
- `3-Interfaz/src/services/courseService.ts`
- `3-Interfaz/src/types/index.ts`
- `3-Interfaz/tests/demo.test.mjs`
- `4-Codigo/API/Controllers/CoursesController.cs`
- `4-Codigo/API/Controllers/EnrollmentsController.cs`
- `4-Codigo/API/Controllers/UsersController.cs`
- `4-Codigo/API/Errors/ApiExceptionMiddleware.cs`
- `4-Codigo/API/Http/CourseEditTag.cs`
- `4-Codigo/API/Http/ListPaging.cs`
- `4-Codigo/API/OpenApi/EndpointContractOperationTransformer.cs`
- `4-Codigo/BusinessLogic/Interfaces/ICourseService.cs`
- `4-Codigo/BusinessLogic/Interfaces/IEnrollmentService.cs`
- `4-Codigo/BusinessLogic/Interfaces/IProgressService.cs`
- `4-Codigo/BusinessLogic/Interfaces/IUserService.cs`
- `4-Codigo/BusinessLogic/Services/CourseService.cs`
- `4-Codigo/BusinessLogic/Services/EnrollmentService.cs`
- `4-Codigo/BusinessLogic/Services/ProgressService.cs`
- `4-Codigo/BusinessLogic/Services/ServiceGuard.cs`
- `4-Codigo/BusinessLogic/Services/UserService.cs`
- `4-Codigo/DataAccess/Configurations/CourseReviewSnapshotConfiguration.cs`
- `4-Codigo/DataAccess/Entities/CourseReview.cs`
- `4-Codigo/DataAccess/Entities/CourseReviewSnapshot.cs`
- `4-Codigo/DataAccess/Repositories/CourseRepository.cs`
- `4-Codigo/DataAccess/Repositories/EnrollmentRepository.cs`
- `4-Codigo/DataAccess/Repositories/ICourseRepository.cs`
- `4-Codigo/DataAccess/Repositories/IEnrollmentRepository.cs`
- `4-Codigo/DataAccess/Repositories/IUserRepository.cs`
- `4-Codigo/DataAccess/Repositories/PagedQuery.cs`
- `4-Codigo/DataAccess/Repositories/UserRepository.cs`
- `4-Codigo/MODELO_DE_DATOS.md`
- `4-Codigo/Migrations/DatabaseMigrations/20261106212402_PreserveReviewSnapshots.Designer.cs`
- `4-Codigo/Migrations/DatabaseMigrations/20261106212402_PreserveReviewSnapshots.cs`
- `4-Codigo/Migrations/DatabaseMigrations/LumenDbContextModelSnapshot.cs`
- `4-Codigo/README.md`
- `4-Codigo/Shared/DTOs/CourseDTOs.cs`
- `4-Codigo/Shared/DTOs/CoursePage.cs`
- `4-Codigo/Shared/DTOs/ListPage.cs`
- `4-Codigo/Shared/Exceptions/CourseEditPreconditionRequiredException.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/AccountValidationIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/ApiIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/ConcurrencyTestSupport.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/CourseClosureEnrollmentIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/CourseConcurrencyIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/CourseReviewHistoryIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/CourseUpdateIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/EnrollmentConcurrencyRegressionTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/EnrollmentCreationIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/EnrollmentResolutionConcurrencyIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/OpenApiContractTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/PagingIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.API.IntegrationTests/TraceabilityContractIntegrationTests.cs`
- `4-Codigo/Tests/Lumen.BusinessLogic.Tests/EnrollmentCreationServiceTests.cs`
- `4-Codigo/bruno/Courses/13-require-edit-tag.bru`
- `4-Codigo/bruno/Courses/14-review-snapshot.bru`
- `4-Codigo/bruno/Courses/15-reject-review.bru`
- `4-Codigo/bruno/Courses/16-recover-draft.bru`
- `4-Codigo/bruno/README.md`
- `AUDITORIA_Y_PLAN_DE_CORRECCIONES.md`
- `README.md`
