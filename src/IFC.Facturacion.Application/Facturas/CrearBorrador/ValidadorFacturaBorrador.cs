using System.Net.Mail;

namespace IFC.Facturacion.Application.Facturas.CrearBorrador;

public sealed class ValidadorFacturaBorrador
{
    private readonly List<ErrorValidacionFactura> errores = [];

    private ValidadorFacturaBorrador()
    {
    }

    public static IReadOnlyList<ErrorValidacionFactura> Validar(
        CrearFacturaBorradorSolicitud? solicitud, ContextoCreacionFactura? contexto)
    {
        var validador = new ValidadorFacturaBorrador();
        validador.ValidarContexto(contexto);
        validador.ValidarSolicitud(solicitud);
        return validador.errores.AsReadOnly();
    }

    private void ValidarContexto(ContextoCreacionFactura? contexto)
    {
        if (contexto is null)
        {
            Error("Contexto", "requerido", "Se requiere el contexto autorizado de la operacion.");
            return;
        }

        Requerido(contexto.TenantId, "Contexto.TenantId");
        if (contexto.EmisorId == Guid.Empty)
        {
            Error("Contexto.EmisorId", "requerido", "Se requiere el emisor autorizado de la operacion.");
        }
        Requerido(contexto.UsuarioId, "Contexto.UsuarioId");
        Requerido(contexto.ClaveIdempotencia, "Contexto.ClaveIdempotencia");
    }

    private void ValidarSolicitud(CrearFacturaBorradorSolicitud? solicitud)
    {
        if (solicitud is null)
        {
            Error("Factura", "requerido", "Se requiere la solicitud de factura.");
            return;
        }

        if (solicitud.FechaHoraEmision is null || solicitud.FechaHoraEmision == default(DateTimeOffset))
        {
            Error("FechaHoraEmision", "requerido", "Se requiere una fecha y hora de emision.");
        }
        Codigo(solicitud.MonedaCodigo, "MonedaCodigo", 3, soloLetras: true);
        Codigo(solicitud.TipoOperacionCodigo, "TipoOperacionCodigo", 2, soloDigitos: true);

        ValidarAdquirente(solicitud.Adquirente);
        ValidarPago(solicitud.CondicionesPago, solicitud.FechaHoraEmision);
        Coleccion(solicitud.Detalles, "Detalles", ValidarDetalle, requerida: true);
        Coleccion(solicitud.Ajustes, "Ajustes", ValidarAjuste);
        Coleccion(solicitud.Anticipos, "Anticipos", ValidarAnticipo);
        Coleccion(solicitud.Notas, "Notas", (nota, campo) => Requerido(nota, campo));

        var numeros = new HashSet<int>();
        if (solicitud.Detalles is not null)
        {
            for (var i = 0; i < solicitud.Detalles.Count; i++)
            {
                if (solicitud.Detalles[i]?.Numero is int numero && numero > 0 && !numeros.Add(numero))
                {
                    Error($"Detalles[{i}].Numero", "duplicado", "El numero de detalle no puede repetirse.");
                }
            }
        }
    }

    private void ValidarAdquirente(AdquirenteFacturaSolicitud? adquirente)
    {
        const string campo = "Adquirente";
        if (adquirente is null)
        {
            Error(campo, "requerido", "Se requiere el adquirente.");
            return;
        }

        Requerido(adquirente.NombreRazonSocial, $"{campo}.NombreRazonSocial");
        if (adquirente.TipoOrganizacionCodigo is not ("1" or "2"))
        {
            Error($"{campo}.TipoOrganizacionCodigo", "formato_invalido",
                "El tipo de organizacion debe ser 1 (juridica) o 2 (natural).");
        }

        var identificacion = adquirente.Identificacion;
        if (identificacion is null)
        {
            Error($"{campo}.Identificacion", "requerido", "Se requiere la identificacion fiscal.");
        }
        else
        {
            Codigo(identificacion.TipoDocumentoCodigo, $"{campo}.Identificacion.TipoDocumentoCodigo",
                2, soloDigitos: true);
            Requerido(identificacion.Numero, $"{campo}.Identificacion.Numero");
            if (identificacion.TipoDocumentoCodigo == "31" || identificacion.DigitoVerificacion is not null)
            {
                Codigo(identificacion.DigitoVerificacion, $"{campo}.Identificacion.DigitoVerificacion",
                    1, soloDigitos: true);
            }
        }

        Coleccion(adquirente.ResponsabilidadesFiscales, $"{campo}.ResponsabilidadesFiscales",
            (responsabilidad, ruta) => Requerido(responsabilidad, ruta));
        if (adquirente.TributoCodigo is not null)
        {
            Codigo(adquirente.TributoCodigo, $"{campo}.TributoCodigo", 2);
        }
        if (adquirente.CorreoElectronico is not null &&
            (!MailAddress.TryCreate(adquirente.CorreoElectronico, out var correo) ||
             correo.Address != adquirente.CorreoElectronico))
        {
            Error($"{campo}.CorreoElectronico", "formato_invalido", "El correo electronico no es valido.");
        }

        if (adquirente.Direccion is { } direccion)
        {
            Codigo(direccion.PaisCodigo, $"{campo}.Direccion.PaisCodigo", 2, soloLetras: true);
            Requerido(direccion.Linea, $"{campo}.Direccion.Linea");
        }
    }

