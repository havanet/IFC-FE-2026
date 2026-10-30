using IFC.Facturacion.Application.Facturas.CrearBorrador;
using IFC.Facturacion.Domain.Facturas;

namespace IFC.Facturacion.Tests.Application.Facturas;

public sealed class FacturaBorradorCasoUsoTests
{
    [Fact]
    public async Task Crear_UsesAuthorizedEmitterAndReturnsRepositoryResult()
    {
        var repositorio = new RepositorioFalso();
        var casoUso = new CrearFacturaBorradorCasoUso(
            new PrepararFacturaBorrador(TimeProvider.System),
            repositorio);
        var contexto = FacturaBorradorTestData.Contexto;

        var resultado = await casoUso.EjecutarAsync(
            FacturaBorradorTestData.Solicitud(), contexto);

        Assert.NotNull(repositorio.FacturaRecibida);
        Assert.Equal(contexto.TenantId, repositorio.FacturaRecibida.TenantId);
        Assert.Equal(contexto.EmisorId, repositorio.FacturaRecibida.EmisorId);
        Assert.Same(repositorio.FacturaRecibida, resultado.Factura);
        Assert.True(resultado.Creada);
    }

    [Fact]
    public async Task Obtener_RestrictsRepositoryLookupToAuthorizedTenant()
    {
        var repositorio = new RepositorioFalso();
        var casoUso = new ObtenerFacturaBorradorCasoUso(repositorio);
        var contexto = new ContextoConsultaFactura("tenant-autorizado", "usuario-autorizado");

        await casoUso.EjecutarAsync(Guid.NewGuid(), contexto);

        Assert.Equal(contexto.TenantId, repositorio.TenantRecibido);
    }

    private sealed class RepositorioFalso : IFacturaBorradorRepositorio
    {
        public FacturaBorrador? FacturaRecibida { get; private set; }
        public string? TenantRecibido { get; private set; }

        public Task<ResultadoCreacionFacturaBorrador> CrearAsync(
            FacturaBorrador factura, CancellationToken cancellationToken)
        {
            FacturaRecibida = factura;
            return Task.FromResult(new ResultadoCreacionFacturaBorrador(factura, Creada: true));
        }

        public Task<FacturaBorrador?> ObtenerAsync(
            string tenantId, Guid facturaId, CancellationToken cancellationToken)
        {
            TenantRecibido = tenantId;
            return Task.FromResult<FacturaBorrador?>(null);
        }
    }
}
