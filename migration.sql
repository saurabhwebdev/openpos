-- OpenPOS Migration: Add customers, credit_notes, purchase_orders tables

-- Customers table
CREATE TABLE IF NOT EXISTS public.customers (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    name character varying(200) NOT NULL,
    phone character varying(50) DEFAULT '' NOT NULL,
    email character varying(200) DEFAULT '' NOT NULL,
    address text DEFAULT '' NOT NULL,
    city character varying(100) DEFAULT '' NOT NULL,
    state character varying(100) DEFAULT '' NOT NULL,
    pin_code character varying(20) DEFAULT '' NOT NULL,
    gstin character varying(50) DEFAULT '' NOT NULL,
    notes text DEFAULT '' NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL
);
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname='public' AND sequencename='customers_id_seq') THEN
    CREATE SEQUENCE public.customers_id_seq AS integer START WITH 1 INCREMENT BY 1;
  END IF;
END $$;
ALTER SEQUENCE public.customers_id_seq OWNED BY public.customers.id;
ALTER TABLE ONLY public.customers ALTER COLUMN id SET DEFAULT nextval('public.customers_id_seq'::regclass);
DO $$ BEGIN
  ALTER TABLE ONLY public.customers ADD CONSTRAINT customers_pkey PRIMARY KEY (id);
EXCEPTION WHEN others THEN NULL;
END $$;
DO $$ BEGIN
  ALTER TABLE ONLY public.customers ADD CONSTRAINT customers_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- Add customer_id to invoices
ALTER TABLE public.invoices ADD COLUMN IF NOT EXISTS customer_id integer;
DO $$ BEGIN
  ALTER TABLE ONLY public.invoices ADD CONSTRAINT invoices_customer_id_fkey FOREIGN KEY (customer_id) REFERENCES public.customers(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- Credit Notes table
CREATE TABLE IF NOT EXISTS public.credit_notes (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    credit_note_number character varying(50) NOT NULL,
    invoice_id integer NOT NULL,
    customer_name character varying(200) DEFAULT '' NOT NULL,
    customer_id integer,
    subtotal numeric(12,2) DEFAULT 0 NOT NULL,
    tax_amount numeric(12,2) DEFAULT 0 NOT NULL,
    total_amount numeric(12,2) DEFAULT 0 NOT NULL,
    reason text DEFAULT '' NOT NULL,
    status character varying(20) DEFAULT 'ISSUED' NOT NULL,
    created_by integer,
    created_at timestamp without time zone DEFAULT now() NOT NULL
);
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname='public' AND sequencename='credit_notes_id_seq') THEN
    CREATE SEQUENCE public.credit_notes_id_seq AS integer START WITH 1 INCREMENT BY 1;
  END IF;
END $$;
ALTER SEQUENCE public.credit_notes_id_seq OWNED BY public.credit_notes.id;
ALTER TABLE ONLY public.credit_notes ALTER COLUMN id SET DEFAULT nextval('public.credit_notes_id_seq'::regclass);
DO $$ BEGIN
  ALTER TABLE ONLY public.credit_notes ADD CONSTRAINT credit_notes_pkey PRIMARY KEY (id);
EXCEPTION WHEN others THEN NULL;
END $$;
DO $$ BEGIN
  ALTER TABLE ONLY public.credit_notes ADD CONSTRAINT credit_notes_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;
DO $$ BEGIN
  ALTER TABLE ONLY public.credit_notes ADD CONSTRAINT credit_notes_invoice_id_fkey FOREIGN KEY (invoice_id) REFERENCES public.invoices(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- Credit note items
CREATE TABLE IF NOT EXISTS public.credit_note_items (
    id integer NOT NULL,
    credit_note_id integer NOT NULL,
    invoice_item_id integer,
    product_id integer,
    product_name character varying(200) DEFAULT '' NOT NULL,
    quantity numeric(12,2) DEFAULT 0 NOT NULL,
    unit_price numeric(12,2) DEFAULT 0 NOT NULL,
    tax_rate numeric(10,2) DEFAULT 0 NOT NULL,
    tax_amount numeric(12,2) DEFAULT 0 NOT NULL,
    line_total numeric(12,2) DEFAULT 0 NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL
);
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname='public' AND sequencename='credit_note_items_id_seq') THEN
    CREATE SEQUENCE public.credit_note_items_id_seq AS integer START WITH 1 INCREMENT BY 1;
  END IF;
END $$;
ALTER SEQUENCE public.credit_note_items_id_seq OWNED BY public.credit_note_items.id;
ALTER TABLE ONLY public.credit_note_items ALTER COLUMN id SET DEFAULT nextval('public.credit_note_items_id_seq'::regclass);
DO $$ BEGIN
  ALTER TABLE ONLY public.credit_note_items ADD CONSTRAINT credit_note_items_pkey PRIMARY KEY (id);
EXCEPTION WHEN others THEN NULL;
END $$;
DO $$ BEGIN
  ALTER TABLE ONLY public.credit_note_items ADD CONSTRAINT credit_note_items_cn_id_fkey FOREIGN KEY (credit_note_id) REFERENCES public.credit_notes(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- Credit note sequence
CREATE TABLE IF NOT EXISTS public.credit_note_sequences (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    last_sequence integer DEFAULT 0 NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL
);
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname='public' AND sequencename='credit_note_sequences_id_seq') THEN
    CREATE SEQUENCE public.credit_note_sequences_id_seq AS integer START WITH 1 INCREMENT BY 1;
  END IF;
