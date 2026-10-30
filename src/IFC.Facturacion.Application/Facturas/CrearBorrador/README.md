# Preparacion de factura borrador

Este incremento define el modelo de negocio, el contrato de entrada, su
validacion estructural y los casos de uso/repositorio para persistir borradores
mediante SP. No implementa numeracion, emision ni endpoints HTTP. La API
continua exponiendo unicamente su comprobacion de vida.

## Responsabilidades

- [CrearFacturaBorradorSolicitud](CrearFacturaBorradorSolicitud.cs): datos
  editables enviados por el cliente, independientes de las clases XML UBL.
- [ContextoCreacionFactura](ContextoCreacionFactura.cs): tenant, emisor, usuario
  y clave de idempotencia proporcionados por el servidor, fuera del cuerpo JSON.
- [ValidadorFacturaBorrador](ValidadorFacturaBorrador.cs): devuelve errores con
  campo, codigo y mensaje, incluidos errores en colecciones anidadas.
- [PrepararFacturaBorrador](PrepararFacturaBorrador.cs): valida y construye una
  instantanea de negocio con ID interno, fecha UTC del servidor y estado Borrador.
- [FacturaBorrador](../../../IFC.Facturacion.Domain/Facturas/FacturaBorrador.cs):
  modelo independiente de HTTP, XML, Firebase y PostgreSQL.

Tenant y emisor corresponden uno a uno en el modelo actual. El `EmisorId`
procede del contexto autorizado del servidor; no se recibe en el cuerpo JSON ni
se puede elegir otro emisor desde la interfaz.

**El contexto no es un mecanismo de autenticacion ni autorizacion.** Antes de
invocar la preparacion desde una API real se deben validar el token Firebase,
la membresia, los permisos y el acceso al emisor. Todavia no estan implementados.

## Contrato de entrada

Ejemplo de borrador, con datos ficticios; no es una factura lista para emitir:

```json
{
  "fechaHoraEmision": "2026-10-27T21:00:00-05:00",
  "tipoOperacionCodigo": "10",
  "monedaCodigo": "COP",
  "adquirente": {
    "nombreRazonSocial": "Adquirente de prueba",
    "tipoOrganizacionCodigo": "2",
    "identificacion": {
      "tipoDocumentoCodigo": "13",
      "numero": "123456789"
    },
    "responsabilidadesFiscales": ["R-99-PN"],
    "correoElectronico": "adquirente@example.com"
  },
  "condicionesPago": {
    "formaPagoCodigo": "1",
    "medios": [{"codigo": "42", "referencia": "transferencia-prueba"}]
  },
  "detalles": [
    {
      "numero": 1,
      "codigoProducto": "PRODUCTO-01",
      "descripcion": "Producto de prueba",
      "cantidad": 2.5,
      "unidadMedidaCodigo": "94",
      "precioUnitario": 100.123456,
      "cantidadBasePrecio": 1,
      "impuestos": [{"tributoCodigo": "01", "tarifaPorcentaje": 19}],
      "ajustes": []
    }
  ],
  "ajustes": [],
  "anticipos": [],
  "notas": ["Factura borrador de prueba"],
  "referenciaExterna": "pedido-100"
}
```

Los contratos rechazan propiedades JSON desconocidas, tambien en objetos
anidados. No admiten tenant, emisor, usuario, ID interno, estado, numero fiscal,
CUFE, totales ni importes calculados como parte de la solicitud.

## Reglas iniciales

- El contexto autorizado del servidor requiere tenant/emisor y usuario; la
  solicitud requiere fecha/hora de emision, moneda, tipo de operacion,
  adquirente, condiciones de pago y al menos un detalle.
- Los numeros de detalle deben ser positivos y no repetirse.
- Cantidad y cantidad base del precio deben ser positivas; precio no negativo.
  `CantidadBasePrecio` vale 1 si se omite; un `null` explicito es invalido.
- Importes y tarifas se conservan como `decimal`, sin redondear ni calcular
  impuestos en C#. Fecha/hora de negocio usa `DateTimeOffset` y conserva el
  offset proporcionado.
- Los impuestos pueden declarar tarifa porcentual o valor por unidad, nunca
  ambos. Un 19 por ciento se expresa como `19`, no `0.19`.
  La modalidad por unidad requiere cantidad base positiva y codigo de unidad.
- La base gravable opcional es un dato candidato, no una base ya validada
  fiscalmente. Su derivacion o comprobacion queda para la logica PostgreSQL.
- Cada descuento/cargo usa importe fijo o porcentaje, de manera excluyente.
  El porcentaje requiere base de calculo. `EsCargo=false` representa descuento.
- Pago: `1` contado o `2` credito; se requiere al menos un medio de pago.
  Credito requiere vencimiento no anterior a la fecha local de emision.
- Un NIT (`TipoDocumentoCodigo=31`) requiere DV de un digito. No se calcula ni
  verifica matematicamente su correspondencia en este incremento.
- Las colecciones opcionales omitidas son vacias. Colecciones o elementos
  explicitamente `null` se reportan como errores, no se sustituyen.
- Los errores se consultan con `Validar`, o se reciben mediante
  `FacturaBorradorInvalidaException` al preparar una solicitud invalida.

Los codigos se comprueban estructuralmente, no contra todos los catalogos DIAN.
Direccion, informacion tributaria y precio de referencia pueden estar
incompletos en un borrador; deben completarse segun las reglas aplicables antes
de emitir. Un precio cero no significa automaticamente que el documento
cumpla las reglas de operaciones gratuitas.

## Resultado y limites

La preparacion copia las colecciones para no compartir listas mutables de la
solicitud. No asigna numero fiscal ni CUFE.

`Totales`, `DetalleFactura.ImporteNeto` e `ImpuestoFactura.Importe` quedan
pendientes (`null`), no se presentan ceros como calculos realizados. El modelo
`TotalesFactura` define los valores que devolvera la logica de base de datos.

`CrearFacturaBorradorCasoUso` prepara y delega la persistencia en
`IFacturaBorradorRepositorio`; `ObtenerFacturaBorradorCasoUso` limita la consulta
al tenant autorizado. Los SP `USP_CrearFacturaBorrador` y
`USP_ObtenerFacturaBorrador` guardan/consultan la instantanea JSONB. El SP de
creacion resuelve atomicamente las repeticiones por tenant: contenido igual
devuelve el registro previo y contenido distinto produce conflicto. El
preparador, por si solo, sigue creando una instantanea nueva y no deduplica.

Los SP dejan `Totales`, `ImporteNeto` e importe de impuesto en null. Las reglas
de calculo y redondeo siguen pendientes; no se presentan importes calculados
hasta definirlas y probarlas en PostgreSQL.

Siguientes integraciones: autenticacion/autorizacion Firebase/Firestore y
resolucion segura de la conexion PostgreSQL por tenant. No exponer los casos de
uso como endpoints hasta completar ese acceso. Despues deben definirse los
calculos fiscales y su redondeo en PostgreSQL; posteriormente se mapeara el
documento persistido a las clases XML.

## Pruebas

```powershell
dotnet test .\tests\IFC.Facturacion.Tests --configuration Release --filter FullyQualifiedName~Application.Facturas
```

Cubren contratos JSON, campos controlados por servidor, datos anidados nulos,
errores por campo, precision decimal, offset, vencimiento, copia de colecciones
y propagacion del contexto. Las pruebas del caso de uso cubren el contexto
autorizado y la delegacion al repositorio. Aun no hay pruebas de integracion
contra PostgreSQL; autenticacion real, aislamiento entre bases y validacion DIAN
completa siguen pendientes.
