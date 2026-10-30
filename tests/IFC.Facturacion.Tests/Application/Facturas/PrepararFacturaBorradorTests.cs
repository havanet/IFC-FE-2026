using IFC.Facturacion.Application.Facturas.CrearBorrador;
using IFC.Facturacion.Domain.Facturas;

namespace IFC.Facturacion.Tests.Application.Facturas;

public sealed class PrepararFacturaBorradorTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 28, 2, 30, 0, TimeSpan.Zero);
    private readonly PrepararFacturaBorrador preparador = new(new RelojFijo(Ahora));

    [Fact]
    public void Preparar_MapsBusinessDataAndServerOwnedMetadata()
    {
        var solicitud = FacturaBorradorTestData.Solicitud();
        var contexto = FacturaBorradorTestData.Contexto;

        var factura = preparador.Preparar(solicitud, contexto);

        Assert.NotEqual(Guid.Empty, factura.Id);
        Assert.Equal(contexto.TenantId, factura.TenantId);
        Assert.Equal(contexto.UsuarioId, factura.CreadaPor);
        Assert.Equal(contexto.ClaveIdempotencia, factura.ClaveIdempotencia);
        Assert.Equal(Ahora, factura.CreadaEnUtc);
        Assert.Equal(contexto.EmisorId, factura.EmisorId);
        Assert.Equal(solicitud.FechaHoraEmision, factura.FechaHoraEmision);
        Assert.Equal(TimeSpan.FromHours(-5), factura.FechaHoraEmision.Offset);
        Assert.Equal("10", factura.TipoOperacionCodigo);
        Assert.Equal("COP", factura.MonedaCodigo);
        Assert.Equal(EstadoFactura.Borrador, factura.Estado);
        Assert.Null(factura.Totales);
        Assert.Equal("pedido-100", factura.ReferenciaExterna);
        Assert.Equal("Factura borrador de prueba", Assert.Single(factura.Notas));

        var adquirente = factura.Adquirente;
        Assert.Equal("Adquirente de prueba", adquirente.NombreRazonSocial);
        Assert.Equal("1", adquirente.TipoOrganizacionCodigo);
        Assert.Equal("31", adquirente.Identificacion.TipoDocumentoCodigo);
        Assert.Equal("800197268", adquirente.Identificacion.Numero);
        Assert.Equal("4", adquirente.Identificacion.DigitoVerificacion);
        Assert.Equal("R-99-PN", Assert.Single(adquirente.ResponsabilidadesFiscales));
        Assert.Equal("01", adquirente.TributoCodigo);
        Assert.Equal("adquirente@example.com", adquirente.CorreoElectronico);
        Assert.Equal("3000000000", adquirente.Telefono);
        Assert.NotNull(adquirente.Direccion);
        Assert.Equal("CO", adquirente.Direccion.PaisCodigo);
        Assert.Equal("Direccion de prueba", adquirente.Direccion.Linea);
        Assert.Equal("11", adquirente.Direccion.DepartamentoCodigo);
        Assert.Equal("Bogota D.C.", adquirente.Direccion.DepartamentoNombre);
        Assert.Equal("11001", adquirente.Direccion.MunicipioCodigo);
        Assert.Equal("Bogota D.C.", adquirente.Direccion.MunicipioNombre);
        Assert.Equal("110111", adquirente.Direccion.CodigoPostal);

        var detalle = Assert.Single(factura.Detalles);
        Assert.Equal(1, detalle.Numero);
        Assert.Equal("PRODUCTO-01", detalle.CodigoProducto);
        Assert.Equal("Producto de prueba", detalle.Descripcion);
        Assert.Equal(2.5m, detalle.Cantidad);
        Assert.Equal(100.123456m, detalle.PrecioUnitario);
        Assert.Equal(1m, detalle.CantidadBasePrecio);
        Assert.Equal("94", detalle.UnidadMedidaCodigo);
        Assert.Null(detalle.ImporteNeto);
        var impuesto = Assert.Single(detalle.Impuestos);
        Assert.Equal("01", impuesto.TributoCodigo);
        Assert.Equal(19m, impuesto.TarifaPorcentaje);
        Assert.False(impuesto.EsRetencion);
        Assert.Null(impuesto.Importe);

        Assert.Equal(FormaPagoFactura.Contado, factura.CondicionesPago.Forma);
        var medio = Assert.Single(factura.CondicionesPago.Medios);
        Assert.Equal("42", medio.Codigo);
        Assert.Equal("transferencia-prueba", medio.Referencia);
    }

    [Fact]
    public void Preparar_MapsUnitTaxAdjustmentsAndPrepaymentWithoutCalculatingTotals()
    {
        var fijo = new AjusteFacturaSolicitud { EsCargo = false, Motivo = "Descuento fijo", Importe = 5m };
        var porcentual = new AjusteFacturaSolicitud
        {
            EsCargo = true, Motivo = "Cargo", MotivoCodigo = "01", Porcentaje = 2m, BaseCalculo = 100m
        };
        var solicitud = FacturaBorradorTestData.Solicitud() with
        {
            Detalles =
            [
                FacturaBorradorTestData.Detalle() with
                {
                    PrecioUnitario = 0m,
                    PrecioReferencia = 100m,
                    TipoPrecioReferenciaCodigo = "01",
                    Ajustes = [fijo],
                    Impuestos =
                    [
                        new ImpuestoFacturaSolicitud
                        {
                            TributoCodigo = "02", EsRetencion = true, BaseGravable = 100m,
                            ValorPorUnidad = 20m, CantidadBase = 1m, UnidadMedidaCodigo = "LTR"
                        }
                    ]
                }
            ],
            Ajustes = [porcentual],
            Anticipos =
            [
                new AnticipoFacturaSolicitud
                {
                    Referencia = "anticipo-1", Importe = 10.123456789m, FechaPago = new DateOnly(2026, 10, 1)
                }
            ]
        };

        var factura = preparador.Preparar(solicitud, FacturaBorradorTestData.Contexto);

        var detalle = Assert.Single(factura.Detalles);
        Assert.Equal(0m, detalle.PrecioUnitario);
        Assert.Equal(100m, detalle.PrecioReferencia);
        Assert.Equal("01", detalle.TipoPrecioReferenciaCodigo);
        var impuesto = Assert.Single(detalle.Impuestos);
        Assert.True(impuesto.EsRetencion);
        Assert.Equal(100m, impuesto.BaseGravable);
        Assert.Equal(20m, impuesto.ValorPorUnidad);
        Assert.Equal(1m, impuesto.CantidadBase);
        Assert.Equal("LTR", impuesto.UnidadMedidaCodigo);
        Assert.Null(impuesto.TarifaPorcentaje);
        Assert.Equal(5m, Assert.Single(detalle.Ajustes).Importe);
        var ajuste = Assert.Single(factura.Ajustes);
        Assert.True(ajuste.EsCargo);
        Assert.Equal("01", ajuste.MotivoCodigo);
        Assert.Equal(2m, ajuste.Porcentaje);
        Assert.Equal(100m, ajuste.BaseCalculo);
        Assert.Null(ajuste.Importe);
        Assert.Equal(10.123456789m, Assert.Single(factura.Anticipos).Importe);
        Assert.Equal(new DateOnly(2026, 10, 1), factura.Anticipos[0].FechaPago);
        Assert.Null(factura.Totales);
    }

    [Fact]
    public void Preparar_CopiesCollectionsInsteadOfKeepingMutableRequestLists()
    {
        var impuestos = new List<ImpuestoFacturaSolicitud?>
        {
            new() { TributoCodigo = "01", TarifaPorcentaje = 19m }
        };
        var detalles = new List<DetalleFacturaSolicitud?>
        {
            FacturaBorradorTestData.Detalle() with { Impuestos = impuestos }
        };
        var notas = new List<string?> { "Original" };
        var solicitud = FacturaBorradorTestData.Solicitud() with { Detalles = detalles, Notas = notas };

        var factura = preparador.Preparar(solicitud, FacturaBorradorTestData.Contexto);
        impuestos.Clear();
        detalles.Clear();
        notas.Clear();

        Assert.Single(factura.Detalles);
        Assert.Single(factura.Detalles[0].Impuestos);
        Assert.Equal("Original", Assert.Single(factura.Notas));
        Assert.Throws<NotSupportedException>(() =>
            ((IList<DetalleFactura>)factura.Detalles).Clear());
    }

    [Fact]
    public void Preparar_DoesNotReuseTenantOrActorBetweenOperations()
    {
        var solicitud = FacturaBorradorTestData.Solicitud();
        var emisor = Guid.Parse("ff6653f8-c959-4f85-99b0-1c9a1d47f463");
        var otroEmisor = Guid.Parse("36bc99bf-7bf1-4aae-a69d-cc8b6a2734ec");
        var primera = preparador.Preparar(
            solicitud, new ContextoCreacionFactura("tenant-a", emisor, "actor-a", "clave-a"));
        var segunda = preparador.Preparar(
            solicitud, new ContextoCreacionFactura("tenant-b", otroEmisor, "actor-b", "clave-b"));

        Assert.Equal("tenant-a", primera.TenantId);
        Assert.Equal("actor-a", primera.CreadaPor);
        Assert.Equal("tenant-b", segunda.TenantId);
        Assert.Equal("actor-b", segunda.CreadaPor);
        Assert.NotEqual(primera.Id, segunda.Id);
    }

    [Fact]
    public void Preparar_InvalidRequestThrowsStructuredErrorsInsteadOfReturningDraft()
    {
        var solicitud = FacturaBorradorTestData.Solicitud() with { MonedaCodigo = "cop", Detalles = [] };

        var exception = Assert.Throws<FacturaBorradorInvalidaException>(() =>
            preparador.Preparar(solicitud, FacturaBorradorTestData.Contexto));

        Assert.Contains(exception.Errores, error => error.Campo == "MonedaCodigo");
        Assert.Contains(exception.Errores, error => error.Campo == "Detalles");
        Assert.All(exception.Errores, error => Assert.False(string.IsNullOrWhiteSpace(error.Mensaje)));
    }

    [Fact]
    public void Preparar_PreservesDecimalInputsWithoutRounding()
    {
        const decimal precio = 123456789012345.123456789m;
        const decimal cantidad = 0.000000001m;
        var solicitud = FacturaBorradorTestData.Solicitud() with
        {
            Detalles = [FacturaBorradorTestData.Detalle() with { PrecioUnitario = precio, Cantidad = cantidad }]
        };

        var factura = preparador.Preparar(solicitud, FacturaBorradorTestData.Contexto);

        Assert.Equal(precio, factura.Detalles[0].PrecioUnitario);
        Assert.Equal(cantidad, factura.Detalles[0].Cantidad);
        Assert.Null(factura.Detalles[0].ImporteNeto);
    }

    private sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
