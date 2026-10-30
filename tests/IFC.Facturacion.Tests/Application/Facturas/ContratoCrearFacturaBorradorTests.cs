using System.Text.Json;
using System.Text.Json.Nodes;
using IFC.Facturacion.Application.Facturas.CrearBorrador;

namespace IFC.Facturacion.Tests.Application.Facturas;

public sealed class ContratoCrearFacturaBorradorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Json_RoundTripPreservesInputPrecisionAndOffset()
    {
        var solicitud = FacturaBorradorTestData.Solicitud();

        var json = JsonSerializer.Serialize(solicitud, JsonOptions);
        var leida = Assert.IsType<CrearFacturaBorradorSolicitud>(
            JsonSerializer.Deserialize<CrearFacturaBorradorSolicitud>(json, JsonOptions));

        Assert.Equal(solicitud.FechaHoraEmision, leida.FechaHoraEmision);
        Assert.Equal(TimeSpan.FromHours(-5), leida.FechaHoraEmision!.Value.Offset);
        Assert.Equal(solicitud.Detalles![0]!.PrecioUnitario, leida.Detalles![0]!.PrecioUnitario);
        Assert.Empty(ValidadorFacturaBorrador.Validar(leida, FacturaBorradorTestData.Contexto));
    }

    [Theory]
    [InlineData("id")]
    [InlineData("tenantId")]
    [InlineData("emisorId")]
    [InlineData("creadaPor")]
    [InlineData("creadaEnUtc")]
    [InlineData("claveIdempotencia")]
    [InlineData("numeroFactura")]
    [InlineData("cufe")]
    [InlineData("estado")]
    [InlineData("totales")]
    public void Json_RejectsServerOwnedOrUnknownFields(string campo)
    {
        var json = Assert.IsType<JsonObject>(
            JsonSerializer.SerializeToNode(FacturaBorradorTestData.Solicitud(), JsonOptions));
        json[campo] = "valor-no-autorizado";

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CrearFacturaBorradorSolicitud>(json.ToJsonString(), JsonOptions));
    }

    [Theory]
    [InlineData("importeNeto")]
    [InlineData("total")]
    public void Json_RejectsComputedFieldsInsideLine(string campo)
    {
        var json = Assert.IsType<JsonObject>(
            JsonSerializer.SerializeToNode(FacturaBorradorTestData.Solicitud(), JsonOptions));
        var detalles = Assert.IsType<JsonArray>(json["detalles"]);
        Assert.IsType<JsonObject>(detalles[0])[campo] = 123m;

        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<CrearFacturaBorradorSolicitud>(json.ToJsonString(), JsonOptions));
    }

    [Fact]
    public void Json_MissingNumericValueIsDifferentFromExplicitZero()
    {
        var solicitud = JsonSerializer.Deserialize<CrearFacturaBorradorSolicitud>(
            """{"detalles":[{"precioUnitario":0}]}""", JsonOptions);

        var detalle = Assert.Single(solicitud!.Detalles!);
        Assert.Equal(0m, detalle!.PrecioUnitario);
        Assert.Null(detalle.Cantidad);
        Assert.Equal(1m, detalle.CantidadBasePrecio);
        Assert.Contains(ValidadorFacturaBorrador.Validar(solicitud, FacturaBorradorTestData.Contexto),
            error => error.Campo == "Detalles[0].Cantidad" && error.Codigo == "requerido");
    }
}
