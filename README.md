# IFC - Facturacion electronica DIAN

Base de la plataforma multiempresa en ASP.NET Core / .NET 10 LTS para Cloud Run.
Primero se implementara la factura electronica y despues los demas documentos.

## Estructura

```text
.github/
  copilot-instructions.md
src/
  IFC.Facturacion.Api/
  IFC.Facturacion.Application/
    Facturas/
      CrearBorrador/
  IFC.Facturacion.Domain/
    Facturas/
  IFC.Facturacion.Infrastructure/
    Facturas/
    Dian/
      Schemas/
        V1_9/
      Xml/
        Generated/
    Script/
      migrations/
      functions/
      procedures/
tests/
  IFC.Facturacion.Tests/
    Api/
    Application/
      Facturas/
    Dian/
    Fixtures/
tools/
  Generate-DianXmlModels.ps1
  IFC.Facturacion.XmlGenerator/
Dockerfile
global.json
IFC.Facturacion.sln
```

| Proyecto | Responsabilidad |
| --- | --- |
| Api | HTTP, autenticacion, autorizacion y composicion de servicios |
| Application | Casos de uso y contratos de integracion |
| Domain | Modelos y reglas independientes de infraestructura |
| Infrastructure | Dapper/Npgsql, Firebase, DIAN, firma, almacenamiento y correo |
| Tests | Pruebas unitarias y de integracion |

Referencias: Application depende de Domain; Infrastructure depende de Application;
Api depende de Application e Infrastructure; Tests referencia los proyectos
que prueba directamente.

## Decisiones principales

- Sin Entity Framework. Dapper/Npgsql invoca Stored Procedures PostgreSQL; el codigo .NET no contiene SQL de negocio. Scripts SQL versionados.
- Una base PostgreSQL por tenant/cliente; para el modelo actual tenant y emisor corresponden uno a uno. Varias bases pueden compartir instancia Cloud SQL.
- Los Stored Procedures del proyecto usan el prefijo `USP_`.
- Firebase Authentication identifica al usuario; la API verifica sus permisos.
- Firestore mantiene membresias y enrutamiento privado. El navegador no recibe
  credenciales ni se conecta a PostgreSQL.
- Secret Manager para secretos y certificados; Cloud Storage para artefactos.
- Los modelos de API, negocio y XML DIAN se mantendran separados.

Las reglas completas y los pendientes estan en
[las instrucciones del proyecto](.github/copilot-instructions.md).
La organizacion SQL se describe en
[Script/README.md](src/IFC.Facturacion.Infrastructure/Script/README.md).
Cada procedimiento almacenado tiene su propio archivo `.sql` con su definicion completa.

## Desarrollo local

Requiere el SDK .NET 10 indicado en `global.json`, o un parche posterior de
la misma banda de SDK.

```powershell
dotnet restore .\IFC.Facturacion.sln
dotnet build .\IFC.Facturacion.sln --configuration Release --no-restore
dotnet test .\IFC.Facturacion.sln --configuration Release --no-build
dotnet run --project .\src\IFC.Facturacion.Api --launch-profile http
```

El perfil HTTP local escucha en `http://localhost:5199`.
`GET /health/live` devuelve `200` y `Healthy` si la aplicacion esta en ejecucion.
Es una comprobacion de vida del proceso, no de PostgreSQL, Firebase o DIAN.
No hay endpoints de negocio publicados.

## Contenedor para Cloud Run

Desde la raiz, con Docker instalado:

```powershell
docker build --tag ifc-facturacion .
docker run --rm --publish 8080:8080 ifc-facturacion
```

La imagen ejecuta la API como usuario no root y escucha en el puerto 8080.
Configurar ese puerto en Cloud Run. No incluye secretos ni credenciales.
La construccion y el despliegue del contenedor requieren validacion en el
entorno de destino; crear el Dockerfile no despliega recursos GCP.

## Estado actual

Existe el esqueleto de la solucion, el endpoint de vida y sus pruebas.
Infraestructura incluye los XSD oficiales FE 1.9, edicion 2026, las clases
XML de factura generadas y pruebas de serializacion y validacion estructural.
Ver [modelos XML DIAN](src/IFC.Facturacion.Infrastructure/Dian/README.md)
para fuentes, regeneracion y la discrepancia detectada entre el XSD de
extensiones DIAN y el anexo/ejemplo oficial. No implica validacion DIAN completa.
Domain incluye el modelo de factura borrador, adquirente, detalles, impuestos,
ajustes, pagos, anticipos y totales. Application incluye el contrato JSON,
validacion estructural y preparacion de una instantanea de negocio.
Ver [contrato de factura borrador](src/IFC.Facturacion.Application/Facturas/CrearBorrador/README.md).
La preparacion no calcula totales ni asigna numeracion fiscal. Se agregaron los
casos de uso, el codigo de repositorio Dapper y los scripts SP para persistir y
consultar borradores JSONB con idempotencia. Aun no se ha conectado a una base:
el proveedor real de conexiones por tenant y el despliegue automatizado de
scripts todavia no estan implementados. Los totales permanecen pendientes; no
se inventan redondeos ni reglas fiscales.
No se han implementado ni configurado Firebase, autorizacion real, certificados,
firma, comunicacion con DIAN, PDF, correo ni tareas. La API no expone endpoints
de negocio hasta contar con autenticacion, autorizacion y resolucion segura del
tenant.

Validacion actual: 87 pruebas aprobadas; build Release sin advertencias ni
errores. La integracion de los SP contra una instancia PostgreSQL no se pudo
ejecutar porque no hay cliente/servidor PostgreSQL configurado en el entorno.
Regeneracion de clases XML verificada como reproducible; integridad de los XSD
comprobada con SHA256.
No se construyo la imagen porque Docker no esta instalado en el entorno inicial.
No se han desplegado recursos GCP.

Queda implementar la autenticacion/autorizacion Firebase y el enrutamiento
Firestore hacia la base del tenant antes de exponer la persistencia. Tambien
deben definirse y probarse en PostgreSQL las reglas de calculo y redondeo antes
de asignar totales o habilitar la emision.
