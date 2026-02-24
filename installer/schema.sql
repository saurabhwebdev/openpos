-- OpenPOS Database Schema
-- Run this against a fresh PostgreSQL database

SET statement_timeout = 0;
SET lock_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;

SET default_tablespace = '';
SET default_table_access_method = heap;

-- ============================================
-- TABLES
-- ============================================

CREATE TABLE public.roles (
    id integer NOT NULL,
    name character varying(50) NOT NULL
);
CREATE SEQUENCE public.roles_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.roles_id_seq OWNED BY public.roles.id;
ALTER TABLE ONLY public.roles ALTER COLUMN id SET DEFAULT nextval('public.roles_id_seq'::regclass);

CREATE TABLE public.users (
    id integer NOT NULL,
    full_name character varying(200) NOT NULL,
    email character varying(200) NOT NULL,
    password_hash character varying(200) NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.users_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.users_id_seq OWNED BY public.users.id;
ALTER TABLE ONLY public.users ALTER COLUMN id SET DEFAULT nextval('public.users_id_seq'::regclass);

CREATE TABLE public.tenants (
    id integer NOT NULL,
    name character varying(200) NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.tenants_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.tenants_id_seq OWNED BY public.tenants.id;
ALTER TABLE ONLY public.tenants ALTER COLUMN id SET DEFAULT nextval('public.tenants_id_seq'::regclass);

CREATE TABLE public.user_tenants (
    id integer NOT NULL,
    user_id integer NOT NULL,
    tenant_id integer NOT NULL,
    role_id integer NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.user_tenants_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.user_tenants_id_seq OWNED BY public.user_tenants.id;
ALTER TABLE ONLY public.user_tenants ALTER COLUMN id SET DEFAULT nextval('public.user_tenants_id_seq'::regclass);

CREATE TABLE public.modules (
    id integer NOT NULL,
    name character varying(100) NOT NULL,
    key character varying(100) NOT NULL,
    icon character varying(100),
    sort_order integer DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.modules_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.modules_id_seq OWNED BY public.modules.id;
ALTER TABLE ONLY public.modules ALTER COLUMN id SET DEFAULT nextval('public.modules_id_seq'::regclass);

CREATE TABLE public.role_permissions (
    id integer NOT NULL,
    role_id integer NOT NULL,
    module_id integer NOT NULL,
    tenant_id integer NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.role_permissions_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.role_permissions_id_seq OWNED BY public.role_permissions.id;
ALTER TABLE ONLY public.role_permissions ALTER COLUMN id SET DEFAULT nextval('public.role_permissions_id_seq'::regclass);

CREATE TABLE public.business_details (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    business_name character varying(200) DEFAULT '' NOT NULL,
    business_type character varying(100) DEFAULT '' NOT NULL,
    owner_name character varying(200) DEFAULT '' NOT NULL,
    email character varying(200) DEFAULT '' NOT NULL,
    phone character varying(50) DEFAULT '' NOT NULL,
    website character varying(200) DEFAULT '' NOT NULL,
    address_line1 character varying(200) DEFAULT '' NOT NULL,
    address_line2 character varying(200) DEFAULT '' NOT NULL,
    city character varying(100) DEFAULT '' NOT NULL,
    state character varying(100) DEFAULT '' NOT NULL,
    country character varying(100) DEFAULT 'India' NOT NULL,
    postal_code character varying(20) DEFAULT '' NOT NULL,
    gstin character varying(50) DEFAULT '' NOT NULL,
    pan character varying(50) DEFAULT '' NOT NULL,
    business_reg_no character varying(100) DEFAULT '' NOT NULL,
    currency_code character varying(10) DEFAULT 'INR' NOT NULL,
    currency_symbol character varying(10) DEFAULT '₹' NOT NULL,
    bank_name character varying(200) DEFAULT '' NOT NULL,
    bank_branch character varying(200) DEFAULT '' NOT NULL,
    bank_account_no character varying(100) DEFAULT '' NOT NULL,
    bank_ifsc character varying(50) DEFAULT '' NOT NULL,
    bank_account_holder character varying(200) DEFAULT '' NOT NULL,
    upi_id character varying(200) DEFAULT '' NOT NULL,
    upi_name character varying(200) DEFAULT '' NOT NULL,
    invoice_prefix character varying(50) DEFAULT '' NOT NULL,
    invoice_footer text DEFAULT '' NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.business_details_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.business_details_id_seq OWNED BY public.business_details.id;
ALTER TABLE ONLY public.business_details ALTER COLUMN id SET DEFAULT nextval('public.business_details_id_seq'::regclass);

CREATE TABLE public.categories (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    name character varying(200) NOT NULL,
    description text DEFAULT '' NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.categories_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.categories_id_seq OWNED BY public.categories.id;
ALTER TABLE ONLY public.categories ALTER COLUMN id SET DEFAULT nextval('public.categories_id_seq'::regclass);

CREATE TABLE public.units (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    name character varying(100) NOT NULL,
    short_name character varying(20) NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.units_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.units_id_seq OWNED BY public.units.id;
ALTER TABLE ONLY public.units ALTER COLUMN id SET DEFAULT nextval('public.units_id_seq'::regclass);

CREATE TABLE public.suppliers (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    name character varying(255) NOT NULL,
    contact_person character varying(255) DEFAULT '' NOT NULL,
    email character varying(255) DEFAULT '' NOT NULL,
    phone character varying(50) DEFAULT '' NOT NULL,
    address text DEFAULT '' NOT NULL,
    city character varying(100) DEFAULT '' NOT NULL,
    state character varying(100) DEFAULT '' NOT NULL,
    pin_code character varying(20) DEFAULT '' NOT NULL,
    gst_number character varying(50) DEFAULT '' NOT NULL,
    notes text DEFAULT '' NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT now(),
    updated_at timestamp without time zone DEFAULT now()
);
CREATE SEQUENCE public.suppliers_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.suppliers_id_seq OWNED BY public.suppliers.id;
ALTER TABLE ONLY public.suppliers ALTER COLUMN id SET DEFAULT nextval('public.suppliers_id_seq'::regclass);

CREATE TABLE public.tax_slabs (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    country character varying(100) DEFAULT '' NOT NULL,
    tax_name character varying(100) DEFAULT '' NOT NULL,
    tax_type character varying(50) DEFAULT '' NOT NULL,
    rate numeric(10,2) DEFAULT 0 NOT NULL,
    component1_name character varying(100) DEFAULT '' NOT NULL,
    component1_rate numeric(10,2),
    component2_name character varying(100) DEFAULT '' NOT NULL,
    component2_rate numeric(10,2),
    description text DEFAULT '' NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    is_default boolean DEFAULT false NOT NULL,
    sort_order integer DEFAULT 0 NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.tax_slabs_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.tax_slabs_id_seq OWNED BY public.tax_slabs.id;
ALTER TABLE ONLY public.tax_slabs ALTER COLUMN id SET DEFAULT nextval('public.tax_slabs_id_seq'::regclass);

CREATE TABLE public.products (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    category_id integer,
    tax_slab_id integer,
    unit_id integer,
    supplier_id integer,
    name character varying(200) NOT NULL,
    description text DEFAULT '' NOT NULL,
    sku character varying(100) DEFAULT '' NOT NULL,
    barcode character varying(100) DEFAULT '' NOT NULL,
    hsn_code character varying(50) DEFAULT '' NOT NULL,
    cost_price numeric(12,2) DEFAULT 0 NOT NULL,
    selling_price numeric(12,2) DEFAULT 0 NOT NULL,
    mrp numeric(12,2) DEFAULT 0 NOT NULL,
    current_stock numeric(12,2) DEFAULT 0 NOT NULL,
    min_stock_level numeric(12,2) DEFAULT 0 NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.products_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.products_id_seq OWNED BY public.products.id;
ALTER TABLE ONLY public.products ALTER COLUMN id SET DEFAULT nextval('public.products_id_seq'::regclass);

CREATE TABLE public.invoices (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    invoice_number character varying(50) NOT NULL,
    customer_name character varying(200) DEFAULT '' NOT NULL,
    subtotal numeric(12,2) DEFAULT 0 NOT NULL,
    discount_type character varying(20) DEFAULT 'FIXED' NOT NULL,
    discount_value numeric(12,2) DEFAULT 0 NOT NULL,
    discount_amount numeric(12,2) DEFAULT 0 NOT NULL,
    tax_amount numeric(12,2) DEFAULT 0 NOT NULL,
    total_amount numeric(12,2) DEFAULT 0 NOT NULL,
    payment_method character varying(20) DEFAULT 'CASH' NOT NULL,
    amount_tendered numeric(12,2),
    change_given numeric(12,2),
    status character varying(20) DEFAULT 'COMPLETED' NOT NULL,
    notes text DEFAULT '' NOT NULL,
    created_by integer,
    created_at timestamp without time zone DEFAULT now() NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.invoices_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.invoices_id_seq OWNED BY public.invoices.id;
ALTER TABLE ONLY public.invoices ALTER COLUMN id SET DEFAULT nextval('public.invoices_id_seq'::regclass);

CREATE TABLE public.invoice_items (
    id integer NOT NULL,
    invoice_id integer NOT NULL,
    product_id integer,
    product_name character varying(200) DEFAULT '' NOT NULL,
    quantity numeric(12,2) DEFAULT 0 NOT NULL,
    unit_price numeric(12,2) DEFAULT 0 NOT NULL,
    tax_slab_id integer,
    tax_rate numeric(10,2) DEFAULT 0 NOT NULL,
    tax_amount numeric(12,2) DEFAULT 0 NOT NULL,
    line_total numeric(12,2) DEFAULT 0 NOT NULL,
    hsn_code character varying(50) DEFAULT '' NOT NULL,
    created_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.invoice_items_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.invoice_items_id_seq OWNED BY public.invoice_items.id;
ALTER TABLE ONLY public.invoice_items ALTER COLUMN id SET DEFAULT nextval('public.invoice_items_id_seq'::regclass);

CREATE TABLE public.invoice_sequences (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    last_sequence integer DEFAULT 0 NOT NULL,
    updated_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.invoice_sequences_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.invoice_sequences_id_seq OWNED BY public.invoice_sequences.id;
ALTER TABLE ONLY public.invoice_sequences ALTER COLUMN id SET DEFAULT nextval('public.invoice_sequences_id_seq'::regclass);

CREATE TABLE public.stock_movements (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    product_id integer NOT NULL,
    movement_type character varying(20) NOT NULL,
    quantity numeric(12,2) DEFAULT 0 NOT NULL,
    previous_stock numeric(12,2) DEFAULT 0 NOT NULL,
    new_stock numeric(12,2) DEFAULT 0 NOT NULL,
    reference character varying(200) DEFAULT '' NOT NULL,
    notes text DEFAULT '' NOT NULL,
    created_by integer,
    created_at timestamp without time zone DEFAULT now() NOT NULL
);
CREATE SEQUENCE public.stock_movements_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.stock_movements_id_seq OWNED BY public.stock_movements.id;
ALTER TABLE ONLY public.stock_movements ALTER COLUMN id SET DEFAULT nextval('public.stock_movements_id_seq'::regclass);

CREATE TABLE public.email_settings (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    provider_name character varying(100) DEFAULT '' NOT NULL,
    smtp_host character varying(255) DEFAULT '' NOT NULL,
    smtp_port integer DEFAULT 587 NOT NULL,
    use_ssl boolean DEFAULT true NOT NULL,
    sender_email character varying(255) DEFAULT '' NOT NULL,
    sender_name character varying(255) DEFAULT '' NOT NULL,
    password character varying(500) DEFAULT '' NOT NULL,
    is_active boolean DEFAULT true NOT NULL,
    created_at timestamp without time zone DEFAULT now(),
    updated_at timestamp without time zone DEFAULT now()
);
CREATE SEQUENCE public.email_settings_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.email_settings_id_seq OWNED BY public.email_settings.id;
ALTER TABLE ONLY public.email_settings ALTER COLUMN id SET DEFAULT nextval('public.email_settings_id_seq'::regclass);

CREATE TABLE public.payment_gateway_settings (
    id integer NOT NULL,
    tenant_id integer NOT NULL,
    gateway_name character varying(100) DEFAULT '' NOT NULL,
    api_key character varying(500) DEFAULT '' NOT NULL,
    api_secret character varying(500) DEFAULT '' NOT NULL,
    merchant_id character varying(255) DEFAULT '' NOT NULL,
    webhook_secret character varying(500) DEFAULT '' NOT NULL,
    is_test_mode boolean DEFAULT true NOT NULL,
    is_active boolean DEFAULT false NOT NULL,
    currency character varying(10) DEFAULT 'INR' NOT NULL,
    created_at timestamp without time zone DEFAULT now(),
    updated_at timestamp without time zone DEFAULT now()
);
CREATE SEQUENCE public.payment_gateway_settings_id_seq AS integer START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
ALTER SEQUENCE public.payment_gateway_settings_id_seq OWNED BY public.payment_gateway_settings.id;
ALTER TABLE ONLY public.payment_gateway_settings ALTER COLUMN id SET DEFAULT nextval('public.payment_gateway_settings_id_seq'::regclass);

-- ============================================
-- PRIMARY KEYS
-- ============================================

ALTER TABLE ONLY public.roles ADD CONSTRAINT roles_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.users ADD CONSTRAINT users_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.tenants ADD CONSTRAINT tenants_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.user_tenants ADD CONSTRAINT user_tenants_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.modules ADD CONSTRAINT modules_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.role_permissions ADD CONSTRAINT role_permissions_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.business_details ADD CONSTRAINT business_details_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.categories ADD CONSTRAINT categories_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.units ADD CONSTRAINT units_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.suppliers ADD CONSTRAINT suppliers_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.tax_slabs ADD CONSTRAINT tax_slabs_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.products ADD CONSTRAINT products_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.invoices ADD CONSTRAINT invoices_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.invoice_items ADD CONSTRAINT invoice_items_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.invoice_sequences ADD CONSTRAINT invoice_sequences_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.stock_movements ADD CONSTRAINT stock_movements_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.email_settings ADD CONSTRAINT email_settings_pkey PRIMARY KEY (id);
ALTER TABLE ONLY public.payment_gateway_settings ADD CONSTRAINT payment_gateway_settings_pkey PRIMARY KEY (id);

-- ============================================
-- UNIQUE CONSTRAINTS
-- ============================================

ALTER TABLE ONLY public.roles ADD CONSTRAINT roles_name_key UNIQUE (name);
ALTER TABLE ONLY public.users ADD CONSTRAINT users_email_key UNIQUE (email);
ALTER TABLE ONLY public.modules ADD CONSTRAINT modules_key_key UNIQUE (key);
ALTER TABLE ONLY public.role_permissions ADD CONSTRAINT role_permissions_role_id_module_id_tenant_id_key UNIQUE (role_id, module_id, tenant_id);
ALTER TABLE ONLY public.invoice_sequences ADD CONSTRAINT invoice_sequences_tenant_id_key UNIQUE (tenant_id);
ALTER TABLE ONLY public.email_settings ADD CONSTRAINT email_settings_tenant_id_key UNIQUE (tenant_id);
ALTER TABLE ONLY public.payment_gateway_settings ADD CONSTRAINT payment_gateway_settings_tenant_id_key UNIQUE (tenant_id);

-- ============================================
-- FOREIGN KEYS
-- ============================================

ALTER TABLE ONLY public.user_tenants ADD CONSTRAINT user_tenants_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(id);
ALTER TABLE ONLY public.user_tenants ADD CONSTRAINT user_tenants_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
ALTER TABLE ONLY public.user_tenants ADD CONSTRAINT user_tenants_role_id_fkey FOREIGN KEY (role_id) REFERENCES public.roles(id);
ALTER TABLE ONLY public.role_permissions ADD CONSTRAINT role_permissions_role_id_fkey FOREIGN KEY (role_id) REFERENCES public.roles(id);
ALTER TABLE ONLY public.role_permissions ADD CONSTRAINT role_permissions_module_id_fkey FOREIGN KEY (module_id) REFERENCES public.modules(id);
ALTER TABLE ONLY public.role_permissions ADD CONSTRAINT role_permissions_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
ALTER TABLE ONLY public.business_details ADD CONSTRAINT business_details_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
ALTER TABLE ONLY public.categories ADD CONSTRAINT categories_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
ALTER TABLE ONLY public.units ADD CONSTRAINT units_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
ALTER TABLE ONLY public.suppliers ADD CONSTRAINT suppliers_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
ALTER TABLE ONLY public.tax_slabs ADD CONSTRAINT tax_slabs_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
ALTER TABLE ONLY public.products ADD CONSTRAINT products_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
ALTER TABLE ONLY public.products ADD CONSTRAINT products_category_id_fkey FOREIGN KEY (category_id) REFERENCES public.categories(id);
ALTER TABLE ONLY public.products ADD CONSTRAINT products_tax_slab_id_fkey FOREIGN KEY (tax_slab_id) REFERENCES public.tax_slabs(id);
ALTER TABLE ONLY public.products ADD CONSTRAINT products_unit_id_fkey FOREIGN KEY (unit_id) REFERENCES public.units(id);
ALTER TABLE ONLY public.products ADD CONSTRAINT products_supplier_id_fkey FOREIGN KEY (supplier_id) REFERENCES public.suppliers(id);
ALTER TABLE ONLY public.invoices ADD CONSTRAINT invoices_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
ALTER TABLE ONLY public.invoices ADD CONSTRAINT invoices_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(id);
ALTER TABLE ONLY public.invoice_items ADD CONSTRAINT invoice_items_invoice_id_fkey FOREIGN KEY (invoice_id) REFERENCES public.invoices(id);
ALTER TABLE ONLY public.invoice_items ADD CONSTRAINT invoice_items_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(id);
ALTER TABLE ONLY public.invoice_items ADD CONSTRAINT invoice_items_tax_slab_id_fkey FOREIGN KEY (tax_slab_id) REFERENCES public.tax_slabs(id);
ALTER TABLE ONLY public.invoice_sequences ADD CONSTRAINT invoice_sequences_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
ALTER TABLE ONLY public.stock_movements ADD CONSTRAINT stock_movements_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
ALTER TABLE ONLY public.stock_movements ADD CONSTRAINT stock_movements_product_id_fkey FOREIGN KEY (product_id) REFERENCES public.products(id);
ALTER TABLE ONLY public.stock_movements ADD CONSTRAINT stock_movements_created_by_fkey FOREIGN KEY (created_by) REFERENCES public.users(id);
ALTER TABLE ONLY public.email_settings ADD CONSTRAINT email_settings_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
ALTER TABLE ONLY public.payment_gateway_settings ADD CONSTRAINT payment_gateway_settings_tenant_id_fkey FOREIGN KEY (tenant_id) REFERENCES public.tenants(id);
