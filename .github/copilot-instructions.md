# Memoria y reglas del proyecto: facturacion electronica DIAN

## Uso y mantenimiento
- Leer y aplicar estas decisiones al trabajar en este proyecto.
- Actualizar este archivo cuando el usuario apruebe cambios de arquitectura o nuevas reglas. No convertir sugerencias en decisiones confirmadas.
- Mantener separados los acuerdos, los pendientes y el avance verificado. No afirmar que una funcionalidad existe sin revisar su implementacion.
- Antes de modificar la solucion, revisar el codigo y las instrucciones existentes. No reemplazar trabajo del usuario.

## Objetivo y alcance
- Construir una plataforma multiempresa de facturacion electronica para Colombia, integrada con la DIAN.
- Empezar por una factura electronica completa. Incorporar gradualmente notas credito y debito, reutilizando componentes comunes sin implementar sus flujos prematuramente.
- El alcance incluye XML, firma, envio DIAN, consulta de estados, PDF, correo, reintentos y trazabilidad.
- La propiedad del codigo no determina por si sola la modalidad regulatoria de software propio o proveedor tecnologico. Verificar los requisitos de habilitacion por emisor.

## Stack acordado
- API en C# con ASP.NET Core / .NET 10 LTS, destinada a GCP Cloud Run. SDK fijado en global.json.
- Acceso a PostgreSQL mediante Dapper y Npgsql. NO usar Entity Framework ni sus migraciones.
- Mantener una parte importante de la logica de datos y de negocio transaccional en funciones y procedimientos almacenados PostgreSQL.
- Evolucionar la base mediante scripts SQL versionados y automatizables, con registro de version por base.
- Guardar los scripts de base de datos en src/IFC.Facturacion.Infrastructure/Script, dentro de Infraestructura.
- Cada procedimiento almacenado (SP) debe tener su propio archivo .sql con su definicion completa, nombrado segun el procedimiento. No agrupar varios SP en un mismo archivo de definiciones.
- Mantener tambien una definicion por archivo para las funciones. Separar las definiciones de objetos de las migraciones versionadas.
- Firebase Authentication para identidad.
- Firestore para el directorio central de tenants, membresias, permisos y enrutamiento privado hacia PostgreSQL; no como repositorio principal de facturas.
- Cloud SQL para PostgreSQL aloja las bases de negocio. Firebase SQL Connect no es necesario para la conexion Dapper/Npgsql y no forma parte del acceso inicial.
- Cloud Run functions puede incorporarse si hay una necesidad concreta; no fragmentar la aplicacion sin justificacion.

## Tenants y distribucion de bases
- Una base PostgreSQL por tenant, con varias bases dentro de una misma instancia Cloud SQL.
- No crear una instancia por cliente como arquitectura predeterminada.
- Permitir varias instancias y mantener el mapeo tenant -> instancia -> base en el directorio privado de Firestore.
- Diez bases por instancia es una referencia inicial de agrupacion, no una garantia ni un limite tecnico de rendimiento. Distribuir segun carga, conexiones, latencia y capacidad.
- Cloud SQL cobra por recursos y servicios, no una tarifa fija por cada base dentro de la instancia. No prometer precios sin estimacion actual.
- Distinguir tenant, emisor, usuario y sucursal. Un tenant puede administrar varios emisores; cada emisor mantiene su configuracion fiscal y certificado correspondiente.
- Autorizar al usuario para el tenant antes de resolver la conexion. No aceptar nombres de base ni cadenas de conexion arbitrarias desde el cliente.
- La interfaz nunca recibe credenciales PostgreSQL ni se conecta directamente a la base.
- Usar permisos de base restringidos por tenant y controlar los pools Npgsql junto con la concurrencia y el escalado de Cloud Run.
- Automatizar despliegues SQL en todas las bases y contemplar fallos parciales y versiones diferentes.
- Los backups administrados y PITR de Cloud SQL son de instancia. La recuperacion individual requiere un procedimiento especifico probado.

