CREATE OR REPLACE PROCEDURE "USP_CrearFacturaBorrador"
(
    IN p_tenant_id text,
    IN p_emisor_id uuid,
    IN p_usuario_id text,
    IN p_clave_idempotencia text,
    IN p_documento_json text,
    OUT p_documento_resultado text,
    OUT p_creada boolean,
    OUT p_conflicto boolean
)
LANGUAGE plpgsql
AS $procedure$
DECLARE
    v_entrada jsonb;
    v_documento jsonb;
    v_huella jsonb;
    v_documento_existente jsonb;
    v_huella_existente jsonb;
    v_id uuid;
    v_creada_en_utc timestamptz;
    v_filas_insertadas integer;
BEGIN
    IF p_tenant_id IS NULL OR btrim(p_tenant_id) = '' THEN
        RAISE EXCEPTION 'El tenant autorizado es obligatorio.' USING ERRCODE = '22023';
    END IF;

    IF p_emisor_id IS NULL OR p_emisor_id = '00000000-0000-0000-0000-000000000000'::uuid THEN
        RAISE EXCEPTION 'El emisor autorizado es obligatorio.' USING ERRCODE = '22023';
    END IF;

    IF p_usuario_id IS NULL OR btrim(p_usuario_id) = '' THEN
        RAISE EXCEPTION 'El usuario autorizado es obligatorio.' USING ERRCODE = '22023';
    END IF;

    IF p_clave_idempotencia IS NULL OR btrim(p_clave_idempotencia) = '' THEN
        RAISE EXCEPTION 'La clave de idempotencia es obligatoria.' USING ERRCODE = '22023';
    END IF;

    v_entrada := p_documento_json::jsonb;
    IF jsonb_typeof(v_entrada) IS DISTINCT FROM 'object' THEN
        RAISE EXCEPTION 'El documento de factura debe ser un objeto JSON.' USING ERRCODE = '22023';
    END IF;

    IF v_entrada ->> 'tenantId' IS DISTINCT FROM p_tenant_id THEN
        RAISE EXCEPTION 'El tenant del documento no coincide con el contexto autorizado.' USING ERRCODE = '22023';
    END IF;

    IF (v_entrada ->> 'emisorId')::uuid IS DISTINCT FROM p_emisor_id THEN
        RAISE EXCEPTION 'El emisor del documento no coincide con el contexto autorizado.' USING ERRCODE = '22023';
    END IF;

    v_huella := v_entrada - ARRAY['id', 'creadaEnUtc', 'creadaPor']::text[];
    v_id := gen_random_uuid();
    v_creada_en_utc := clock_timestamp();
    v_documento := v_entrada || jsonb_build_object(
        'id', v_id,
        'tenantId', p_tenant_id,
        'emisorId', p_emisor_id,
        'creadaPor', p_usuario_id,
        'creadaEnUtc', v_creada_en_utc,
        'claveIdempotencia', p_clave_idempotencia,
        'estado', 'Borrador',
        'totales', NULL
    );

    INSERT INTO public.factura_borrador
    (
        factura_id,
        tenant_id,
        emisor_id,
        usuario_id,
        clave_idempotencia,
        documento,
        huella_solicitud,
        creada_en_utc
    )
    VALUES
    (
        v_id,
        p_tenant_id,
        p_emisor_id,
        p_usuario_id,
        p_clave_idempotencia,
        v_documento,
        v_huella,
        v_creada_en_utc
    )
    ON CONFLICT (tenant_id, clave_idempotencia) DO NOTHING;

    GET DIAGNOSTICS v_filas_insertadas = ROW_COUNT;
    p_conflicto := false;

    IF v_filas_insertadas = 1 THEN
        p_documento_resultado := v_documento::text;
        p_creada := true;
        RETURN;
    END IF;

    SELECT documento, huella_solicitud
    INTO v_documento_existente, v_huella_existente
    FROM public.factura_borrador
    WHERE tenant_id = p_tenant_id
      AND clave_idempotencia = p_clave_idempotencia;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No fue posible resolver la clave de idempotencia concurrente.'
            USING ERRCODE = '40001';
    END IF;

    IF v_huella_existente IS DISTINCT FROM v_huella THEN
        p_documento_resultado := NULL;
        p_creada := false;
        p_conflicto := true;
        RETURN;
    END IF;

    p_documento_resultado := v_documento_existente::text;
    p_creada := false;
END;
$procedure$;
