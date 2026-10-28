# Script - PostgreSQL

Cada tenant tendra su propia base de negocio. Firestore conserva el directorio
central de tenants y su enrutamiento; no se crea aqui otra base de control.

## Estructura

- `migrations/`: cambios versionados de esquema y datos.
- `functions/`: un archivo `.sql` por funcion.
- `procedures/`: un archivo `.sql` por procedimiento almacenado (SP).

Cada SP debe tener un archivo propio, nombrado segun el procedimiento, con su
definicion SQL completa. Por ejemplo, `procedures/sp_crear_factura.sql`.
No agrupar las definiciones de varios SP en un unico archivo.
Estos nombres son ejemplos, no procedimientos ya implementados.

Antes del primer script ejecutable se definiran el mecanismo de despliegue,
el registro de versiones y como incluir funciones y procedimientos en cada
version. No ejecutar automaticamente todos los archivos de estas carpetas.

Los cambios deberan poder desplegarse y verificarse por base, registrar errores
y detectar versiones pendientes. No se usa Entity Framework.

No hay tablas ni procedimientos implementados todavia. No guardar datos reales,
credenciales ni certificados en estos scripts.
