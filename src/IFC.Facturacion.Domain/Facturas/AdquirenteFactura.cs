namespace IFC.Facturacion.Domain.Facturas;

public sealed record AdquirenteFactura
{
    public required string NombreRazonSocial { get; init; }
    public required string TipoOrganizacionCodigo { get; init; }
    public required IdentificacionFiscal Identificacion { get; init; }
    public required IReadOnlyList<string> ResponsabilidadesFiscales { get; init; }
    public string? TributoCodigo { get; init; }
    public string? CorreoElectronico { get; init; }
    public string? Telefono { get; init; }
    public DireccionFiscal? Direccion { get; init; }
}

public sealed record IdentificacionFiscal
{
    public required string TipoDocumentoCodigo { get; init; }
    public required string Numero { get; init; }
    public string? DigitoVerificacion { get; init; }
}

public sealed record DireccionFiscal
{
    public required string PaisCodigo { get; init; }
    public required string Linea { get; init; }
    public string? DepartamentoCodigo { get; init; }
    public string? DepartamentoNombre { get; init; }
    public string? MunicipioCodigo { get; init; }
    public string? MunicipioNombre { get; init; }
    public string? CodigoPostal { get; init; }
}
