namespace IFC.Facturacion.Application.Facturas.CrearBorrador;

public sealed class IdempotenciaFacturaBorradorConflictivaException : Exception
{
    public IdempotenciaFacturaBorradorConflictivaException()
        : base("La clave de idempotencia ya se utilizo con datos diferentes.")
    {
    }
}
