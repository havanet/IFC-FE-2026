namespace IFC.Facturacion.Application.Facturas.CrearBorrador;

public sealed record ContextoCreacionFactura(
    string TenantId,
    string UsuarioId,
    string ClaveIdempotencia);
