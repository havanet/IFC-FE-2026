namespace IFC.Facturacion.Application.Facturas.CrearBorrador;

public sealed record ContextoCreacionFactura(
    string TenantId,
    Guid EmisorId,
    string UsuarioId,
    string ClaveIdempotencia);
