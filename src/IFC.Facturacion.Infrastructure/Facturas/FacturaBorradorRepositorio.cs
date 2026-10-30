using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using IFC.Facturacion.Application.Facturas.CrearBorrador;
using IFC.Facturacion.Domain.Facturas;

namespace IFC.Facturacion.Infrastructure.Facturas;

public sealed class FacturaBorradorRepositorio(
    ITenantNpgsqlConnectionFactory connectionFactory) : IFacturaBorradorRepositorio
{
    private const string CrearProcedimiento = "\"USP_CrearFacturaBorrador\"";
    private const string ObtenerProcedimiento = "\"USP_ObtenerFacturaBorrador\"";

    private static readonly JsonSerializerOptions JsonOptions = CrearOpcionesJson();

    public async Task<ResultadoCreacionFacturaBorrador> CrearAsync(
        FacturaBorrador factura, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(factura);

        await using var connection = await connectionFactory.OpenAsync(factura.TenantId, cancellationToken);
        var parametros = new DynamicParameters();
        parametros.Add("p_tenant_id", factura.TenantId, DbType.String);
        parametros.Add("p_emisor_id", factura.EmisorId, DbType.Guid);
        parametros.Add("p_usuario_id", factura.CreadaPor, DbType.String);
        parametros.Add("p_clave_idempotencia", factura.ClaveIdempotencia, DbType.String);
        parametros.Add("p_documento_json", JsonSerializer.Serialize(factura, JsonOptions), DbType.String);
        parametros.Add("p_documento_resultado", dbType: DbType.String, direction: ParameterDirection.Output);
        parametros.Add("p_creada", dbType: DbType.Boolean, direction: ParameterDirection.Output);
        parametros.Add("p_conflicto", dbType: DbType.Boolean, direction: ParameterDirection.Output);

        await using var reader = await connection.ExecuteReaderAsync(new CommandDefinition(
            CrearProcedimiento,
            parametros,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidDataException("El procedimiento almacenado no devolvio un resultado de creacion.");
        }

        if (reader.GetBoolean(2))
        {
            throw new IdempotenciaFacturaBorradorConflictivaException();
        }

        var documento = DeserializarDocumento(
            reader.IsDBNull(0) ? null : reader.GetString(0));
        return new ResultadoCreacionFacturaBorrador(documento, reader.GetBoolean(1));
    }

    public async Task<FacturaBorrador?> ObtenerAsync(
        string tenantId, Guid facturaId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentOutOfRangeException.ThrowIfEqual(facturaId, Guid.Empty);

        await using var connection = await connectionFactory.OpenAsync(tenantId, cancellationToken);
        var parametros = new DynamicParameters();
        parametros.Add("p_tenant_id", tenantId, DbType.String);
        parametros.Add("p_factura_id", facturaId, DbType.Guid);
        parametros.Add("p_encontrada", dbType: DbType.Boolean, direction: ParameterDirection.Output);
        parametros.Add("p_documento_resultado", dbType: DbType.String, direction: ParameterDirection.Output);

        await using var reader = await connection.ExecuteReaderAsync(new CommandDefinition(
            ObtenerProcedimiento,
            parametros,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidDataException("El procedimiento almacenado no devolvio un resultado de consulta.");
        }

        if (!reader.GetBoolean(0))
        {
            return null;
        }

        return DeserializarDocumento(reader.IsDBNull(1) ? null : reader.GetString(1));
    }

    private static FacturaBorrador DeserializarDocumento(string? documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            throw new InvalidDataException("El procedimiento almacenado no devolvio el documento de factura.");
        }

        return JsonSerializer.Deserialize<FacturaBorrador>(documento, JsonOptions)
            ?? throw new InvalidDataException("El procedimiento almacenado devolvio un documento de factura invalido.");
    }

    private static JsonSerializerOptions CrearOpcionesJson()
    {
        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        opciones.Converters.Add(new JsonStringEnumConverter());
        return opciones;
    }
}
