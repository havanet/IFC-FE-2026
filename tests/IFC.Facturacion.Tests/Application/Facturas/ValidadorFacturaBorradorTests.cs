using IFC.Facturacion.Application.Facturas.CrearBorrador;

namespace IFC.Facturacion.Tests.Application.Facturas;

public sealed class ValidadorFacturaBorradorTests
{
    [Fact]
    public void Validar_ValidRequestHasNoErrors()
    {
        Assert.Empty(Validar(FacturaBorradorTestData.Solicitud()));
    }

    [Fact]
    public void Validar_NullRequestAndContextAreReported()
    {
        var errores = ValidadorFacturaBorrador.Validar(null, null);

        Assert.Contains(errores, error => error.Campo == "Factura" && error.Codigo == "requerido");
        Assert.Contains(errores, error => error.Campo == "Contexto" && error.Codigo == "requerido");
    }

    [Fact]
    public void Validar_EmptyRequestReportsAllRequiredGroups()
    {
        var errores = Validar(new CrearFacturaBorradorSolicitud());

        foreach (var campo in new[]
                 {
                     "EmisorId", "FechaHoraEmision", "MonedaCodigo", "TipoOperacionCodigo",
                     "Adquirente", "CondicionesPago", "Detalles"
                 })
        {
            Assert.Contains(errores, error => error.Campo == campo);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("cop")]
    [InlineData("CO")]
    [InlineData("CO1")]
    [InlineData("\uFF23\uFF2F\uFF30")]
    public void Validar_InvalidCurrencyIsRejected(string? moneda)
    {
        var errores = Validar(FacturaBorradorTestData.Solicitud() with { MonedaCodigo = moneda });

        Assert.Contains(errores, error => error.Campo == "MonedaCodigo");
    }

    [Fact]
    public void Validar_EmptyContextFieldsAreRejected()
    {
        var errores = ValidadorFacturaBorrador.Validar(
            FacturaBorradorTestData.Solicitud(), new ContextoCreacionFactura("", " ", ""));

        Assert.Equal(3, errores.Count);
        Assert.All(errores, error => Assert.StartsWith("Contexto.", error.Campo));
    }

    [Theory]
    [InlineData("cantidad-ausente", "Cantidad")]
    [InlineData("cantidad-cero", "Cantidad")]
    [InlineData("cantidad-negativa", "Cantidad")]
    [InlineData("precio-ausente", "PrecioUnitario")]
    [InlineData("precio-negativo", "PrecioUnitario")]
    [InlineData("base-ausente", "CantidadBasePrecio")]
    [InlineData("base-cero", "CantidadBasePrecio")]
    [InlineData("numero-cero", "Numero")]
    [InlineData("descripcion-vacia", "Descripcion")]
    [InlineData("referencia-incompleta", "PrecioReferencia")]
    public void Validar_InvalidLineReportsExactField(string caso, string campo)
    {
        var detalle = FacturaBorradorTestData.Detalle();
        detalle = caso switch
        {
            "cantidad-ausente" => detalle with { Cantidad = null },
            "cantidad-cero" => detalle with { Cantidad = 0m },
            "cantidad-negativa" => detalle with { Cantidad = -1m },
            "precio-ausente" => detalle with { PrecioUnitario = null },
            "precio-negativo" => detalle with { PrecioUnitario = -1m },
            "base-ausente" => detalle with { CantidadBasePrecio = null },
            "base-cero" => detalle with { CantidadBasePrecio = 0m },
            "numero-cero" => detalle with { Numero = 0 },
            "descripcion-vacia" => detalle with { Descripcion = " " },
            "referencia-incompleta" => detalle with { PrecioReferencia = 100m },
            _ => throw new ArgumentOutOfRangeException(nameof(caso))
        };

        var errores = Validar(FacturaBorradorTestData.Solicitud() with { Detalles = [detalle] });

        Assert.Contains(errores, error => error.Campo == $"Detalles[0].{campo}");
    }

    [Theory]
    [InlineData("detalles-null", "Detalles")]
    [InlineData("detalle-null", "Detalles[0]")]
    [InlineData("ajustes-null", "Ajustes")]
    [InlineData("ajuste-null", "Ajustes[0]")]
    [InlineData("anticipos-null", "Anticipos")]
    [InlineData("anticipo-null", "Anticipos[0]")]
    [InlineData("notas-null", "Notas")]
    [InlineData("nota-null", "Notas[0]")]
    [InlineData("impuestos-null", "Detalles[0].Impuestos")]
    [InlineData("impuesto-null", "Detalles[0].Impuestos[0]")]
    [InlineData("ajustes-detalle-null", "Detalles[0].Ajustes")]
    [InlineData("medios-null", "CondicionesPago.Medios")]
    [InlineData("medio-null", "CondicionesPago.Medios[0]")]
    [InlineData("responsabilidades-null", "Adquirente.ResponsabilidadesFiscales")]
    public void Validar_NullCollectionsAndElementsAreNotSilentlyDefaulted(string caso, string campo)
    {
        var solicitud = FacturaBorradorTestData.Solicitud();
        solicitud = caso switch
        {
            "detalles-null" => solicitud with { Detalles = null },
            "detalle-null" => solicitud with { Detalles = [null] },
            "ajustes-null" => solicitud with { Ajustes = null },
            "ajuste-null" => solicitud with { Ajustes = [null] },
            "anticipos-null" => solicitud with { Anticipos = null },
            "anticipo-null" => solicitud with { Anticipos = [null] },
            "notas-null" => solicitud with { Notas = null },
            "nota-null" => solicitud with { Notas = [null] },
            "impuestos-null" => solicitud with
            {
                Detalles = [FacturaBorradorTestData.Detalle() with { Impuestos = null }]
            },
            "impuesto-null" => solicitud with
            {
                Detalles = [FacturaBorradorTestData.Detalle() with { Impuestos = [null] }]
            },
            "ajustes-detalle-null" => solicitud with
            {
                Detalles = [FacturaBorradorTestData.Detalle() with { Ajustes = null }]
            },
            "medios-null" => solicitud with { CondicionesPago = solicitud.CondicionesPago! with { Medios = null } },
            "medio-null" => solicitud with { CondicionesPago = solicitud.CondicionesPago! with { Medios = [null] } },
            "responsabilidades-null" => solicitud with
            {
                Adquirente = solicitud.Adquirente! with { ResponsabilidadesFiscales = null }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(caso))
        };

        var errores = Validar(solicitud);

        Assert.Contains(errores, error => error.Campo == campo);
        Assert.Throws<FacturaBorradorInvalidaException>(() =>
            new PrepararFacturaBorrador(TimeProvider.System).Preparar(solicitud, FacturaBorradorTestData.Contexto));
    }

    [Fact]
    public void Validar_DuplicateLineNumbersAreRejected()
    {
        var detalle = FacturaBorradorTestData.Detalle();

        var errores = Validar(FacturaBorradorTestData.Solicitud() with { Detalles = [detalle, detalle] });

        Assert.Contains(errores, error => error.Campo == "Detalles[1].Numero" && error.Codigo == "duplicado");
    }

    [Fact]
    public void Validar_CreditRequiresDueDate()
    {
        var solicitud = FacturaBorradorTestData.Solicitud();
        solicitud = solicitud with
        {
            CondicionesPago = solicitud.CondicionesPago! with { FormaPagoCodigo = "2", FechaVencimiento = null }
        };

        Assert.Contains(Validar(solicitud), error => error.Campo == "CondicionesPago.FechaVencimiento");
    }

    [Theory]
    [InlineData(26, false)]
    [InlineData(27, true)]
    [InlineData(28, true)]
    public void Validar_DueDateUsesLocalIssueDateNotUtc(int diaVencimiento, bool valido)
    {
        var solicitud = FacturaBorradorTestData.Solicitud();
        solicitud = solicitud with
        {
            FechaHoraEmision = new DateTimeOffset(2026, 10, 27, 23, 30, 0, TimeSpan.FromHours(-5)),
            CondicionesPago = solicitud.CondicionesPago! with
            {
                FormaPagoCodigo = "2", FechaVencimiento = new DateOnly(2026, 10, diaVencimiento)
            }
        };

        Assert.Equal(valido, Validar(solicitud).Count == 0);
    }

    [Theory]
    [InlineData("sin-tarifa")]
    [InlineData("dos-tarifas")]
    [InlineData("negativo")]
    [InlineData("especifico-sin-base")]
    [InlineData("especifico-sin-unidad")]
    [InlineData("porcentual-con-unidad")]
    public void Validar_AmbiguousOrInvalidTaxInputsAreRejected(string caso)
    {
        var impuesto = caso switch
        {
            "sin-tarifa" => new ImpuestoFacturaSolicitud { TributoCodigo = "01" },
            "dos-tarifas" => new ImpuestoFacturaSolicitud
            {
                TributoCodigo = "01", TarifaPorcentaje = 19m, ValorPorUnidad = 1m
            },
            "negativo" => new ImpuestoFacturaSolicitud { TributoCodigo = "01", TarifaPorcentaje = -1m },
            "especifico-sin-base" => new ImpuestoFacturaSolicitud
            {
                TributoCodigo = "02", ValorPorUnidad = 1m, UnidadMedidaCodigo = "LTR"
            },
            "especifico-sin-unidad" => new ImpuestoFacturaSolicitud
            {
                TributoCodigo = "02", ValorPorUnidad = 1m, CantidadBase = 1m
            },
            "porcentual-con-unidad" => new ImpuestoFacturaSolicitud
            {
                TributoCodigo = "01", TarifaPorcentaje = 19m, UnidadMedidaCodigo = "LTR"
            },
            _ => throw new ArgumentOutOfRangeException(nameof(caso))
        };
        var solicitud = FacturaBorradorTestData.Solicitud() with
        {
            Detalles = [FacturaBorradorTestData.Detalle() with { Impuestos = [impuesto] }]
        };

        Assert.Contains(Validar(solicitud), error =>
            error.Campo.StartsWith("Detalles[0].Impuestos[0]", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("sin-valor")]
    [InlineData("dos-valores")]
    [InlineData("porcentaje-sin-base")]
    [InlineData("fijo-con-base")]
    [InlineData("negativo")]
    public void Validar_AmbiguousAdjustmentsAreRejected(string caso)
    {
        var ajuste = new AjusteFacturaSolicitud { Motivo = "Prueba" };
        ajuste = caso switch
        {
            "sin-valor" => ajuste,
            "dos-valores" => ajuste with { Importe = 1m, Porcentaje = 1m, BaseCalculo = 100m },
            "porcentaje-sin-base" => ajuste with { Porcentaje = 1m },
            "fijo-con-base" => ajuste with { Importe = 1m, BaseCalculo = 100m },
            "negativo" => ajuste with { Importe = -1m },
            _ => throw new ArgumentOutOfRangeException(nameof(caso))
        };

        Assert.Contains(Validar(FacturaBorradorTestData.Solicitud() with { Ajustes = [ajuste] }),
            error => error.Campo.StartsWith("Ajustes[0]", StringComparison.Ordinal));
    }

    [Fact]
    public void Validar_NitRequiresSingleDigitButDoesNotRecalculateIt()
    {
        var solicitud = FacturaBorradorTestData.Solicitud();
        foreach (var digito in new[] { null, "", "12", "X" })
        {
            var invalida = solicitud with
            {
                Adquirente = solicitud.Adquirente! with
                {
                    Identificacion = solicitud.Adquirente.Identificacion! with { DigitoVerificacion = digito }
                }
            };
            Assert.Contains(Validar(invalida),
                error => error.Campo == "Adquirente.Identificacion.DigitoVerificacion");
        }
    }

    [Fact]
    public void Validar_PersonalIdentificationDoesNotRequireNitDigit()
    {
        var solicitud = FacturaBorradorTestData.Solicitud();
        solicitud = solicitud with
        {
            Adquirente = solicitud.Adquirente! with
            {
                TipoOrganizacionCodigo = "2",
                Identificacion = new IdentificacionFiscalSolicitud { TipoDocumentoCodigo = "13", Numero = "123456789" }
            }
        };

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void Validar_ZeroPriceAndZeroPercentageAreValidDraftInputs()
    {
        var solicitud = FacturaBorradorTestData.Solicitud() with
        {
            Detalles =
            [
                FacturaBorradorTestData.Detalle() with
                {
                    PrecioUnitario = 0m,
                    Impuestos = [new ImpuestoFacturaSolicitud { TributoCodigo = "01", TarifaPorcentaje = 0m }]
                }
            ]
        };

        Assert.Empty(Validar(solicitud));
    }

    [Fact]
    public void Validar_DoesNotAccumulateErrorsBetweenCalls()
    {
        Assert.NotEmpty(Validar(new CrearFacturaBorradorSolicitud()));
        Assert.Empty(Validar(FacturaBorradorTestData.Solicitud()));
    }

    private static IReadOnlyList<ErrorValidacionFactura> Validar(CrearFacturaBorradorSolicitud solicitud) =>
        ValidadorFacturaBorrador.Validar(solicitud, FacturaBorradorTestData.Contexto);
}
