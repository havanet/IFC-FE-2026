using IFC.Facturacion.Domain.Facturas;

namespace IFC.Facturacion.Application.Facturas.CrearBorrador;

public sealed class PrepararFacturaBorrador
{
    private readonly TimeProvider timeProvider;

    public PrepararFacturaBorrador(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        this.timeProvider = timeProvider;
    }

    public FacturaBorrador Preparar(
        CrearFacturaBorradorSolicitud? solicitud, ContextoCreacionFactura? contexto)
    {
        var errores = ValidadorFacturaBorrador.Validar(solicitud, contexto);
        if (errores.Count != 0)
        {
            throw new FacturaBorradorInvalidaException(errores);
        }

        // All nullable input fields and collection elements are checked before mapping.
        return new FacturaBorrador
        {
            Id = Guid.NewGuid(),
            TenantId = contexto!.TenantId,
            EmisorId = solicitud!.EmisorId!.Value,
            CreadaPor = contexto.UsuarioId,
            CreadaEnUtc = timeProvider.GetUtcNow(),
            ClaveIdempotencia = contexto.ClaveIdempotencia,
            FechaHoraEmision = solicitud.FechaHoraEmision!.Value,
            TipoOperacionCodigo = solicitud.TipoOperacionCodigo!,
            MonedaCodigo = solicitud.MonedaCodigo!,
            Adquirente = MapearAdquirente(solicitud.Adquirente!),
            CondicionesPago = MapearPago(solicitud.CondicionesPago!),
            Detalles = Array.AsReadOnly(solicitud.Detalles!.Select(detalle => MapearDetalle(detalle!)).ToArray()),
            Ajustes = Array.AsReadOnly(solicitud.Ajustes!.Select(ajuste => MapearAjuste(ajuste!)).ToArray()),
            Anticipos = Array.AsReadOnly(solicitud.Anticipos!.Select(anticipo => new AnticipoFactura
            {
                Referencia = anticipo!.Referencia!,
                Importe = anticipo.Importe!.Value,
                FechaPago = anticipo.FechaPago
            }).ToArray()),
            Notas = Array.AsReadOnly(solicitud.Notas!.Select(nota => nota!).ToArray()),
            ReferenciaExterna = solicitud.ReferenciaExterna
        };
    }

    private static AdquirenteFactura MapearAdquirente(AdquirenteFacturaSolicitud solicitud) => new()
    {
        NombreRazonSocial = solicitud.NombreRazonSocial!,
        TipoOrganizacionCodigo = solicitud.TipoOrganizacionCodigo!,
        Identificacion = new IdentificacionFiscal
        {
            TipoDocumentoCodigo = solicitud.Identificacion!.TipoDocumentoCodigo!,
            Numero = solicitud.Identificacion.Numero!,
            DigitoVerificacion = solicitud.Identificacion.DigitoVerificacion
        },
        ResponsabilidadesFiscales = Array.AsReadOnly(
            solicitud.ResponsabilidadesFiscales!.Select(responsabilidad => responsabilidad!).ToArray()),
        TributoCodigo = solicitud.TributoCodigo,
        CorreoElectronico = solicitud.CorreoElectronico,
        Telefono = solicitud.Telefono,
        Direccion = solicitud.Direccion is { } direccion ? new DireccionFiscal
        {
            PaisCodigo = direccion.PaisCodigo!,
            Linea = direccion.Linea!,
            DepartamentoCodigo = direccion.DepartamentoCodigo,
            DepartamentoNombre = direccion.DepartamentoNombre,
            MunicipioCodigo = direccion.MunicipioCodigo,
            MunicipioNombre = direccion.MunicipioNombre,
            CodigoPostal = direccion.CodigoPostal
        } : null
    };

    private static CondicionesPagoFactura MapearPago(CondicionesPagoFacturaSolicitud solicitud) => new()
    {
        Forma = solicitud.FormaPagoCodigo switch
        {
            "1" => FormaPagoFactura.Contado,
            "2" => FormaPagoFactura.Credito,
            _ => throw new InvalidOperationException("La forma de pago debe validarse antes de mapear.")
        },
        FechaVencimiento = solicitud.FechaVencimiento,
        Medios = Array.AsReadOnly(solicitud.Medios!.Select(medio => new MedioPagoFactura
        {
            Codigo = medio!.Codigo!,
            Referencia = medio.Referencia
        }).ToArray())
    };

    private static DetalleFactura MapearDetalle(DetalleFacturaSolicitud solicitud) => new()
    {
        Numero = solicitud.Numero!.Value,
        CodigoProducto = solicitud.CodigoProducto!,
        Descripcion = solicitud.Descripcion!,
        Cantidad = solicitud.Cantidad!.Value,
        UnidadMedidaCodigo = solicitud.UnidadMedidaCodigo!,
        PrecioUnitario = solicitud.PrecioUnitario!.Value,
        CantidadBasePrecio = solicitud.CantidadBasePrecio!.Value,
        PrecioReferencia = solicitud.PrecioReferencia,
        TipoPrecioReferenciaCodigo = solicitud.TipoPrecioReferenciaCodigo,
        Impuestos = Array.AsReadOnly(solicitud.Impuestos!.Select(impuesto => new ImpuestoFactura
        {
            TributoCodigo = impuesto!.TributoCodigo!,
            EsRetencion = impuesto.EsRetencion,
            BaseGravable = impuesto.BaseGravable,
            TarifaPorcentaje = impuesto.TarifaPorcentaje,
            ValorPorUnidad = impuesto.ValorPorUnidad,
            CantidadBase = impuesto.CantidadBase,
            UnidadMedidaCodigo = impuesto.UnidadMedidaCodigo
        }).ToArray()),
        Ajustes = Array.AsReadOnly(solicitud.Ajustes!.Select(ajuste => MapearAjuste(ajuste!)).ToArray())
    };

    private static AjusteFactura MapearAjuste(AjusteFacturaSolicitud solicitud) => new()
    {
        EsCargo = solicitud.EsCargo,
        Motivo = solicitud.Motivo!,
        MotivoCodigo = solicitud.MotivoCodigo,
        Importe = solicitud.Importe,
        Porcentaje = solicitud.Porcentaje,
        BaseCalculo = solicitud.BaseCalculo
    };
}
