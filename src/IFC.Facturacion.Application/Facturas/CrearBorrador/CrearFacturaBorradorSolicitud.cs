using System.Text.Json.Serialization;

namespace IFC.Facturacion.Application.Facturas.CrearBorrador;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CrearFacturaBorradorSolicitud
{
    public DateTimeOffset? FechaHoraEmision { get; init; }
    public string? TipoOperacionCodigo { get; init; }
    public string? MonedaCodigo { get; init; }
    public AdquirenteFacturaSolicitud? Adquirente { get; init; }
    public CondicionesPagoFacturaSolicitud? CondicionesPago { get; init; }
    public IReadOnlyList<DetalleFacturaSolicitud?>? Detalles { get; init; } = [];
    public IReadOnlyList<AjusteFacturaSolicitud?>? Ajustes { get; init; } = [];
    public IReadOnlyList<AnticipoFacturaSolicitud?>? Anticipos { get; init; } = [];
    public IReadOnlyList<string?>? Notas { get; init; } = [];
    public string? ReferenciaExterna { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AdquirenteFacturaSolicitud
{
    public string? NombreRazonSocial { get; init; }
    public string? TipoOrganizacionCodigo { get; init; }
    public IdentificacionFiscalSolicitud? Identificacion { get; init; }
    public IReadOnlyList<string?>? ResponsabilidadesFiscales { get; init; } = [];
    public string? TributoCodigo { get; init; }
    public string? CorreoElectronico { get; init; }
    public string? Telefono { get; init; }
    public DireccionFiscalSolicitud? Direccion { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record IdentificacionFiscalSolicitud
{
    public string? TipoDocumentoCodigo { get; init; }
    public string? Numero { get; init; }
    public string? DigitoVerificacion { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DireccionFiscalSolicitud
{
    public string? PaisCodigo { get; init; }
    public string? Linea { get; init; }
    public string? DepartamentoCodigo { get; init; }
    public string? DepartamentoNombre { get; init; }
    public string? MunicipioCodigo { get; init; }
    public string? MunicipioNombre { get; init; }
    public string? CodigoPostal { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DetalleFacturaSolicitud
{
    public int? Numero { get; init; }
    public string? CodigoProducto { get; init; }
    public string? Descripcion { get; init; }
    public decimal? Cantidad { get; init; }
    public string? UnidadMedidaCodigo { get; init; }
    public decimal? PrecioUnitario { get; init; }
    public decimal? CantidadBasePrecio { get; init; } = 1m;
    public IReadOnlyList<ImpuestoFacturaSolicitud?>? Impuestos { get; init; } = [];
    public IReadOnlyList<AjusteFacturaSolicitud?>? Ajustes { get; init; } = [];
    public decimal? PrecioReferencia { get; init; }
    public string? TipoPrecioReferenciaCodigo { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ImpuestoFacturaSolicitud
{
    public string? TributoCodigo { get; init; }
    public bool EsRetencion { get; init; }
    public decimal? BaseGravable { get; init; }
    public decimal? TarifaPorcentaje { get; init; }
    public decimal? ValorPorUnidad { get; init; }
    public decimal? CantidadBase { get; init; }
    public string? UnidadMedidaCodigo { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AjusteFacturaSolicitud
{
    public bool EsCargo { get; init; }
    public string? Motivo { get; init; }
    public string? MotivoCodigo { get; init; }
    public decimal? Importe { get; init; }
    public decimal? Porcentaje { get; init; }
    public decimal? BaseCalculo { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CondicionesPagoFacturaSolicitud
{
    public string? FormaPagoCodigo { get; init; }
    public IReadOnlyList<MedioPagoFacturaSolicitud?>? Medios { get; init; } = [];
    public DateOnly? FechaVencimiento { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MedioPagoFacturaSolicitud
{
    public string? Codigo { get; init; }
    public string? Referencia { get; init; }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AnticipoFacturaSolicitud
{
    public string? Referencia { get; init; }
    public decimal? Importe { get; init; }
    public DateOnly? FechaPago { get; init; }
}
