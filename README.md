<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 9" />
  <img src="https://img.shields.io/badge/WPF-Desktop-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="WPF" />
  <img src="https://img.shields.io/badge/PostgreSQL-18-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" alt="PostgreSQL" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="MIT License" />
</p>

<h1 align="center">FreePOS</h1>

<p align="center">
  <strong>A modern, full-featured Point of Sale system for Windows</strong>
  <br />
  Built with .NET 9 WPF, Material Design, and PostgreSQL
  <br /><br />
  <a href="#features">Features</a> &middot;
  <a href="#getting-started">Getting Started</a> &middot;
  <a href="#architecture">Architecture</a> &middot;
  <a href="#database-schema">Database Schema</a> &middot;
  <a href="#installation">Installation</a>
</p>

---

## Overview

FreePOS is a desktop Point of Sale application designed for retail businesses, shops, and small enterprises. It provides a complete business management solution including sales processing, inventory management, supplier tracking, tax compliance, PDF reporting, email integration, and Excel data import/export.

The application follows a multi-tenant architecture, allowing multiple businesses to operate on the same installation with complete data isolation.

---

## Features

### Point of Sale (POS)
- **Product Search & Selection** &mdash; Search products by name, SKU, or barcode with real-time filtering
- **Category Navigation** &mdash; Pill-shaped category buttons with horizontal scrolling for quick product filtering
- **Cart Management** &mdash; Add, update quantity, and remove items with real-time total calculation
- **Customer Name** &mdash; Optional customer name field per transaction
- **Discount Support** &mdash; Apply fixed amount or percentage-based discounts
- **Tax Calculation** &mdash; Automatic tax computation using configurable tax slabs (GST, VAT, etc.) with component-level breakdown (CGST/SGST)
- **Multiple Payment Methods** &mdash; Cash (with tendered/change calculation), UPI, and Card payments
- **UPI QR Code** &mdash; Auto-generated UPI payment QR code with pre-filled amount at checkout
- **Hold & Resume** &mdash; Hold current transactions and resume them later
- **Invoice Generation** &mdash; Automatic invoice number generation with configurable prefix

### Invoice Management
- **Invoice List** &mdash; View all invoices with search, status filtering, and date range selection
- **Invoice Details** &mdash; Detailed view with line items, tax breakdown, and payment information
- **PDF Export** &mdash; Generate professional PDF invoices with business branding
- **Invoice PDF Includes:**
  - Business header with logo, address, GSTIN, and contact info
  - Itemized table with HSN codes, quantities, prices, and tax breakdown
  - Component-level tax breakdown (e.g., CGST 9% + SGST 9%)
  - Bank transfer details (account holder, account number, bank, branch, IFSC)
  - UPI QR code with pre-filled payment amount
  - Configurable invoice footer
- **Email Invoice** &mdash; Send invoice PDF directly via email
- **WhatsApp Sharing** &mdash; Share invoice via WhatsApp with pre-filled message

### Inventory Management

#### Products
- **Full CRUD** &mdash; Create, read, update, and delete products
- **Rich Product Data** &mdash; Name, SKU, barcode, HSN code, description, category, unit, supplier, tax slab
- **Pricing** &mdash; Cost price, selling price, and MRP tracking
- **Stock Management** &mdash; Current stock levels with minimum stock threshold alerts
- **Search** &mdash; Real-time search by name, SKU, or barcode
- **Detail View** &mdash; Click any product row to see complete product information
- **Excel Import** &mdash; Bulk import products from Excel files (.xlsx)

#### Categories
- **Organize Products** &mdash; Create categories with name, description, and sort order
- **Active/Inactive** &mdash; Toggle category visibility
- **Excel Import** &mdash; Bulk import categories from Excel files

#### Units
- **Measurement Units** &mdash; Define units of measurement (e.g., Kg, Pcs, Ltr) with short names
- **Auto-seeded** &mdash; Common units are pre-populated on first use

#### Suppliers
- **Supplier Directory** &mdash; Full supplier management with company info, contact details, and GST number
- **Address Management** &mdash; Complete address fields (street, city, state, PIN code)
- **Search** &mdash; Filter suppliers by name, contact, phone, GST, or city
- **Excel Import** &mdash; Bulk import suppliers from Excel files

### Reports & Analytics

#### Report Types
| Report | Description |
|--------|-------------|
| **Daily Sales** | All transactions for a specific date with revenue, tax, and discount totals |
| **Sales Summary** | Aggregated sales data for a date range with payment method breakdown |
| **Product Sales** | Product-wise sales analysis showing quantity sold, revenue, and tax collected |
| **Inventory Report** | Complete stock overview with values across all products (landscape format) |
| **Low Stock Alert** | Products below minimum stock threshold with deficit calculations |
| **Tax Collection** | Tax-wise collection report grouped by tax slab |
| **Consolidated Report** | All-in-one business report combining all above sections plus individual invoice copies |

