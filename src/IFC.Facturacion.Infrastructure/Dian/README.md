# Modelos XML de factura DIAN

## Fuentes oficiales

Descargadas el 27 de octubre de 2026 desde la
[documentacion tecnica DIAN](https://micrositios.dian.gov.co/sistema-de-facturacion-electronica/documentacion-tecnica/):

- [Anexo tecnico de factura electronica de venta 1.9](https://www.dian.gov.co/impuestos/factura-electronica/Documents/Anexo-Tecnico-Factura-Electronica-de-Venta-vr-1-9.pdf).
- [Caja de herramientas FE 1.9, edicion 2026](https://www.dian.gov.co/impuestos/factura-electronica/Documents/Caja-de-herramientas-FE_V19_v2026.zip).

Los 20 XSD de `XSD/` se conservan sin cambios en `Schemas/V1_9`, incluidos
los avisos originales de licencia y copyright. El manifiesto
[provenance.json](Schemas/V1_9/provenance.json) registra URLs y SHA256.
El ZIP y el PDF completos no se incluyen en el repositorio.

El ejemplo oficial `Ejemplificaciones/XMLs de ejemplo/Generica.xml` se conserva
sin cambios en el proyecto de pruebas. Es un ejemplo publicado con fecha de
emision de 2019, no evidencia de aceptacion actual por la DIAN.

## Clases y regeneracion

`Xml/Generated` contiene clases C# para `Invoice` y las extensiones DIAN, con
sus componentes comunes UBL 2.1, XMLDSig y XAdES. El tipo raiz es `InvoiceType`.
Los otros documentos se conservan como XSD, pero sus clases raiz y flujos
no se generan todavia.

Son modelos de intercambio XML de Infraestructura, no entidades de Domain
ni contratos publicos de la API. No editar las clases generadas manualmente.

Desde la raiz del proyecto:

```powershell
.\tools\Generate-DianXmlModels.ps1
dotnet test .\tests\IFC.Facturacion.Tests --configuration Release --filter FullyQualifiedName~InvoiceXmlTests
```

El generador es un proyecto de desarrollo independiente de la solucion y del
contenedor productivo. Usa `XmlSchemaClassGenerator-beta` version `3.0.1430`
fijada en su manifiesto. El script restaura y ejecuta ese proyecto.

Configuracion relevante:

- Namespaces explicitos y orden de elementos obtenido del XSD.
- Importes XML como `decimal`.
- Los valores UBL de fecha, hora y fecha/hora se representan como `string`
  para conservar su forma XML, incluido el offset. No usar `DateTime` para
  normalizarlos implicitamente antes de calcular CUFE o firmar.
- Enumeraciones representadas como `string`. La deserializacion no valida
  restricciones del XSD; la validacion debe ser un paso separado.
- Los contenidos abiertos de `ExtensionContent` se conservan como `XmlElement`.
  La extension DIAN puede deserializarse aparte con `DianExtensionsType`.

Los cambios de mapeo se realizan mediante el visitante CodeDOM del generador,
sin alterar los XSD ni reescribir texto de las clases generadas.

## Discrepancia oficial que no debe ocultarse

En `DIAN_UBL_Structures.xsd`, `coID2Type/@schemeID` enumera tipos de documento
como `11`, `13` y `31`. Sin embargo, el anexo 1.9 indica:

- Pagina 25, FAB22/FAB23: `ProviderID/@schemeID` contiene el DV del NIT;
  `@schemeName` contiene `31`.
- Pagina 27, FAB34/FAB35: `AuthorizationProviderID/@schemeID` contiene el DV
  de DIAN, `4`; `@schemeName` contiene `31`.

El ejemplo oficial usa `schemeID="4"` y `schemeName="31"` en ambos casos.
Por ello pasa la validacion estructural UBL, pero al incluir el XSD de
extensiones DIAN se reportan dos errores de enumeracion.

Las pruebas registran explicitamente esa discrepancia. No se modifico el
ejemplo, no se parcheo el XSD y no se ocultan los errores de validacion.
La representacion como string permite conservar el valor del anexo y del
ejemplo sin hacer que una enumeracion C# lo rechace antes de validarlo.

Antes de habilitar la emision real se debe definir y verificar el tratamiento
de esta discrepancia contra el anexo y el ambiente de habilitacion DIAN.
No considerar la validacion UBL sola como validacion DIAN completa.

## Alcance de las pruebas

- Integridad SHA256 de los XSD y del ejemplo oficial.
- Compilacion de esquemas y resolucion local de dependencias.
- Validacion UBL del ejemplo y rechazo de ID ausente o importe no numerico.
- Lectura y escritura de factura y extension DIAN.
- Preservacion de fecha, hora, offset, importes decimales y extensiones.
- Deteccion explicita de la discrepancia del XSD DIAN.

El esquema XMLDSig se precarga ignorando su DTD interno, sin resolver entidades
externas. No se descargan esquemas durante las pruebas.

Estas pruebas no verifican CUFE, reglas tributarias completas, certificados,
validez criptografica de firmas ni aceptacion por la DIAN.
Deserializar y volver a serializar un XML firmado puede invalidar su firma.
La prueba de round-trip es estructural, no un mecanismo para modificar o
reenviar facturas firmadas. Los bytes originales firmados deben conservarse.
