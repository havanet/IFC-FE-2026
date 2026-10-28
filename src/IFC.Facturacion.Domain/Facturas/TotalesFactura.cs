namespace IFC.Facturacion.Domain.Facturas;

public sealed record TotalesFactura
{
    public required decimal NetoLineas { get; init; }
    public required decimal SinImpuestos { get; init; }
    public required decimal Impuestos { get; init; }
    public required decimal Retenciones { get; init; }
    public required decimal ConImpuestos { get; init; }
    public required decimal Descuentos { get; init; }
    public required decimal Cargos { get; init; }
    public required decimal Anticipos { get; init; }
    public required decimal Redondeo { get; init; }
    public required decimal Pagar { get; init; }
}