#### Report Features
- **Date Range Selection** &mdash; Custom date pickers with quick selectors (Today, This Week, This Month)
- **PDF Export** &mdash; All reports generate as professionally formatted PDF documents
- **Email Reports** &mdash; Send any report as a PDF email attachment
- **Auto-open** &mdash; Generated PDFs open automatically in the default viewer
- **Download to ~/Downloads** &mdash; All reports save to the user's Downloads folder

### Settings

#### General Settings
- **Invoice Prefix** &mdash; Configurable invoice number prefix (e.g., "INV-", "BILL-")
- **Invoice Footer** &mdash; Custom text printed at the bottom of every invoice

#### Business Details
- **Company Profile** &mdash; Business name, type, owner name
- **Contact Information** &mdash; Email, phone, website
- **Address** &mdash; Full business address (street, city, state, country, postal code)
- **Tax Registration** &mdash; GSTIN, PAN, business registration number
- **Currency** &mdash; Configurable currency code and symbol (INR, USD, EUR, GBP, etc.)
- **Banking** &mdash; Bank account details for invoice payment info (account holder, number, bank, branch, IFSC)
- **UPI** &mdash; UPI ID and display name for QR code generation on invoices

#### Tax Configuration
- **Tax Slabs** &mdash; Create tax slabs with name, rate, and type
- **Component Taxes** &mdash; Support for split taxes (e.g., GST split into CGST + SGST)
- **Country-specific** &mdash; Tax slabs scoped by country
- **Default Slabs** &mdash; Pre-seeded with Indian GST slabs (0%, 5%, 12%, 18%, 28%)

#### Roles & Access Control
- **Role Management** &mdash; Admin and Cashier roles out of the box
- **Module Permissions** &mdash; Granular access control per module (POS, Inventory, Invoices, Reports, Settings)
- **User Management** &mdash; Manage users, assign roles, activate/deactivate accounts

#### Email Settings
- **Provider Presets** &mdash; One-click setup for Gmail, Outlook, Yahoo
- **Custom SMTP** &mdash; Full SMTP configuration (host, port, SSL/TLS)
- **Sender Configuration** &mdash; Sender name, email, and app password
- **Test Email** &mdash; Send a test email to verify configuration
- **Enable/Disable** &mdash; Toggle email service on or off

#### Payment Gateway Settings
- **Multiple Gateways** &mdash; Support for Stripe, PayPal, Razorpay, Paytm, PhonePe, Square, Instamojo, Cashfree
- **API Credentials** &mdash; API key, secret, merchant ID, webhook secret
- **Test/Live Mode** &mdash; Toggle between sandbox and production environments
- **Currency Selection** &mdash; Gateway-specific currency configuration

### Excel Import
- **Products** &mdash; Columns: Name, SKU, Barcode, HSNCode, Category, CostPrice, SellingPrice, MRP, CurrentStock, MinStockLevel, Description
- **Categories** &mdash; Columns: Name, Description
- **Suppliers** &mdash; Columns: Name, ContactPerson, Email, Phone, Address, City, State, PinCode, GSTNumber
- **Duplicate Detection** &mdash; Automatically skips existing records (matched by name or SKU)
- **Category Matching** &mdash; Products are linked to categories by name (case-insensitive)
- **Error Handling** &mdash; Reports imported count, skipped count, and any errors

### Authentication
- **User Registration** &mdash; New users register with name, email, and password
- **Secure Login** &mdash; BCrypt password hashing
- **Tenant Creation** &mdash; Auto-creates a business tenant on registration
- **Role Assignment** &mdash; First user gets Admin role with full access
- **Session Management** &mdash; Static session tracking for current user, tenant, and permissions

---

## Tech Stack

| Component | Technology |
|-----------|------------|
| **Framework** | .NET 9.0 (Windows) |
| **UI** | WPF (Windows Presentation Foundation) |
| **Design System** | Material Design In XAML Toolkit 5.3.0 |
| **Database** | PostgreSQL 18 |
| **ORM** | Dapper 2.1.66 |
| **PDF Generation** | QuestPDF 2026.2.1 (Community License) |
| **QR Codes** | QRCoder 1.7.0 |
| **Excel Import** | ClosedXML 0.104.2 |
| **Password Hashing** | BCrypt.Net-Next 4.1.0 |
| **DB Driver** | Npgsql 10.0.1 |
| **Configuration** | Microsoft.Extensions.Configuration.Json 10.0.3 |
| **Installer** | Inno Setup 6 |

