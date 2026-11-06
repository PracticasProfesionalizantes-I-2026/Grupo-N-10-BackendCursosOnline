# Lumen — Interfaz

Prototipo frontend Hi-Fi del Sistema de Gestión de Cursos Online, independiente de la API. Usar solo datos ficticios. El registro de demostración conserva un derivado local con salt, sin guardar la contraseña original. Usar HTTPS o localhost.

## Ejecutar

    npm ci
    npm run dev

Build de producción:

    npm run build

Verificación con Node.js 24:

    npm test
    npm run lint

El catálogo conserva la versión aprobada durante una auditoría y oculta cursos pausados o finalizados. El contenido y el progreso del alumno usan la publicación vigente. Cada inscripción aprobada tiene su propio avance; los registros locales históricos sin una asignación inequívoca se conservan sin atribuirlos a una inscripción activa.

La edición administrativa y la del profesor usan el mismo formulario de versión de trabajo. Enviar una edición genera una auditoría y conserva la publicación anterior. Los reportes usan fechas civiles en la zona del navegador. La API ofrece por separado un parámetro de zona para obtener el mismo intervalo de fechas.

En el prototipo se puede copiar una revisión rechazada a un borrador y consultar la copia local de cada envío nuevo desde Auditorías. Los completados solo se trasladan a módulos sin cambios al republicar. Las condiciones HTTP de edición y paginación corresponden a la API.

El registro local aplica los formatos argentinos aprobados para DNI, teléfono y código postal, además de contraseñas nuevas de 8 a 128 caracteres. El formulario de cursos muestra los máximos de módulos, objetivos, requisitos y recursos. Una aprobación local ocupa plaza y una cancelación la libera. Pausar conserva las solicitudes anteriores. Finalizar rechaza las pendientes con motivo y conserva acceso y progreso de alumnos aprobados.

## Usuarios demo

| Rol | Email | Contraseña |
| --- | --- | --- |
| Alumno | alumno@lumen.demo | demo123 |
| Profesor | profesor@lumen.demo | demo123 |
| Administrador | admin@lumen.demo | demo123 |

Los datos se persisten en localStorage bajo la clave lumen-demo-state. La opción **Restablecer demostración** recupera el seed inicial.

## Vercel

Configurar **Root Directory** como 3-Interfaz. El archivo vercel.json incluye el rewrite necesario para las rutas SPA. No se requieren variables de entorno.
