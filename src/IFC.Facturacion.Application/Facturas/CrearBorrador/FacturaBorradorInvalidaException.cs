namespace IFC.Facturacion.Application.Facturas.CrearBorrador;

public sealed class FacturaBorradorInvalidaException : Exception
{
    public FacturaBorradorInvalidaException(IReadOnlyList<ErrorValidacionFactura> errores)
        : base("La solicitud de factura borrador contiene errores de validacion.")
    {
        ArgumentNullException.ThrowIfNull(errores);
        if (errores.Count == 0)
        {
            throw new ArgumentException("Debe existir al menos un error de validacion.", nameof(errores));
        }

        Errores = Array.AsReadOnly(errores.ToArray());
    }

    public IReadOnlyList<ErrorValidacionFactura> Errores { get; }
}