    private void ValidarPago(CondicionesPagoFacturaSolicitud? pago, DateTimeOffset? fechaEmision)
    {
        const string campo = "CondicionesPago";
        if (pago is null)
        {
            Error(campo, "requerido", "Se requieren las condiciones de pago.");
            return;
        }

        if (pago.FormaPagoCodigo is not ("1" or "2"))
        {
            Error($"{campo}.FormaPagoCodigo", "formato_invalido",
                "La forma de pago debe ser 1 (contado) o 2 (credito).");
        }
        if (pago.FormaPagoCodigo == "2" && pago.FechaVencimiento is null)
        {
            Error($"{campo}.FechaVencimiento", "requerido", "El pago a credito requiere vencimiento.");
        }
        if (pago.FechaVencimiento is { } vencimiento &&
            (vencimiento == default ||
             fechaEmision is { } emision && vencimiento < DateOnly.FromDateTime(emision.DateTime)))
        {
            Error($"{campo}.FechaVencimiento", "valor_fuera_rango",
                "El vencimiento no puede ser anterior a la fecha local de emision.");
        }
        Coleccion(pago.Medios, $"{campo}.Medios",
            (medio, ruta) => Requerido(medio.Codigo, $"{ruta}.Codigo"), requerida: true);
    }

    private void ValidarDetalle(DetalleFacturaSolicitud detalle, string campo)
    {
        if (detalle.Numero is null or <= 0)
        {
            Error($"{campo}.Numero", "valor_fuera_rango", "El numero de detalle debe ser positivo.");
        }
        Requerido(detalle.CodigoProducto, $"{campo}.CodigoProducto");
        Requerido(detalle.Descripcion, $"{campo}.Descripcion");
        Requerido(detalle.UnidadMedidaCodigo, $"{campo}.UnidadMedidaCodigo");
        Numero(detalle.Cantidad, $"{campo}.Cantidad", requerido: true, positivo: true);
        Numero(detalle.PrecioUnitario, $"{campo}.PrecioUnitario", requerido: true);
        Numero(detalle.CantidadBasePrecio, $"{campo}.CantidadBasePrecio", requerido: true, positivo: true);
        Numero(detalle.PrecioReferencia, $"{campo}.PrecioReferencia", positivo: true);

        if (detalle.PrecioReferencia.HasValue != (detalle.TipoPrecioReferenciaCodigo is not null))
        {
            Error($"{campo}.PrecioReferencia", "inconsistente",
                "El precio de referencia y su tipo deben informarse juntos.");
        }
        if (detalle.TipoPrecioReferenciaCodigo is not null)
        {
            Requerido(detalle.TipoPrecioReferenciaCodigo, $"{campo}.TipoPrecioReferenciaCodigo");
        }
        Coleccion(detalle.Impuestos, $"{campo}.Impuestos", ValidarImpuesto);
        Coleccion(detalle.Ajustes, $"{campo}.Ajustes", ValidarAjuste);
    }

