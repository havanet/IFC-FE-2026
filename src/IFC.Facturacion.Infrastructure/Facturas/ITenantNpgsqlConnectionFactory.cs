using Npgsql;

namespace IFC.Facturacion.Infrastructure.Facturas;

public interface ITenantNpgsqlConnectionFactory
{
    Task<NpgsqlConnection> OpenAsync(string tenantId, CancellationToken cancellationToken);
}
