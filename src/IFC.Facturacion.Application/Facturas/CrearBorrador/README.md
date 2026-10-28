# Preparacion de factura borrador

Este incremento define el modelo de negocio, el contrato de entrada y su
validacion estructural. No implementa persistencia, numeracion, emision ni un
endpoint HTTP. La API continua exponiendo unicamente su comprobacion de vida.

## Responsabilidades

- [CrearFacturaBorradorSolicitud](CrearFacturaBorradorSolicitud.cs): datos
  editables enviados por el cliente, independientes de las clases XML UBL.
- [ContextoCreacionFactura](ContextoCreacionFactura.cs): tenant, usuario y clave
  de idempotencia proporcionados por el servidor, fuera del cuerpo JSON.
- [ValidadorFacturaBorrador](ValidadorFacturaBorrador.cs): devuelve errores con
  campo, codigo y mensaje, incluidos errores en colecciones anidadas.
- [PrepararFacturaBorrador](PrepararFacturaBorrador.cs): valida y construye una
  instantanea de negocio con ID interno, fecha UTC del servidor y estado Borrador.
- [FacturaBorrador](../../../IFC.Facturacion.Domain/Facturas/FacturaBorrador.cs):
  modelo independiente de HTTP, XML, Firebase y PostgreSQL.

El emisor se referencia mediante `EmisorId`; no se reciben datos fiscales del
emisor desde la interfaz. Su configuracion y pertenencia al tenant deben
consultarse y verificarse en el futuro caso de uso de persistencia.

**El contexto no es un mecanismo de autenticacion ni autorizacion.** Antes de
invocar la preparacion desde una API real se deben validar el token Firebase,
la membresia, los permisos y el acceso al emisor. Todavia no estan implementados.

## Contrato de entrada

Ejemplo de borrador, con datos ficticios; no es una factura lista para emitir:

```json
{
  "emisorId": "ff6653f8-c959-4f85-99b0-1c9a1d47f463",
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
anidados. No admiten tenant, usuario, ID interno, estado, numero fiscal, CUFE,
totales ni importes calculados como parte de la solicitud.

## Reglas iniciales

- Se requiere emisor no vacio, fecha/hora de emision, moneda, tipo de operacion,
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

Conservar la clave de idempotencia **no implementa deduplicacion**. Cada llamada
de preparacion produce un candidato con un ID interno nuevo. El futuro SP de
registro debera resolver atomicamente las repeticiones y los conflictos de
contenido dentro del tenant y emisor correspondiente.

Siguientes integraciones: autorizacion Firebase/Firestore, consulta de emisor,
tablas y SP por tenant, Dapper/Npgsql y calculos autoritativos PostgreSQL.
Posteriormente se mapeara el documento de negocio validado a las clases XML.

## Pruebas

```powershell
dotnet test .\tests\IFC.Facturacion.Tests --configuration Release --filter FullyQualifiedName~Application.Facturas
```

Cubren contratos JSON, campos controlados por servidor, datos anidados nulos,
errores por campo, precision decimal, offset, vencimiento, copia de colecciones
y propagacion del contexto. No prueban autenticacion real, aislamiento entre
bases, idempotencia persistente ni validacion DIAN completa.
