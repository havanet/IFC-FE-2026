namespace IFC.Facturacion.Application.Facturas.CrearBorrador;

public sealed class CrearFacturaBorradorCasoUso(
    PrepararFacturaBorrador preparador,
    IFacturaBorradorRepositorio repositorio)
{
    public async Task<ResultadoCreacionFacturaBorrador> EjecutarAsync(
        CrearFacturaBorradorSolicitud? solicitud,
        ContextoCreacionFactura? contexto,
        CancellationToken cancellationToken = default)
    {
        var factura = preparador.Preparar(solicitud, contexto);
        return await repositorio.CrearAsync(factura, cancellationToken);
    }
}