    private void ValidarImpuesto(ImpuestoFacturaSolicitud impuesto, string campo)
    {
        Codigo(impuesto.TributoCodigo, $"{campo}.TributoCodigo", 2);
        Numero(impuesto.BaseGravable, $"{campo}.BaseGravable");
        Numero(impuesto.TarifaPorcentaje, $"{campo}.TarifaPorcentaje");
        Numero(impuesto.ValorPorUnidad, $"{campo}.ValorPorUnidad");

        if (impuesto.TarifaPorcentaje.HasValue == impuesto.ValorPorUnidad.HasValue)
        {
            Error(campo, "inconsistente", "Informe una tarifa porcentual o un valor por unidad, no ambos.");
        }
        if (impuesto.ValorPorUnidad.HasValue)
        {
            Numero(impuesto.CantidadBase, $"{campo}.CantidadBase", requerido: true, positivo: true);
            Requerido(impuesto.UnidadMedidaCodigo, $"{campo}.UnidadMedidaCodigo");
        }
        else if (impuesto.CantidadBase.HasValue || impuesto.UnidadMedidaCodigo is not null)
        {
            Error(campo, "inconsistente", "La base en unidades solo aplica al impuesto por unidad.");
        }
    }

    private void ValidarAjuste(AjusteFacturaSolicitud ajuste, string campo)
    {
        Requerido(ajuste.Motivo, $"{campo}.Motivo");
        Numero(ajuste.Importe, $"{campo}.Importe");
        Numero(ajuste.Porcentaje, $"{campo}.Porcentaje");
        Numero(ajuste.BaseCalculo, $"{campo}.BaseCalculo");
        if (ajuste.Importe.HasValue == ajuste.Porcentaje.HasValue)
        {
            Error(campo, "inconsistente", "Informe un importe fijo o un porcentaje, no ambos.");
        }
        if (ajuste.Porcentaje.HasValue && !ajuste.BaseCalculo.HasValue)
        {
            Error($"{campo}.BaseCalculo", "requerido", "El ajuste porcentual requiere una base de calculo.");
        }
        else if (ajuste.Importe.HasValue && ajuste.BaseCalculo.HasValue)
        {
            Error($"{campo}.BaseCalculo", "inconsistente", "Un ajuste fijo no requiere base de calculo.");
        }
    }

    private void ValidarAnticipo(AnticipoFacturaSolicitud anticipo, string campo)
    {
        Requerido(anticipo.Referencia, $"{campo}.Referencia");
        Numero(anticipo.Importe, $"{campo}.Importe", requerido: true, positivo: true);
        if (anticipo.FechaPago == default(DateOnly))
        {
            Error($"{campo}.FechaPago", "valor_fuera_rango", "La fecha de pago no es valida.");
        }
    }

    private void Coleccion<T>(IReadOnlyList<T?>? elementos, string campo,
        Action<T, string> validar, bool requerida = false) where T : class
    {
        if (elementos is null)
        {
            Error(campo, "requerido", "La coleccion no puede ser null; use una lista vacia si no aplica.");
            return;
        }
        if (requerida && elementos.Count == 0)
        {
            Error(campo, "requerido", "Se requiere al menos un elemento.");
        }
        for (var i = 0; i < elementos.Count; i++)
        {
            if (elementos[i] is { } elemento)
            {
                validar(elemento, $"{campo}[{i}]");
            }
            else
            {
                Error($"{campo}[{i}]", "requerido", "El elemento no puede ser null.");
            }
        }
    }

    private bool Requerido(string? valor, string campo)
    {
        if (!string.IsNullOrWhiteSpace(valor))
        {
            return true;
        }
        Error(campo, "requerido", "El valor es obligatorio.");
        return false;
    }

    private void Codigo(string? valor, string campo, int longitud,
        bool soloLetras = false, bool soloDigitos = false)
    {
        if (!Requerido(valor, campo))
        {
            return;
        }
        if (valor!.Length != longitud || !valor.All(caracter =>
                soloLetras ? char.IsAsciiLetterUpper(caracter) :
                soloDigitos ? char.IsAsciiDigit(caracter) :
                char.IsAsciiLetterUpper(caracter) || char.IsAsciiDigit(caracter)))
        {
            Error(campo, "formato_invalido", $"El codigo debe tener {longitud} caracteres ASCII validos.");
        }
    }

    private void Numero(decimal? valor, string campo, bool requerido = false, bool positivo = false)
    {
        if (valor is null)
        {
            if (requerido)
            {
                Error(campo, "requerido", "El valor numerico es obligatorio.");
            }
            return;
        }
        if (valor < 0 || positivo && valor == 0)
        {
            Error(campo, "valor_fuera_rango",
                positivo ? "El valor debe ser mayor que cero." : "El valor no puede ser negativo.");
        }
    }

    private void Error(string campo, string codigo, string mensaje) =>
        errores.Add(new ErrorValidacionFactura(campo, codigo, mensaje));
}