END $$;
ALTER SEQUENCE public.credit_note_sequences_id_seq OWNED BY public.credit_note_sequences.id;
ALTER TABLE ONLY public.credit_note_sequences ALTER COLUMN id SET DEFAULT nextval('public.credit_note_sequences_id_seq'::regclass);
DO $$ BEGIN
  ALTER TABLE ONLY public.credit_note_sequences ADD CONSTRAINT credit_note_sequences_pkey PRIMARY KEY (id);
EXCEPTION WHEN others THEN NULL;
END $$;
DO $$ BEGIN
  ALTER TABLE ONLY public.credit_note_sequences ADD CONSTRAINT cn_seq_tenant_key UNIQUE (tenant_id);
EXCEPTION WHEN others THEN NULL;
END $$;

-- Purchase Orders
CREATE TABLE IF NOT EXISTS public.purchase_orders (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    po_number character varying(50) NOT NULL,
    supplier_id integer,
    supplier_name character varying(255) DEFAULT '' NOT NULL,
    subtotal numeric(12,2) DEFAULT 0 NOT NULL,
    tax_amount numeric(12,2) DEFAULT 0 NOT NULL,
    total_amount numeric(12,2) DEFAULT 0 NOT NULL,
    status character varying(20) DEFAULT 'DRAFT' NOT NULL,
    notes text DEFAULT '' NOT NULL,
    expected_date timestamp without time zone,
    created_by integer,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL
);
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname='public' AND sequencename='purchase_orders_id_seq') THEN
    CREATE SEQUENCE public.purchase_orders_id_seq AS integer START WITH 1 INCREMENT BY 1;
  END IF;
END $$;
ALTER SEQUENCE public.purchase_orders_id_seq OWNED BY public.purchase_orders.id;
ALTER TABLE ONLY public.purchase_orders ALTER COLUMN id SET DEFAULT nextval('public.purchase_orders_id_seq'::regclass);
DO $$ BEGIN
  ALTER TABLE ONLY public.purchase_orders ADD CONSTRAINT purchase_orders_pkey PRIMARY KEY (id);
EXCEPTION WHEN others THEN NULL;
END $$;
DO $$ BEGIN
  ALTER TABLE ONLY public.purchase_orders ADD CONSTRAINT purchase_orders_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- Purchase Order Items
CREATE TABLE IF NOT EXISTS public.purchase_order_items (
    id integer NOT NULL,
    purchase_order_id integer NOT NULL,
    product_id integer,
    product_name character varying(200) DEFAULT '' NOT NULL,
    quantity numeric(12,2) DEFAULT 0 NOT NULL,
    unit_price numeric(12,2) DEFAULT 0 NOT NULL,
    tax_rate numeric(10,2) DEFAULT 0 NOT NULL,
    tax_amount numeric(12,2) DEFAULT 0 NOT NULL,
    line_total numeric(12,2) DEFAULT 0 NOT NULL,
    received_quantity numeric(12,2) DEFAULT 0 NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL
);
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname='public' AND sequencename='purchase_order_items_id_seq') THEN
    CREATE SEQUENCE public.purchase_order_items_id_seq AS integer START WITH 1 INCREMENT BY 1;
  END IF;
END $$;
ALTER SEQUENCE public.purchase_order_items_id_seq OWNED BY public.purchase_order_items.id;
ALTER TABLE ONLY public.purchase_order_items ALTER COLUMN id SET DEFAULT nextval('public.purchase_order_items_id_seq'::regclass);
DO $$ BEGIN
  ALTER TABLE ONLY public.purchase_order_items ADD CONSTRAINT purchase_order_items_pkey PRIMARY KEY (id);
EXCEPTION WHEN others THEN NULL;
END $$;
DO $$ BEGIN
  ALTER TABLE ONLY public.purchase_order_items ADD CONSTRAINT po_items_po_id_fkey FOREIGN KEY (purchase_order_id) REFERENCES public.purchase_orders(id);
EXCEPTION WHEN duplicate_object THEN NULL;
END $$;

-- PO Sequence
CREATE TABLE IF NOT EXISTS public.po_sequences (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    last_sequence integer DEFAULT 0 NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL
);
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_sequences WHERE schemaname='public' AND sequencename='po_sequences_id_seq') THEN
    CREATE SEQUENCE public.po_sequences_id_seq AS integer START WITH 1 INCREMENT BY 1;
  END IF;
END $$;
ALTER SEQUENCE public.po_sequences_id_seq OWNED BY public.po_sequences.id;
ALTER TABLE ONLY public.po_sequences ALTER COLUMN id SET DEFAULT nextval('public.po_sequences_id_seq'::regclass);
DO $$ BEGIN
  ALTER TABLE ONLY public.po_sequences ADD CONSTRAINT po_sequences_pkey PRIMARY KEY (id);
EXCEPTION WHEN others THEN NULL;
END $$;
DO $$ BEGIN
  ALTER TABLE ONLY public.po_sequences ADD CONSTRAINT po_sequences_tenant_id_key UNIQUE (tenant_id);
EXCEPTION WHEN others THEN NULL;
END $$;