## Identidad, certificados y archivos
- Validar los tokens Firebase en el backend. Autenticacion no sustituye autorizacion.
- Un usuario puede pertenecer a varios tenants con roles diferentes. Verificar acceso en cada operacion protegida.
- Separar documentos Firestore visibles para la interfaz de la configuracion privada de enrutamiento.
- Guardar secretos y material de certificados en Secret Manager; persistir referencias y metadatos, no claves privadas o contrasenas en columnas ordinarias, codigo o logs.
- Asociar certificados al emisor correspondiente y verificar los requisitos aplicables. No reutilizar el certificado de otro emisor por conveniencia.
- Registrar vigencia, huella y version de certificado utilizada. Contemplar renovacion sin modificar documentos historicos.
- Conservar XML, respuestas y PDF en Cloud Storage con acceso autorizado por tenant. No sobrescribir silenciosamente artefactos firmados o enviados.

## Reglas de implementacion de facturacion
- Revisar anexo tecnico, XSD, reglas y ejemplos oficiales aplicables antes de fijar clases XML. No inferir todo el contrato desde un XML de ejemplo.
- Separar contratos de API, modelos de negocio y modelos XML UBL/DIAN.
- Usar numeric en PostgreSQL y decimal en C#; definir precision y redondeos explicitamente. Evitar motores de calculo independientes con reglas divergentes.
- Controlar numeracion concurrentemente por emisor y configuracion aplicable. Un borrador no consume numeracion fiscal definitiva.
- No llamar a DIAN, enviar correos ni generar PDF desde procedimientos almacenados. No mantener transacciones abiertas durante llamadas externas.
- Implementar idempotencia para creacion, emision y tareas. Un reintento no debe crear otra factura.
- Ante un envio con resultado incierto, consultar o conciliar antes de reenviar a ciegas.
- Separar estados del documento, DIAN, PDF y correo. Un fallo de correo no invalida una factura aceptada.
- Registrar intentos, respuestas, errores, correlacion y eventos con contexto de tenant y emisor.
- Usar trabajo durable: outbox transaccional y despacho recuperable; Cloud Tasks es la opcion inicial para tareas HTTP con reintentos controlados.
- No depender de trabajo en segundo plano posterior a la respuesta HTTP dentro de Cloud Run.
- Las tareas identifican al tenant y resuelven su ubicacion al ejecutarse. Coordinar cambios de ubicacion con tareas y operaciones en curso.

## Secuencia de implementacion aprobada
1. Revisar insumos DIAN y crear una solucion modular compilable, sin microservicios prematuros.
2. Implementar identidad, autorizacion y resolucion de tenant; probar aislamiento entre dos tenants.
3. Crear y consultar una factura borrador mediante Dapper y SQL versionado, con calculos, idempotencia y trazabilidad inicial.
4. Generar XML y validar XSD y reglas de negocio.
5. Incorporar firma y flujo de habilitacion DIAN, envio, consulta y recuperacion.
6. Completar PDF y correo; despues incorporar otros documentos.
- Validar cada incremento con pruebas enfocadas, incluyendo aislamiento, importes, concurrencia e idempotencia donde corresponda.

## Pendientes por definir o verificar
- Versiones de dependencias de integracion al implementar cada componente.
- Resolver la discrepancia XSD/anexo descrita en Dian/README.md y verificar modalidad de habilitacion DIAN por emisor.
- Framework de interfaz y modalidad de alojamiento en Firebase.
- Region GCP, dimensionamiento, disponibilidad, presupuesto y politica de recuperacion.
- Herramienta de despliegue SQL, proveedor de correo y componente de firma.
- Credenciales, certificados y recursos reales para las pruebas de integracion.
- Este archivo registra acuerdos de diseno; no acredita implementacion ni infraestructura desplegada.