---

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 18](https://www.postgresql.org/download/) (or 17+)
- Windows 10/11

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/saurabhwebdev/freepos.git
   cd freepos
   ```

2. **Create the database**
   ```bash
   psql -U postgres -c "CREATE DATABASE mywinformsapp_db;"
   psql -U postgres -d mywinformsapp_db -f installer/schema.sql
   ```

3. **Configure connection string**

   Edit `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=mywinformsapp_db;Username=postgres;Password=postgres"
     }
   }
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Register a new account** &mdash; The first user automatically gets Admin access with a new tenant/business created.

### Building for Production

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

---

## Architecture

### Project Structure

```
freepos-app/
├── Models/                     # Data models (POCO classes for Dapper)
│   ├── Invoice.cs              # Invoice with computed properties
│   ├── InvoiceItem.cs          # Individual line items
│   ├── Product.cs              # Product with stock tracking
│   ├── Category.cs             # Product categories
│   ├── Supplier.cs             # Supplier directory
│   ├── TaxSlab.cs              # Tax configuration
│   ├── Unit.cs                 # Units of measurement
│   ├── BusinessDetails.cs      # Business profile + banking
│   ├── EmailSettings.cs        # SMTP configuration
│   ├── PaymentGatewaySettings.cs # Payment gateway config
│   └── ...                     # User, Tenant, Role, Module models
│
├── Services/                   # Business logic layer
│   ├── SalesService.cs         # POS transactions, invoicing
│   ├── InventoryService.cs     # Products, categories, units, suppliers
│   ├── ReportService.cs        # Report data queries
│   ├── PdfExportService.cs     # PDF generation (QuestPDF)
│   ├── EmailService.cs         # SMTP email sending
│   ├── ExcelImportService.cs   # Excel file import (ClosedXML)
│   ├── BusinessService.cs      # Business details CRUD
│   ├── TaxService.cs           # Tax slab management
│   ├── PaymentGatewayService.cs # Payment gateway config
│   └── AuthService.cs          # Authentication
│
├── Views/                      # WPF UserControls and Windows
│   ├── PosView.xaml/.cs        # Point of Sale screen
│   ├── InvoicesView.xaml/.cs   # Invoice listing and management
│   ├── ReceiptView.xaml/.cs    # Receipt display with share options
│   ├── InventoryView.xaml/.cs  # Inventory tab container
│   ├── InventoryProductsView.xaml/.cs
│   ├── InventoryCategoriesView.xaml/.cs
│   ├── InventorySuppliersView.xaml/.cs
│   ├── DataManagementView.xaml/.cs  # Reports dashboard
│   ├── SettingsView.xaml/.cs   # Settings tab container
│   ├── SettingsEmailView.xaml/.cs
│   ├── SettingsPaymentView.xaml/.cs
│   ├── EmailInputDialog.xaml/.cs
│   ├── PhoneInputDialog.xaml/.cs
│   └── ...
│
├── Helpers/                    # Utilities
│   ├── DatabaseHelper.cs       # Dapper wrapper (QueryAsync, ExecuteAsync)
│   └── Session.cs              # Static session state
│
├── installer/                  # Windows installer files
│   ├── FreePOS.iss             # Inno Setup script
│   ├── schema.sql              # Full database schema
│   ├── setup-db.bat            # Database setup script
│   └── build-installer.ps1     # Build automation
│
├── MainWindow.xaml/.cs         # App shell with sidebar navigation
├── LoginWindow.xaml/.cs        # Login screen
├── RegisterWindow.xaml/.cs     # Registration screen
├── appsettings.json            # Configuration
└── MyWinFormsApp.csproj        # Project file
```

### Multi-Tenant Architecture

All data is scoped by `tenant_id`, enabling complete data isolation between businesses:

```
User ─── registers ──→ Tenant (Business) created
  │                        │
  └── user_tenants ──→ role_id (Admin/Cashier)
                           │
                    ┌──────┴──────┐
                    │  All data   │
                    │  scoped by  │
                    │  tenant_id  │
                    └─────────────┘
                    Products, Categories, Invoices,
                    Suppliers, Tax Slabs, Settings...
```

### Data Flow

```
POS View ──→ SalesService ──→ DatabaseHelper ──→ PostgreSQL
                 │
                 ├── Creates Invoice + InvoiceItems
                 ├── Updates product stock
                 └── Returns Invoice data
                          │
                          ▼
               PdfExportService ──→ QuestPDF ──→ PDF file
                          │
                          ▼
                 EmailService ──→ SMTP ──→ Email with attachment
```

---

## Database Schema

The database consists of 18 tables organized as follows:

### Core Tables

