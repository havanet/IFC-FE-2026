CREATE OR REPLACE PROCEDURE "USP_ObtenerFacturaBorrador"
(
    IN p_tenant_id text,
    IN p_factura_id uuid,
    OUT p_encontrada boolean,
    OUT p_documento_resultado text
)
LANGUAGE plpgsql
AS $procedure$
BEGIN
    SELECT documento::text
    INTO p_documento_resultado
    FROM public.factura_borrador
    WHERE tenant_id = p_tenant_id
      AND factura_id = p_factura_id;

    p_encontrada := FOUND;
END;
$procedure$;
