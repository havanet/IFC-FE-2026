using IFC.Facturacion.Domain.Facturas;

namespace IFC.Facturacion.Application.Facturas.CrearBorrador;

public interface IFacturaBorradorRepositorio
{
    Task<ResultadoCreacionFacturaBorrador> CrearAsync(
        FacturaBorrador factura, CancellationToken cancellationToken);

    Task<FacturaBorrador?> ObtenerAsync(
        string tenantId, Guid facturaId, CancellationToken cancellationToken);
}

public sealed record ResultadoCreacionFacturaBorrador(FacturaBorrador Factura, bool Creada);
