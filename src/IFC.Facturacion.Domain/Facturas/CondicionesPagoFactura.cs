namespace IFC.Facturacion.Domain.Facturas;

public sealed record CondicionesPagoFactura
{
    public required FormaPagoFactura Forma { get; init; }
    public required IReadOnlyList<MedioPagoFactura> Medios { get; init; }
    public DateOnly? FechaVencimiento { get; init; }
}

public enum FormaPagoFactura
{
    Contado = 1,
    Credito = 2
}

public sealed record MedioPagoFactura
{
    public required string Codigo { get; init; }
    public string? Referencia { get; init; }
}

public sealed record AnticipoFactura
{
    public required string Referencia { get; init; }
    public required decimal Importe { get; init; }
    public DateOnly? FechaPago { get; init; }
}
