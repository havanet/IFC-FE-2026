namespace IFC.Facturacion.Domain.Facturas;

public sealed record AjusteFactura
{
    public required bool EsCargo { get; init; }
    public required string Motivo { get; init; }
    public string? MotivoCodigo { get; init; }
    public decimal? Importe { get; init; }
    public decimal? Porcentaje { get; init; }
    public decimal? BaseCalculo { get; init; }
}
