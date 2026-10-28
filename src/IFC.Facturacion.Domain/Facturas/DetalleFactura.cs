namespace IFC.Facturacion.Domain.Facturas;

public sealed record DetalleFactura
{
    public required int Numero { get; init; }
    public required string CodigoProducto { get; init; }
    public required string Descripcion { get; init; }
    public required decimal Cantidad { get; init; }
    public required string UnidadMedidaCodigo { get; init; }
    public required decimal PrecioUnitario { get; init; }
    public required decimal CantidadBasePrecio { get; init; }
    public required IReadOnlyList<ImpuestoFactura> Impuestos { get; init; }
    public required IReadOnlyList<AjusteFactura> Ajustes { get; init; }
    public decimal? PrecioReferencia { get; init; }
    public string? TipoPrecioReferenciaCodigo { get; init; }
    public decimal? ImporteNeto { get; init; }
}
