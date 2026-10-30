using IFC.Facturacion.Domain.Facturas;

namespace IFC.Facturacion.Application.Facturas.CrearBorrador;

public sealed class ObtenerFacturaBorradorCasoUso(IFacturaBorradorRepositorio repositorio)
{
    public Task<FacturaBorrador?> EjecutarAsync(
        Guid facturaId,
        ContextoConsultaFactura? contexto,
        CancellationToken cancellationToken = default)
    {
        if (facturaId == Guid.Empty)
        {
            throw new ArgumentException("El identificador de factura no puede estar vacio.", nameof(facturaId));
        }

        if (contexto is null || string.IsNullOrWhiteSpace(contexto.TenantId) ||
            string.IsNullOrWhiteSpace(contexto.UsuarioId))
        {
            throw new ArgumentException("Se requiere el contexto autorizado de consulta.", nameof(contexto));
        }

        return repositorio.ObtenerAsync(contexto.TenantId, facturaId, cancellationToken);
    }
}
