# Lumen — Interfaz

Prototipo frontend Hi-Fi del Sistema de Gestión de Cursos Online.

## Ejecutar

    npm install
    npm run dev

Build de producción:

    npm run build

## Usuarios demo

| Rol | Email | Contraseña |
| --- | --- | --- |
| Alumno | alumno@lumen.demo | demo123 |
| Profesor | profesor@lumen.demo | demo123 |
| Administrador | admin@lumen.demo | demo123 |

Los datos se persisten en localStorage bajo la clave lumen-demo-state. La opción **Restablecer demostración** recupera el seed inicial.

## Vercel

Configurar **Root Directory** como 3-Interfaz. El archivo vercel.json incluye el rewrite necesario para las rutas SPA. No se requieren variables de entorno.
