# LUMEN — Sistema de Gestión de Cursos Online

LUMEN es un proyecto académico de **Práctica Profesionalizante I**, de la Tecnicatura Superior en Desarrollo de Software. Incluye una API REST para gestionar cursos, usuarios, inscripciones y progreso, y un prototipo frontend navegable independiente.

**[Guía del backend: instalación, configuración, ejecución y pruebas](4-Codigo/README.md)**

## Contenido

- [Descripción y objetivo](#descripción-y-objetivo)
- [Funcionalidades principales](#funcionalidades-principales)
- [Tecnologías utilizadas](#tecnologías-utilizadas)
- [Arquitectura del backend](#arquitectura-del-backend)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Instalación y ejecución](#instalación-y-ejecución)
- [Documentación y pruebas de la API](#documentación-y-pruebas-de-la-api)
- [Prototipo frontend](#prototipo-frontend)
- [Documentación académica](#documentación-académica)
- [Integrantes y datos académicos](#integrantes-y-datos-académicos)
- [Estado de la entrega](#estado-de-la-entrega)

## Descripción y objetivo

La administración de una oferta de cursos requiere coordinar su preparación, aprobación, publicación, inscripción de alumnos y seguimiento. LUMEN reúne esos procesos con responsabilidades diferenciadas para **Alumno, Profesor y Administrador**.

El objetivo es representar ese circuito mediante reglas de negocio, estados y permisos explícitos, con persistencia de datos y contratos HTTP que puedan verificarse mediante pruebas automatizadas.

## Funcionalidades principales

El backend implementa los **16 casos de uso** del proyecto:

| Área | Funcionalidades implementadas | Casos de uso |
|---|---|---|
| Acceso | Registro de alumnos y profesores; inicio de sesión con JWT y autorización según rol. | CU-01, CU-02 |
| Cursos | Creación y edición de cursos y módulos; revisiones de trabajo separadas de la publicación aprobada. | CU-03, CU-05 |
| Auditoría | Envío a revisión y decisión administrativa: aprobar, rechazar o solicitar cambios. | CU-04 |
| Inscripciones | Solicitud, aprobación o rechazo; consulta de inscripciones propias y cancelación de una inscripción aprobada. | CU-06, CU-07, CU-12, CU-13 |
| Aprendizaje | Acceso al contenido publicado con inscripción aprobada, registro de módulos completados y consulta de progreso por alumno y curso. | CU-08, CU-16 |
| Administración | Consulta de usuarios, creación interna de administradores y reportes de resumen por intervalo de fechas. | CU-09, CU-10 |
| Catálogo y disponibilidad | Consulta del catálogo publicado para usuarios autenticados; pausa, reanudación y finalización de cursos según permisos. | CU-11, CU-14, CU-15 |

## Tecnologías utilizadas

| Componente | Tecnologías |
|---|---|
| Backend | C#, .NET 10, ASP.NET Core, inyección de dependencias y arquitectura N-Tier. |
| Persistencia | Entity Framework Core 10, SQLite y migraciones versionadas. |
| Seguridad | JWT Bearer con firma HS256, autorización por roles y hash de contraseñas mediante `PasswordHasher`. |
| Contratos y exploración | OpenAPI y Scalar. |
| Pruebas | xUnit, Moq, `WebApplicationFactory` y SQLite temporal para integración; colección Bruno con verificaciones HTTP. |
| Prototipo frontend | React 18, TypeScript, Vite, Tailwind CSS y React Router; datos locales en el navegador. |

## Arquitectura del backend

La solución mantiene el flujo **Controller → Service → Repository → DbContext → SQLite**:

- **API:** recibe solicitudes HTTP, aplica autenticación y autorización por endpoint y expone OpenAPI/Scalar.
- **BusinessLogic:** implementa reglas de negocio, permisos sobre los recursos, validaciones y transiciones de estado.
- **DataAccess:** contiene entidades, repositorios, configuración de EF Core y `LumenDbContext`.
- **Shared:** reúne DTOs, enumeraciones y excepciones compartidas.
- **Migrations:** contiene las migraciones de esquema y el ejecutor de inicialización.

Los contratos HTTP utilizan DTOs. Las pruebas unitarias y de integración se encuentran en proyectos separados dentro de `Tests`.

## Estructura del repositorio

```text
├── README.md
├── 1-Entrega Personal/          Documentación académica y fichas de casos de uso
├── 2-Documentos Oscar/          Guías y material de referencia de la materia
├── 3-Interfaz/                  Prototipo frontend independiente
└── 4-Codigo/                    Backend
    ├── API/
    ├── BusinessLogic/
    ├── DataAccess/
    ├── Shared/
    ├── Migrations/
    ├── Tests/
    │   ├── Lumen.BusinessLogic.Tests/
    │   └── Lumen.API.IntegrationTests/
    ├── bruno/                  Colección y recorrido 00-Exposicion
    ├── Lumen.slnx
    ├── global.json
    └── README.md
```

## Instalación y ejecución

**Seguir la [guía operativa del backend](4-Codigo/README.md)** para ejecutar desde una copia limpia. Incluye los bloques completos de PowerShell, la configuración segura de JWT, las migraciones y los comandos de inicio y pruebas.

1. Instalar el **SDK .NET 10.0.400** indicado en [global.json](4-Codigo/global.json). Su política `latestPatch` admite parches posteriores de la misma banda **10.0.4xx**. Los ejemplos operativos son compatibles con Windows PowerShell 5.1 y PowerShell 7.
2. Restaurar y compilar desde la raíz del repositorio:

   ```powershell
   Set-Location -LiteralPath '4-Codigo'
   dotnet restore Lumen.slnx
   dotnet build Lumen.slnx -c Release --no-restore
   ```

3. Configurar una clave JWT externa mediante **.NET User Secrets** en Development o **`Jwt__Key`** en el entorno del proceso, siguiendo la guía. La API exige al menos 32 bytes UTF-8 y valida la configuración antes de inicializar la base.
4. Iniciar la API con el bloque de la guía correspondiente al entorno y a la base elegidos. El perfil local `http` utiliza `http://localhost:5080`.
5. Ejecutar las pruebas desde `4-Codigo`, después de compilar:

   ```powershell
   dotnet test Lumen.slnx -c Release --no-build --no-restore
   ```

La API aplica migraciones al iniciar. **Development y Testing** permiten datos demo únicamente si no existen usuarios; **Production no los crea automáticamente**. El ejecutor de migraciones respeta la misma política. La guía de Bruno ofrece una ejecución con SQLite temporal y clave efímera para probar sin utilizar la base habitual.

## Documentación y pruebas de la API

La API expone **29 operaciones HTTP**. Con la instancia local iniciada en el puerto 5080:

- **OpenAPI:** `http://localhost:5080/openapi/v1.json`, con esquemas, respuestas, seguridad por operación, enumeraciones y parámetros.
- **Scalar:** `http://localhost:5080/scalar/v1`, para explorar los contratos y realizar solicitudes.
- **Bruno:** consultar la [guía de la colección](4-Codigo/bruno/README.md), sus entornos y precondiciones.

El recorrido **`00-Exposicion` contiene 19 solicitudes ordenadas**: autenticar los tres roles, crear y aprobar un curso, solicitar y aprobar una inscripción, consultar contenido y completar módulos hasta el 100%, comprobando también la repetición idempotente. Los tokens e identificadores se obtienen de respuestas reales y se mantienen como variables de ejecución. La guía explica cómo utilizarlo con una API temporal en el puerto 5081.

La última ejecución verificada de la suite .NET registró **350 pruebas aprobadas: 101 unitarias y 249 de integración**. Incluye regresiones de concurrencia, JWT, permisos, contratos OpenAPI, logging e inicialización por entorno. Las pruebas de integración utilizan bases SQLite temporales aisladas.

## Prototipo frontend

**[Visualizar el prototipo de LUMEN](https://lumen-bay-phi.vercel.app)**

El prototipo permite navegar las pantallas y flujos propuestos. Utiliza datos locales persistidos en `localStorage` y funciona de forma independiente: **no está integrado con la API en esta entrega**.

Para ejecutarlo localmente, consultar el [README de la interfaz](3-Interfaz/README.md). Sus herramientas de Node.js corresponden al prototipo y no son necesarias para ejecutar el backend.

## Documentación académica

- **[Informe General](https://docs.google.com/document/d/1Cx_dsiIXYoMr_3pcy_dQKhadqeNzyQ1W/edit?usp=drive_link&ouid=115498353339163244987&rtpof=true&sd=true):** definición del proyecto, alcance, requerimientos, estados, permisos y reglas de negocio.
- **[Actores y Casos de Uso](https://docs.google.com/document/d/1D-hUx1-MSs4RW2Y_sViKkLizYKCBs-DG/edit?usp=drive_link&ouid=115498353339163244987&rtpof=true&sd=true):** actores y especificaciones funcionales.

Las copias y fichas académicas están en `1-Entrega Personal`. Se conservan como referencia de la evolución del proyecto; para la instalación y los contratos HTTP vigentes, consultar el README del backend y el OpenAPI generado por la API.

## Integrantes y datos académicos

- **Integrantes:** Rodrigo Gabriel Astore y Nahuel Ferrino.
- **Materia:** Práctica Profesionalizante I.
- **Carrera:** Tecnicatura Superior en Desarrollo de Software.
- **Año:** 2026.
- **Profesores:** Majluf Gabriel — Spila Oscar.

## Estado de la entrega

La entrega incluye el backend implementado con sus 16 casos de uso, pruebas automatizadas, contratos OpenAPI/Scalar, colección Bruno e instrucciones de reproducción local. Se incorporaron las correcciones de concurrencia, logging en Windows y configuración JWT externa, junto con la restricción de cuentas demo por entorno.

El repositorio también conserva la documentación académica y el prototipo frontend publicado. La integración de ese prototipo con la API queda fuera del alcance de esta entrega.