## Estructura inicial
- Solucion IFC.Facturacion.sln, con proyectos Api, Application, Domain e Infrastructure en src y Tests en tests.
- Application referencia Domain; Infrastructure referencia Application; Api referencia Application e Infrastructure.
- Scripts SQL en src/IFC.Facturacion.Infrastructure/Script, separados en migrations, functions y procedures. El mecanismo de despliegue SQL sigue pendiente.
- README.md documenta comandos, alcance y estado. Actualizarlo junto con cambios de comportamiento.
- El esqueleto solo expone GET /health/live, sin comprobar dependencias externas. No confundir liveness con disponibilidad de integraciones.
- Validacion inicial completada: restore, build Release, una prueba de integracion y publish local. Docker no estaba instalado; la imagen y GCP no estan validados ni desplegados.
- Comandos base: dotnet restore IFC.Facturacion.sln; dotnet build IFC.Facturacion.sln --configuration Release --no-restore; dotnet test IFC.Facturacion.sln --configuration Release --no-build.

## Modelos XML DIAN
- Se obtuvieron los 20 XSD de la caja oficial FE 1.9, edicion 2026. Fuentes, hashes y limitaciones en src/IFC.Facturacion.Infrastructure/Dian/README.md y Schemas/V1_9/provenance.json.
- Conservar XSD y ejemplos oficiales sin modificaciones. No confundir la version UBL 2.1 con la version 1.9 del anexo DIAN.
- Clases XML de Invoice y extensiones en Dian/Xml/Generated, separadas de Domain y contratos API. No editar generados manualmente; usar tools/Generate-DianXmlModels.ps1.
- Generador versionado en tools/IFC.Facturacion.XmlGenerator, fuera de la solucion productiva. Importes decimal; valores UBL fecha/hora string para preservar formato y offset.
- Enumeraciones XML representadas como string; deserializar no equivale a validar. ExtensionContent usa XmlElement segun el wildcard original.
- El XSD DIAN enumera tipos de documento en schemeID, pero el anexo (FAB22/23 y FAB34/35) y el ejemplo usan alli el DV. Las pruebas deben reportar esa discrepancia explicitamente, no ocultarla ni parchear fuentes.
- Una validacion UBL exitosa no implica conformidad DIAN completa ni validacion criptografica. No reserializar XML firmados para reenviarlos: conservar sus bytes originales.
- Validacion de este incremento: diez pruebas aprobadas, build y publish Release sin advertencias ni errores, regeneracion identica por SHA256. Aun no hay firma, comunicacion DIAN ni validacion completa de sus reglas de negocio.

## Modelo de factura borrador
- Domain/Facturas contiene el modelo de negocio; Application/Facturas/CrearBorrador contiene los DTO, validacion y preparacion. No usar las clases XML como contrato de creacion.
- PrepararFacturaBorrador construye una instantanea en memoria, no persiste ni autentica. El contexto tenant/usuario procede del servidor autorizado, nunca del cuerpo JSON; verificar permisos y emisor al integrar la API.
- El emisor se referencia por EmisorId; su informacion fiscal se obtiene de configuracion propia del tenant, no de datos de emisor enviados por la interfaz.
- No aceptar numero fiscal, CUFE, estado, totales o importes calculados en el contrato de creacion. Se rechazan propiedades JSON desconocidas.
- No duplicar el motor de calculos SQL en C#: conservar entradas decimal y dejar Totales, ImporteNeto e importe de impuesto en null mientras no se hayan calculado.
- Los DTO admiten impuestos porcentuales o por unidad, descuentos/cargos fijos o porcentuales, pagos y anticipos. Las validaciones iniciales son estructurales, no reglas DIAN exhaustivas.
- Conservar la clave de idempotencia no equivale a deduplicar: la persistencia futura debe hacerlo atomicamente. No exponer este preparador como una creacion persistida.
- Documentacion del contrato y limites en src/IFC.Facturacion.Application/Facturas/CrearBorrador/README.md.
- Validacion del incremento: 74 pruebas nuevas; 84 en total aprobadas, compilacion y publicacion Release sin advertencias ni errores. Persistencia, autorizacion real y calculos PostgreSQL siguen pendientes.
