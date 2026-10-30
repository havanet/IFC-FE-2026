# Script - PostgreSQL

Cada tenant tendra su propia base de negocio. Firestore conserva el directorio
central de tenants y su enrutamiento; no se crea aqui otra base de control.

## Estructura

- `migrations/`: cambios versionados de esquema y datos.
- `functions/`: un archivo `.sql` por funcion.
- `procedures/`: un archivo `.sql` por procedimiento almacenado (SP).

Cada SP debe tener un archivo propio, nombrado segun el procedimiento, con su
definicion SQL completa. Los nombres empiezan con `USP_`, por ejemplo,
`procedures/USP_CrearFacturaBorrador.sql`.
No agrupar las definiciones de varios SP en un unico archivo.

El mecanismo automatizado de despliegue y el registro de versiones por base
siguen pendientes. No ejecutar automaticamente todos los archivos de estas
carpetas.

Los cambios deberan poder desplegarse y verificarse por base, registrar errores
y detectar versiones pendientes. No se usa Entity Framework.

La migracion `001_factura_borrador.sql` crea el almacenamiento inicial de
borradores. `USP_CrearFacturaBorrador` y `USP_ObtenerFacturaBorrador` son los
procedimientos de escritura/consulta correspondientes. El contenido se guarda
como JSONB y la idempotencia se aplica atomicamente por tenant y clave.

## Creacion manual inicial en pgAdmin

Seleccionar la base del emisor (para desarrollo local, `ifc_facturacion_dev`)
y abrir **Query Tool**. Ejecutar los archivos completos en este orden:

1. [001_factura_borrador.sql](migrations/001_factura_borrador.sql):
   crea `public.factura_borrador`, su clave primaria, la restriccion unica de
   idempotencia y el indice de tenant/emisor/fecha.
2. [USP_CrearFacturaBorrador.sql](procedures/USP_CrearFacturaBorrador.sql):
   crea el procedimiento de registro.
3. [USP_ObtenerFacturaBorrador.sql](procedures/USP_ObtenerFacturaBorrador.sql):
   crea el procedimiento de consulta.

La migracion inicial se ejecuta una sola vez por base. Si la tabla ya existe,
el script falla explicitamente: no elimina datos ni oculta diferencias de
esquema mediante `IF NOT EXISTS`. Los SP usan `CREATE OR REPLACE PROCEDURE`.

Los dos SP solo referencian `public.factura_borrador`. Por ahora no hay tablas
independientes de detalle, impuestos, adquirentes o pagos: esos datos forman
parte de la instantanea `documento` de tipo JSONB. `huella_solicitud` conserva
el contenido utilizado para comparar reintentos de la misma clave.
Este esquema no requiere `pgvector`.

Los totales siguen pendientes y se guardan como null: no se aplican reglas de
calculo ni redondeo hasta definirlas y validarlas. No guardar datos reales,
credenciales ni certificados en estos scripts.
