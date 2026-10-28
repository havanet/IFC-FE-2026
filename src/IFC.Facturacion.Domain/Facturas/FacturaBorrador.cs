namespace IFC.Facturacion.Domain.Facturas;

public sealed record FacturaBorrador
{
    public required Guid Id { get; init; }
    public required string TenantId { get; init; }
    public required Guid EmisorId { get; init; }
    public required string CreadaPor { get; init; }
    public required DateTimeOffset CreadaEnUtc { get; init; }
    public required string ClaveIdempotencia { get; init; }
    public required DateTimeOffset FechaHoraEmision { get; init; }
    public required string TipoOperacionCodigo { get; init; }
    public required string MonedaCodigo { get; init; }
    public required AdquirenteFactura Adquirente { get; init; }
    public required CondicionesPagoFactura CondicionesPago { get; init; }
    public required IReadOnlyList<DetalleFactura> Detalles { get; init; }
    public required IReadOnlyList<AjusteFactura> Ajustes { get; init; }
    public required IReadOnlyList<AnticipoFactura> Anticipos { get; init; }
    public required IReadOnlyList<string> Notas { get; init; }
    public string? ReferenciaExterna { get; init; }
    public TotalesFactura? Totales { get; init; }
    public EstadoFactura Estado => EstadoFactura.Borrador;
}

public enum EstadoFactura
{
    Borrador = 1
}
