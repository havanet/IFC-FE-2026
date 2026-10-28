using IFC.Facturacion.Application.Facturas.CrearBorrador;

namespace IFC.Facturacion.Tests.Application.Facturas;

internal static class FacturaBorradorTestData
{
    public static ContextoCreacionFactura Contexto => new("tenant-pruebas", "usuario-pruebas", "orden-100");

    public static CrearFacturaBorradorSolicitud Solicitud() => new()
    {
        EmisorId = Guid.Parse("ff6653f8-c959-4f85-99b0-1c9a1d47f463"),
        FechaHoraEmision = new DateTimeOffset(2026, 10, 27, 21, 0, 0, TimeSpan.FromHours(-5)),
        TipoOperacionCodigo = "10",
        MonedaCodigo = "COP",
        Adquirente = new AdquirenteFacturaSolicitud
        {
            NombreRazonSocial = "Adquirente de prueba",
            TipoOrganizacionCodigo = "1",
            Identificacion = new IdentificacionFiscalSolicitud
            {
                TipoDocumentoCodigo = "31",
                Numero = "800197268",
                DigitoVerificacion = "4"
            },
            ResponsabilidadesFiscales = ["R-99-PN"],
            TributoCodigo = "01",
            CorreoElectronico = "adquirente@example.com",
            Telefono = "3000000000",
            Direccion = new DireccionFiscalSolicitud
            {
                PaisCodigo = "CO",
                Linea = "Direccion de prueba",
                DepartamentoCodigo = "11",
                DepartamentoNombre = "Bogota D.C.",
                MunicipioCodigo = "11001",
                MunicipioNombre = "Bogota D.C.",
                CodigoPostal = "110111"
            }
        },
        CondicionesPago = new CondicionesPagoFacturaSolicitud
        {
            FormaPagoCodigo = "1",
            Medios = [new MedioPagoFacturaSolicitud { Codigo = "42", Referencia = "transferencia-prueba" }]
        },
        Detalles = [Detalle()],
        Ajustes = [],
        Anticipos = [],
        Notas = ["Factura borrador de prueba"],
        ReferenciaExterna = "pedido-100"
    };

    public static DetalleFacturaSolicitud Detalle() => new()
    {
        Numero = 1,
        CodigoProducto = "PRODUCTO-01",
        Descripcion = "Producto de prueba",
        Cantidad = 2.5m,
        UnidadMedidaCodigo = "94",
        PrecioUnitario = 100.123456m,
        Impuestos =
        [
            new ImpuestoFacturaSolicitud { TributoCodigo = "01", TarifaPorcentaje = 19m }
        ],
        Ajustes = []
    };
}
