-- Ejecutar una sola vez en la base del emisor, antes de crear los SP.
-- Los detalles, impuestos, ajustes y pagos se conservan dentro de documento.
CREATE TABLE public.factura_borrador
(
    factura_id uuid PRIMARY KEY,
    tenant_id text NOT NULL,
    emisor_id uuid NOT NULL,
    usuario_id text NOT NULL,
    clave_idempotencia text NOT NULL,
    documento jsonb NOT NULL,
    huella_solicitud jsonb NOT NULL,
    creada_en_utc timestamptz NOT NULL,
    CONSTRAINT uq_factura_borrador_tenant_idempotencia
        UNIQUE (tenant_id, clave_idempotencia),
    CONSTRAINT ck_factura_borrador_documento_objeto
        CHECK (jsonb_typeof(documento) = 'object'),
    CONSTRAINT ck_factura_borrador_huella_objeto
        CHECK (jsonb_typeof(huella_solicitud) = 'object')
);

CREATE INDEX ix_factura_borrador_tenant_emisor_creada
    ON public.factura_borrador (tenant_id, emisor_id, creada_en_utc DESC);
