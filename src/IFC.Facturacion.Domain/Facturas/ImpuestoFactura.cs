namespace IFC.Facturacion.Domain.Facturas;

public sealed record ImpuestoFactura
{
    public required string TributoCodigo { get; init; }
    public required bool EsRetencion { get; init; }
    public decimal? BaseGravable { get; init; }
    public decimal? TarifaPorcentaje { get; init; }
    public decimal? ValorPorUnidad { get; init; }
    public decimal? CantidadBase { get; init; }
    public string? UnidadMedidaCodigo { get; init; }
    public decimal? Importe { get; init; }
}