| Table | Description |
|-------|-------------|
| `roles` | User roles (Admin, Cashier) |
| `users` | User accounts with BCrypt-hashed passwords |
| `tenants` | Business/organization entities |
| `user_tenants` | User-tenant-role associations |
| `modules` | Application modules for access control |
| `role_permissions` | Module-level permissions per role per tenant |

### Business Tables

| Table | Description |
|-------|-------------|
| `business_details` | Business profile, address, banking, UPI, invoice settings |
| `categories` | Product categories with sort order |
| `units` | Units of measurement (Kg, Pcs, Ltr, etc.) |
| `suppliers` | Supplier directory with contact and GST info |
| `tax_slabs` | Tax configurations with component-level rates |
| `products` | Product catalog with pricing, stock, and associations |

### Transaction Tables

| Table | Description |
|-------|-------------|
| `invoices` | Sales transactions with payment and discount info |
| `invoice_items` | Line items with product, quantity, price, and tax |

### Configuration Tables

| Table | Description |
|-------|-------------|
| `email_settings` | SMTP configuration per tenant |
| `payment_gateway_settings` | Payment gateway API credentials per tenant |

---

## Installation

### Windows Installer

FreePOS includes an Inno Setup-based Windows installer that bundles everything needed:

1. **Install prerequisites**
   - [Inno Setup 6](https://jrsoftware.org/isinfo.php)
   - .NET 9.0 SDK

2. **Build the installer**
   ```powershell
   cd installer
   .\build-installer.ps1
   ```

3. **The installer handles:**
   - Copies the self-contained application to Program Files
   - Runs database setup (auto-detects PostgreSQL)
   - Creates desktop and Start Menu shortcuts
   - Registers uninstaller

### Manual Database Setup

```bash
# Using the provided script
cd installer
.\setup-db.bat

# Or manually
psql -U postgres -c "CREATE DATABASE mywinformsapp_db;"
psql -U postgres -d mywinformsapp_db -f schema.sql
```

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=mywinformsapp_db;Username=postgres;Password=postgres"
  }
}
```

### Environment-Specific Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Database Host | `localhost` | PostgreSQL server address |
| Database Port | `5432` | PostgreSQL port |
| Database Name | `mywinformsapp_db` | Database name |
| Username | `postgres` | Database user |
| Password | `postgres` | Database password |

---

## PDF Reports

All PDF reports are generated using QuestPDF with consistent professional formatting:

- **A4 page size** (portrait or landscape depending on report)
- **Business branding** at the top of every report
- **Page numbers** with "FreePOS" footer
- **Color-coded data** (red for low stock, blue for headers)
- **Automatic page breaks** for large datasets

### Consolidated Report Sections

The consolidated report combines all business data into a single multi-page PDF:

1. **Sales Summary** &mdash; Revenue, invoices, average order value, payment breakdown
2. **All Invoices** &mdash; Complete invoice list with date, customer, payment, status, amount
3. **Product Sales** &mdash; Product-wise analysis with quantities and revenue
4. **Tax Collection** &mdash; Tax slab-wise collection summary
5. **Inventory Overview** &mdash; Top products by stock value
6. **Low Stock Alert** &mdash; Items below minimum threshold
7. **Invoice Copies** &mdash; Full itemized copy of each invoice

---

## Excel Import Format

### Products
| Column | Required | Description |
|--------|----------|-------------|
| A: Name | Yes | Product name |
| B: SKU | No | Stock Keeping Unit |
| C: Barcode | No | Product barcode |
| D: HSNCode | No | HSN/SAC code for tax |
| E: Category | No | Category name (matched by name) |
| F: CostPrice | No | Purchase/cost price |
| G: SellingPrice | No | Selling price |
| H: MRP | No | Maximum retail price |
| I: CurrentStock | No | Opening stock quantity |
| J: MinStockLevel | No | Minimum stock alert threshold |
| K: Description | No | Product description |

### Categories
| Column | Required | Description |
|--------|----------|-------------|
| A: Name | Yes | Category name |
| B: Description | No | Category description |

### Suppliers
| Column | Required | Description |
|--------|----------|-------------|
| A: Name | Yes | Supplier/company name |
| B: ContactPerson | No | Contact person name |
| C: Email | No | Email address |
| D: Phone | No | Phone number |
| E: Address | No | Street address |
| F: City | No | City |
| G: State | No | State |
| H: PinCode | No | PIN/ZIP code |
| I: GSTNumber | No | GST registration number |

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

<p align="center">
  Built with .NET 9, WPF, and PostgreSQL
  <br />
  <sub>Made by <a href="https://github.com/saurabhwebdev">Saurabh Thakur</a></sub>
</p>
