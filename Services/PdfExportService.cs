using System.IO;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Services;

public static class PdfExportService
{
    static PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static string GetDownloadPath(string businessName, string reportName, DateTime date)
    {
        var sanitized = string.Join("_", businessName.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{sanitized}_{reportName}_{date:yyyy-MM-dd}.pdf";
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, fileName);
    }

    // --- Daily Sales PDF ---
    public static string GenerateDailySalesReport(BusinessDetails biz, List<Invoice> invoices, DateTime date, string currency)
    {
        var path = GetDownloadPath(biz.BusinessName, "DailySales", date);
        var totalRevenue = invoices.Sum(i => i.TotalAmount);
        var totalTax = invoices.Sum(i => i.TaxAmount);
        var totalDiscount = invoices.Sum(i => i.DiscountAmount);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(biz.BusinessName).FontSize(18).Bold();
                    col.Item().Text($"Daily Sales Report - {date:dd MMM yyyy}").FontSize(12).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    // Summary
                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Total Revenue").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{currency}{totalRevenue:N2}").FontSize(16).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Invoices").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{invoices.Count}").FontSize(16).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Tax Collected").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{currency}{totalTax:N2}").FontSize(16).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Discounts").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{currency}{totalDiscount:N2}").FontSize(16).Bold();
                        });
                    });

                    // Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(2);   // Invoice#
                            cd.RelativeColumn(2);   // Customer
                            cd.RelativeColumn(1.5f);// Time
                            cd.RelativeColumn(1);   // Items
                            cd.RelativeColumn(1);   // Payment
                            cd.RelativeColumn(1.5f);// Amount
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Invoice #").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Customer").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Time").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Items").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Payment").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Amount").Bold().FontSize(9);
                        });

                        foreach (var inv in invoices)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(inv.InvoiceNumber).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(inv.CustomerName).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(inv.CreatedAt.ToString("hh:mm tt")).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(inv.ItemCount.ToString()).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(inv.PaymentMethodDisplay).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{currency}{inv.TotalAmount:N2}").FontSize(9);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span($"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt} | ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span("OpenPOS").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(path);

        return path;
    }

    // --- Sales Summary PDF ---
    public static string GenerateSalesSummaryReport(BusinessDetails biz, ReportService.SalesSummary summary,
        DateTime from, DateTime to, string currency)
    {
        var path = GetDownloadPath(biz.BusinessName, "SalesSummary", from);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(biz.BusinessName).FontSize(18).Bold();
                    col.Item().Text($"Sales Summary Report | {from:dd MMM yyyy} - {to:dd MMM yyyy}").FontSize(12).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    // Key Metrics
                    col.Item().PaddingBottom(15).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Total Revenue").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{currency}{summary.TotalRevenue:N2}").FontSize(18).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Total Invoices").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{summary.TotalInvoices}").FontSize(18).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Avg. Order Value").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{currency}{summary.AvgOrderValue:N2}").FontSize(18).Bold();
                        });
                    });

                    col.Item().PaddingBottom(15).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Tax Collected").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{currency}{summary.TotalTax:N2}").FontSize(14).Bold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Total Discounts").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{currency}{summary.TotalDiscount:N2}").FontSize(14).Bold();
                        });
                    });

                    // Payment Breakdown
                    col.Item().PaddingBottom(5).Text("Payment Method Breakdown").FontSize(12).Bold();
                    col.Item().PaddingBottom(15).Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(1);
                            cd.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Method").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Transactions").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Amount").Bold().FontSize(9);
                        });

                        void AddRow(string method, int count, decimal amount)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(method).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(count.ToString()).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{currency}{amount:N2}").FontSize(9);
                        }

                        AddRow("Cash", summary.CashCount, summary.CashAmount);
                        AddRow("UPI", summary.UpiCount, summary.UpiAmount);
                        AddRow("Card", summary.CardCount, summary.CardAmount);
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span($"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt} | ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span("OpenPOS").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(path);

        return path;
    }

    // --- Product Sales PDF ---
    public static string GenerateProductSalesReport(BusinessDetails biz, List<ReportService.ProductSalesRow> rows,
        DateTime from, DateTime to, string currency)
    {
        var path = GetDownloadPath(biz.BusinessName, "ProductSales", from);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(biz.BusinessName).FontSize(18).Bold();
                    col.Item().Text($"Product Sales Report | {from:dd MMM yyyy} - {to:dd MMM yyyy}").FontSize(12).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(3);   // Product
                        cd.RelativeColumn(1.5f);// SKU
                        cd.RelativeColumn(2);   // Category
                        cd.RelativeColumn(1);   // Qty
                        cd.RelativeColumn(1.5f);// Revenue
                        cd.RelativeColumn(1.5f);// Tax
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Product").Bold().FontSize(9);
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("SKU").Bold().FontSize(9);
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Category").Bold().FontSize(9);
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Qty Sold").Bold().FontSize(9);
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Revenue").Bold().FontSize(9);
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Tax").Bold().FontSize(9);
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(r.ProductName).FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(r.Sku).FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(r.CategoryName).FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{r.QuantitySold:0.##}").FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{currency}{r.Revenue:N2}").FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{currency}{r.TaxCollected:N2}").FontSize(9);
                    }

                    // Totals
                    table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten4).Padding(5).Text("TOTAL").Bold().FontSize(9);
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(5).AlignRight().Text($"{rows.Sum(r => r.QuantitySold):0.##}").Bold().FontSize(9);
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(5).AlignRight().Text($"{currency}{rows.Sum(r => r.Revenue):N2}").Bold().FontSize(9);
                    table.Cell().Background(Colors.Grey.Lighten4).Padding(5).AlignRight().Text($"{currency}{rows.Sum(r => r.TaxCollected):N2}").Bold().FontSize(9);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span($"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt} | ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span("OpenPOS").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(path);

        return path;
    }

    // --- Inventory PDF ---
    public static string GenerateInventoryReport(BusinessDetails biz, List<Product> products, string currency)
    {
        var path = GetDownloadPath(biz.BusinessName, "Inventory", DateTime.Today);
        var totalValue = products.Sum(p => p.CurrentStock * p.CostPrice);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Column(col =>
                {
                    col.Item().Text(biz.BusinessName).FontSize(18).Bold();
                    col.Item().Text($"Inventory Report | {DateTime.Today:dd MMM yyyy} | Total Stock Value: {currency}{totalValue:N2}").FontSize(11).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(3);   // Product
                        cd.RelativeColumn(1.5f);// SKU
                        cd.RelativeColumn(2);   // Category
                        cd.RelativeColumn(2);   // Supplier
                        cd.RelativeColumn(1.2f);// Cost
                        cd.RelativeColumn(1.2f);// Sell
                        cd.RelativeColumn(1);   // Stock
                        cd.RelativeColumn(1);   // Min
                        cd.RelativeColumn(1.5f);// Value
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Product").Bold();
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("SKU").Bold();
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Category").Bold();
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Supplier").Bold();
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Cost").Bold();
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Sell Price").Bold();
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Stock").Bold();
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Min").Bold();
                        h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Value").Bold();
                    });

                    foreach (var p in products)
                    {
                        var isLow = p.CurrentStock <= p.MinStockLevel;
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(p.Name);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(p.Sku);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(p.CategoryName);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(p.SupplierName);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{currency}{p.CostPrice:N2}");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{currency}{p.SellingPrice:N2}");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight()
                            .Text($"{p.CurrentStock:0.##}").FontColor(isLow ? Colors.Red.Medium : Colors.Black);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{p.MinStockLevel:0.##}");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{currency}{p.CurrentStock * p.CostPrice:N2}");
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span($"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt} | ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span("OpenPOS").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(path);

        return path;
    }

    // --- Low Stock PDF ---
    public static string GenerateLowStockReport(BusinessDetails biz, List<Product> products, string currency)
    {
        var path = GetDownloadPath(biz.BusinessName, "LowStock", DateTime.Today);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(biz.BusinessName).FontSize(18).Bold();
                    col.Item().Text($"Low Stock Alert Report | {DateTime.Today:dd MMM yyyy} | {products.Count} items below threshold").FontSize(12).FontColor(Colors.Red.Medium);
                    col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(3);   // Product
                        cd.RelativeColumn(2);   // Category
                        cd.RelativeColumn(2);   // Supplier
                        cd.RelativeColumn(1.2f);// Current
                        cd.RelativeColumn(1.2f);// Min
                        cd.RelativeColumn(1.2f);// Deficit
                    });

                    table.Header(h =>
                    {
                        h.Cell().Background(Colors.Red.Lighten4).Padding(5).Text("Product").Bold().FontSize(9);
                        h.Cell().Background(Colors.Red.Lighten4).Padding(5).Text("Category").Bold().FontSize(9);
                        h.Cell().Background(Colors.Red.Lighten4).Padding(5).Text("Supplier").Bold().FontSize(9);
                        h.Cell().Background(Colors.Red.Lighten4).Padding(5).AlignRight().Text("Current").Bold().FontSize(9);
                        h.Cell().Background(Colors.Red.Lighten4).Padding(5).AlignRight().Text("Minimum").Bold().FontSize(9);
                        h.Cell().Background(Colors.Red.Lighten4).Padding(5).AlignRight().Text("Deficit").Bold().FontSize(9);
                    });

                    foreach (var p in products)
                    {
                        var deficit = p.MinStockLevel - p.CurrentStock;
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(p.Name).FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(p.CategoryName).FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(p.SupplierName).FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{p.CurrentStock:0.##}").FontSize(9).FontColor(Colors.Red.Medium);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{p.MinStockLevel:0.##}").FontSize(9);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{deficit:0.##}").FontSize(9).Bold().FontColor(Colors.Red.Medium);
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span($"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt} | ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span("OpenPOS").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(path);

        return path;
    }

    // --- Individual Invoice PDF ---
    public static string GenerateInvoicePdf(BusinessDetails biz, Invoice invoice, List<InvoiceItem> items,
        Dictionary<int, TaxSlab> taxSlabs, string currency)
    {
        var sanitized = string.Join("_", biz.BusinessName.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{sanitized}_Invoice_{invoice.InvoiceNumber}_{invoice.CreatedAt:yyyy-MM-dd}.pdf";
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, fileName);

        bool hasBankDetails = !string.IsNullOrWhiteSpace(biz.BankAccountNo) && !string.IsNullOrWhiteSpace(biz.BankName);
        bool hasUpi = !string.IsNullOrWhiteSpace(biz.UpiId);

        // Pre-generate QR code bytes so we don't do it inside the layout
        byte[]? qrBytes = null;
        string? upiUri = null;
        if (hasUpi)
        {
            var upiName = !string.IsNullOrWhiteSpace(biz.UpiName) ? biz.UpiName : biz.BusinessName;
            upiUri = $"upi://pay?pa={Uri.EscapeDataString(biz.UpiId)}&pn={Uri.EscapeDataString(upiName)}";
            if (invoice.TotalAmount > 0)
                upiUri += $"&am={invoice.TotalAmount:0.00}&cu=INR";
            try
            {
                using var qrGen = new QRCodeGenerator();
                using var qrData = qrGen.CreateQrCode(upiUri, QRCodeGenerator.ECCLevel.M);
                using var qrCode = new PngByteQRCode(qrData);
                qrBytes = qrCode.GetGraphic(5);
            }
            catch { /* ignore */ }
        }

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30, QuestPDF.Infrastructure.Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(9));

                // ── HEADER ──
                page.Header().PaddingBottom(8).Column(hdr =>
                {
                    hdr.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(biz.BusinessName).FontSize(16).Bold();
                            var addrParts = new List<string>();
                            if (!string.IsNullOrEmpty(biz.AddressLine1)) addrParts.Add(biz.AddressLine1);
                            if (!string.IsNullOrEmpty(biz.City)) addrParts.Add(biz.City);
                            if (!string.IsNullOrEmpty(biz.PostalCode)) addrParts.Add(biz.PostalCode);
                            if (addrParts.Count > 0)
                                c.Item().Text(string.Join(", ", addrParts)).FontSize(8).FontColor(Colors.Grey.Medium);
                            var contactParts = new List<string>();
                            if (!string.IsNullOrEmpty(biz.Phone)) contactParts.Add($"Ph: {biz.Phone}");
                            if (!string.IsNullOrEmpty(biz.Email)) contactParts.Add(biz.Email);
                            if (contactParts.Count > 0)
                                c.Item().Text(string.Join("  |  ", contactParts)).FontSize(8).FontColor(Colors.Grey.Medium);
                            if (!string.IsNullOrEmpty(biz.Gstin))
                                c.Item().Text($"GSTIN: {biz.Gstin}").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(130).AlignRight().Column(c =>
                        {
                            c.Item().Text("INVOICE").FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                            c.Item().Text($"# {invoice.InvoiceNumber}").FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });
                    hdr.Item().PaddingTop(6).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);
                });

                // ── CONTENT ──
                page.Content().Column(col =>
                {
                    // Invoice details + Bill To in one compact row
                    col.Item().PaddingBottom(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Date: {invoice.CreatedAt:dd MMM yyyy hh:mm tt}").FontSize(8.5f);
                            c.Item().Text($"Payment: {invoice.PaymentMethodDisplay}  |  Status: {invoice.StatusDisplay}").FontSize(8.5f);
                        });
                        if (!string.IsNullOrEmpty(invoice.CustomerName))
                        {
                            row.ConstantItem(150).AlignRight().Column(c =>
                            {
                                c.Item().Text("Bill To").FontSize(8.5f).Bold();
                                c.Item().Text(invoice.CustomerName).FontSize(8.5f);
                            });
                        }
                    });

                    // ── Items Table (compact 6-column) ──
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(0.4f); // #
                            cd.RelativeColumn(4f);   // Product
                            cd.RelativeColumn(0.8f); // Qty
                            cd.RelativeColumn(1.3f); // Price
                            cd.RelativeColumn(1f);   // Tax
                            cd.RelativeColumn(1.3f); // Total
                        });

                        var hdrBg = Colors.Blue.Medium;
                        table.Header(h =>
                        {
                            h.Cell().Background(hdrBg).Padding(4).Text("#").Bold().FontSize(8).FontColor(Colors.White);
                            h.Cell().Background(hdrBg).Padding(4).Text("Product").Bold().FontSize(8).FontColor(Colors.White);
                            h.Cell().Background(hdrBg).Padding(4).AlignRight().Text("Qty").Bold().FontSize(8).FontColor(Colors.White);
                            h.Cell().Background(hdrBg).Padding(4).AlignRight().Text("Price").Bold().FontSize(8).FontColor(Colors.White);
                            h.Cell().Background(hdrBg).Padding(4).AlignRight().Text("Tax").Bold().FontSize(8).FontColor(Colors.White);
                            h.Cell().Background(hdrBg).Padding(4).AlignRight().Text("Total").Bold().FontSize(8).FontColor(Colors.White);
                        });

                        int idx = 1;
                        foreach (var item in items)
                        {
                            var bg = idx % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                            table.Cell().Background(bg).Padding(3).Text($"{idx}").FontSize(8);
                            table.Cell().Background(bg).Padding(3).Column(pc =>
                            {
                                pc.Item().Text(item.ProductName).FontSize(8);
                                if (!string.IsNullOrEmpty(item.HsnCode))
                                    pc.Item().Text($"HSN: {item.HsnCode}").FontSize(6.5f).FontColor(Colors.Grey.Medium);
                            });
                            table.Cell().Background(bg).Padding(3).AlignRight().Text($"{item.Quantity:0.##}").FontSize(8);
                            table.Cell().Background(bg).Padding(3).AlignRight().Text($"{currency}{item.UnitPrice:N2}").FontSize(8);
                            table.Cell().Background(bg).Padding(3).AlignRight().Text($"{item.TaxRate:0.##}% ({currency}{item.TaxAmount:N2})").FontSize(7.5f);
                            table.Cell().Background(bg).Padding(3).AlignRight().Text($"{currency}{item.LineTotal:N2}").FontSize(8).Bold();
                            idx++;
                        }
                    });

                    // ── Totals (compact, right-aligned) ──
                    col.Item().PaddingTop(8).AlignRight().Width(220).Column(totals =>
                    {
                        void AddLine(string label, string value, bool bold = false, string? color = null)
                        {
                            totals.Item().PaddingVertical(1.5f).Row(r =>
                            {
                                var lbl = r.RelativeItem().Text(label).FontSize(8.5f);
                                var val = r.ConstantItem(90).AlignRight().Text(value).FontSize(8.5f);
                                if (bold) { lbl.Bold(); val.Bold(); }
                                if (color != null) { val.FontColor(Color.FromHex(color)); }
                            });
                        }

                        AddLine("Subtotal", $"{currency}{invoice.Subtotal:N2}");

                        if (invoice.DiscountAmount > 0)
                        {
                            var dl = invoice.DiscountType == "PERCENTAGE"
                                ? $"Discount ({invoice.DiscountValue}%)" : "Discount";
                            AddLine(dl, $"-{currency}{invoice.DiscountAmount:N2}");
                        }

                        // Tax breakdown
                        if (invoice.TaxAmount > 0)
                        {
                            foreach (var group in items.Where(i => i.TaxRate > 0).GroupBy(i => i.TaxSlabId))
                            {
                                if (group.Key.HasValue && taxSlabs.TryGetValue(group.Key.Value, out var slab))
                                {
                                    decimal taxAmt = group.Sum(i => i.TaxAmount);
                                    if (!string.IsNullOrEmpty(slab.Component1Name) && slab.Rate > 0)
                                    {
                                        decimal c1 = taxAmt * ((slab.Component1Rate ?? 0) / slab.Rate);
                                        AddLine($"  {slab.Component1Name} ({slab.Component1Rate}%)", $"{currency}{c1:N2}");
                                    }
                                    if (!string.IsNullOrEmpty(slab.Component2Name) && slab.Rate > 0)
                                    {
                                        decimal c2 = taxAmt * ((slab.Component2Rate ?? 0) / slab.Rate);
                                        AddLine($"  {slab.Component2Name} ({slab.Component2Rate}%)", $"{currency}{c2:N2}");
                                    }
                                    if (string.IsNullOrEmpty(slab.Component1Name))
                                        AddLine($"  {slab.TaxName} ({slab.Rate}%)", $"{currency}{taxAmt:N2}");
                                }
                            }
                        }

                        totals.Item().PaddingVertical(2).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);
                        AddLine("TOTAL", $"{currency}{invoice.TotalAmount:N2}", true);

                        if (invoice.PaymentMethod == "CASH" && invoice.AmountTendered.HasValue)
                        {
                            AddLine("Tendered", $"{currency}{invoice.AmountTendered:N2}");
                            AddLine("Change", $"{currency}{invoice.ChangeGiven ?? 0:N2}");
                        }
                    });

                    // ── Payment Information (Bank + UPI side by side, compact) ──
                    if (hasBankDetails || hasUpi)
                    {
                        col.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                        col.Item().PaddingTop(6).Row(payRow =>
                        {
                            if (hasBankDetails)
                            {
                                payRow.RelativeItem().Column(bank =>
                                {
                                    bank.Item().Text("Bank Transfer Details").FontSize(9).Bold().FontColor(Colors.Blue.Medium);
                                    bank.Item().PaddingTop(3).Table(bt =>
                                    {
                                        bt.ColumnsDefinition(cd =>
                                        {
                                            cd.RelativeColumn(1.2f);
                                            cd.RelativeColumn(2.8f);
                                        });

                                        void BankRow(string l, string v)
                                        {
                                            bt.Cell().PaddingVertical(1).Text(l).FontSize(7.5f).FontColor(Colors.Grey.Medium);
                                            bt.Cell().PaddingVertical(1).Text(v).FontSize(7.5f).Bold();
                                        }

                                        if (!string.IsNullOrWhiteSpace(biz.BankAccountHolder))
                                            BankRow("A/C Holder", biz.BankAccountHolder);
                                        BankRow("A/C No.", biz.BankAccountNo);
                                        BankRow("Bank", biz.BankName);
                                        if (!string.IsNullOrWhiteSpace(biz.BankBranch))
                                            BankRow("Branch", biz.BankBranch);
                                        if (!string.IsNullOrWhiteSpace(biz.BankIfsc))
                                            BankRow("IFSC", biz.BankIfsc);
                                    });
                                });
                            }

                            if (hasUpi)
                            {
                                var upiCol = hasBankDetails ? payRow.ConstantItem(110) : payRow.RelativeItem();
                                upiCol.AlignCenter().Column(upi =>
                                {
                                    upi.Item().Text("Pay via UPI").FontSize(8).Bold().FontColor(Colors.Blue.Medium);
                                    if (qrBytes != null)
                                        upi.Item().PaddingTop(3).AlignCenter().Width(70).Height(70).Image(qrBytes);
                                    upi.Item().PaddingTop(2).AlignCenter().Text(biz.UpiId).FontSize(7).FontColor(Colors.Grey.Medium);
                                    if (invoice.TotalAmount > 0)
                                        upi.Item().AlignCenter().Text($"{currency}{invoice.TotalAmount:N2}").FontSize(8).Bold();
                                });
                            }
                        });
                    }

                    // Footer note
                    if (!string.IsNullOrEmpty(biz.InvoiceFooter))
                        col.Item().PaddingTop(10).AlignCenter().Text(biz.InvoiceFooter).FontSize(8.5f).FontColor(Colors.Grey.Medium);

                    col.Item().PaddingTop(4).AlignCenter().Text("Thank you for your business!").FontSize(8.5f).Italic().FontColor(Colors.Grey.Medium);
                });

                // ── PAGE FOOTER ──
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span($"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt} | ").FontSize(7).FontColor(Colors.Grey.Medium);
                    t.Span("OpenPOS").FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(path);

        return path;
    }

    // --- Consolidated (All-in-One) PDF ---
    public static string GenerateConsolidatedReport(
        BusinessDetails biz,
        ReportService.SalesSummary summary,
        List<Invoice> dailyInvoices,
        List<ReportService.ProductSalesRow> productRows,
        List<Product> inventory,
        List<Product> lowStock,
        List<ReportService.TaxCollectionRow> taxRows,
        DateTime from, DateTime to, string currency,
        Dictionary<int, List<InvoiceItem>>? invoiceItemsMap = null)
    {
        var path = GetDownloadPath(biz.BusinessName, "ConsolidatedReport", from);

        Document.Create(container =>
        {
            // ===================== PAGE 1: Cover + Sales Summary =====================
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(biz.BusinessName).FontSize(22).Bold();
                    if (!string.IsNullOrEmpty(biz.AddressLine1))
                        col.Item().Text($"{biz.AddressLine1}, {biz.City} {biz.PostalCode}").FontSize(9).FontColor(Colors.Grey.Medium);
                    if (!string.IsNullOrEmpty(biz.Phone))
                        col.Item().Text($"Ph: {biz.Phone}  |  {biz.Email}").FontSize(9).FontColor(Colors.Grey.Medium);
                    if (!string.IsNullOrEmpty(biz.Gstin))
                        col.Item().Text($"GSTIN: {biz.Gstin}").FontSize(9).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingTop(8).PaddingBottom(8).LineHorizontal(2).LineColor(Colors.Blue.Medium);
                    col.Item().Text("Consolidated Business Report").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                    col.Item().Text($"Period: {from:dd MMM yyyy} - {to:dd MMM yyyy}").FontSize(11).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingBottom(10).Text($"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt}").FontSize(9).FontColor(Colors.Grey.Medium);
                });

                page.Content().Column(col =>
                {
                    // === Sales Summary Section ===
                    col.Item().PaddingBottom(8).Text("1. Sales Summary").FontSize(14).Bold();
                    col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    col.Item().PaddingBottom(12).Row(row =>
                    {
                        void Metric(IContainer c, string label, string value)
                        {
                            c.Background(Colors.Grey.Lighten4).Padding(12).Column(mc =>
                            {
                                mc.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Medium);
                                mc.Item().Text(value).FontSize(16).Bold();
                            });
                        }
                        row.RelativeItem().Column(c => Metric(c.Item(), "Total Revenue", $"{currency}{summary.TotalRevenue:N2}"));
                        row.ConstantItem(8);
                        row.RelativeItem().Column(c => Metric(c.Item(), "Total Invoices", $"{summary.TotalInvoices}"));
                        row.ConstantItem(8);
                        row.RelativeItem().Column(c => Metric(c.Item(), "Avg. Order Value", $"{currency}{summary.AvgOrderValue:N2}"));
                    });

                    col.Item().PaddingBottom(12).Row(row =>
                    {
                        void Metric(IContainer c, string label, string value)
                        {
                            c.Background(Colors.Grey.Lighten4).Padding(12).Column(mc =>
                            {
                                mc.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Medium);
                                mc.Item().Text(value).FontSize(14).Bold();
                            });
                        }
                        row.RelativeItem().Column(c => Metric(c.Item(), "Tax Collected", $"{currency}{summary.TotalTax:N2}"));
                        row.ConstantItem(8);
                        row.RelativeItem().Column(c => Metric(c.Item(), "Discounts Given", $"{currency}{summary.TotalDiscount:N2}"));
                    });

                    // Payment breakdown
                    col.Item().PaddingBottom(5).Text("Payment Breakdown").FontSize(11).Bold();
                    col.Item().PaddingBottom(15).Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(1);
                            cd.RelativeColumn(2);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Method").Bold().FontSize(9).FontColor(Colors.White);
                            h.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Count").Bold().FontSize(9).FontColor(Colors.White);
                            h.Cell().Background(Colors.Blue.Medium).Padding(5).AlignRight().Text("Amount").Bold().FontSize(9).FontColor(Colors.White);
                        });
                        void PayRow(string method, int count, decimal amount)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(method).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(count.ToString()).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{currency}{amount:N2}").FontSize(9);
                        }
                        PayRow("Cash", summary.CashCount, summary.CashAmount);
                        PayRow("UPI", summary.UpiCount, summary.UpiAmount);
                        PayRow("Card", summary.CardCount, summary.CardAmount);
                    });

                    // === All Invoices Section ===
                    col.Item().PaddingBottom(8).Text($"2. All Invoices ({dailyInvoices.Count})").FontSize(14).Bold();
                    col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    if (dailyInvoices.Count > 0)
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(1.5f); // Invoice #
                                cd.RelativeColumn(1.5f); // Date
                                cd.RelativeColumn(2);    // Customer
                                cd.RelativeColumn(1);    // Payment
                                cd.RelativeColumn(0.8f); // Status
                                cd.RelativeColumn(1.2f); // Amount
                            });
                            var hBg = Colors.Grey.Lighten3;
                            table.Header(h =>
                            {
                                h.Cell().Background(hBg).Padding(3).Text("Invoice #").Bold().FontSize(7.5f);
                                h.Cell().Background(hBg).Padding(3).Text("Date").Bold().FontSize(7.5f);
                                h.Cell().Background(hBg).Padding(3).Text("Customer").Bold().FontSize(7.5f);
                                h.Cell().Background(hBg).Padding(3).Text("Payment").Bold().FontSize(7.5f);
                                h.Cell().Background(hBg).Padding(3).Text("Status").Bold().FontSize(7.5f);
                                h.Cell().Background(hBg).Padding(3).AlignRight().Text("Amount").Bold().FontSize(7.5f);
                            });
                            foreach (var inv in dailyInvoices)
                            {
                                var b = Colors.Grey.Lighten3;
                                table.Cell().BorderBottom(1).BorderColor(b).Padding(3).Text(inv.InvoiceNumber).FontSize(7.5f);
                                table.Cell().BorderBottom(1).BorderColor(b).Padding(3).Text(inv.CreatedAt.ToString("dd MMM yy hh:mm tt")).FontSize(7.5f);
                                table.Cell().BorderBottom(1).BorderColor(b).Padding(3).Text(string.IsNullOrEmpty(inv.CustomerName) ? "-" : inv.CustomerName).FontSize(7.5f);
                                table.Cell().BorderBottom(1).BorderColor(b).Padding(3).Text(inv.PaymentMethodDisplay).FontSize(7.5f);
                                table.Cell().BorderBottom(1).BorderColor(b).Padding(3).Text(inv.StatusDisplay).FontSize(7.5f);
                                table.Cell().BorderBottom(1).BorderColor(b).Padding(3).AlignRight().Text($"{currency}{inv.TotalAmount:N2}").FontSize(7.5f).Bold();
                            }
                        });
                    }
                    else
                    {
                        col.Item().PaddingBottom(10).Text("No invoices in this period.").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span("  |  OpenPOS").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            // ===================== PAGE 2: Product Sales + Tax Collection =====================
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(biz.BusinessName).FontSize(14).Bold();
                    col.Item().PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    // === Product Sales Section ===
                    col.Item().PaddingBottom(8).Text($"3. Product Sales ({productRows.Count} products)").FontSize(14).Bold();
                    col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    if (productRows.Count > 0)
                    {
                        col.Item().PaddingBottom(15).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(3);
                                cd.RelativeColumn(1.5f);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(1);
                                cd.RelativeColumn(1.5f);
                                cd.RelativeColumn(1.5f);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Product").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("SKU").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Category").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Qty").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Revenue").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Tax").Bold().FontSize(8);
                            });
                            foreach (var r in productRows)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(r.ProductName).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(r.Sku).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(r.CategoryName).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{r.QuantitySold:0.##}").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{currency}{r.Revenue:N2}").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{currency}{r.TaxCollected:N2}").FontSize(8);
                            }
                            table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten4).Padding(4).Text("TOTAL").Bold().FontSize(8);
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text($"{productRows.Sum(r => r.QuantitySold):0.##}").Bold().FontSize(8);
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text($"{currency}{productRows.Sum(r => r.Revenue):N2}").Bold().FontSize(8);
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text($"{currency}{productRows.Sum(r => r.TaxCollected):N2}").Bold().FontSize(8);
                        });
                    }
                    else
                    {
                        col.Item().PaddingBottom(15).Text("No product sales in this period.").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                    }

                    // === Tax Collection Section ===
                    col.Item().PaddingBottom(8).Text("4. Tax Collection").FontSize(14).Bold();
                    col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    if (taxRows.Count > 0)
                    {
                        var totalTax = taxRows.Sum(r => r.TaxCollected);
                        col.Item().PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Total Tax Collected").FontSize(9).FontColor(Colors.Grey.Medium);
                                c.Item().Text($"{currency}{totalTax:N2}").FontSize(16).Bold();
                            });
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(2.5f);
                                cd.RelativeColumn(1);
                                cd.RelativeColumn(1);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Tax Slab").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Rate %").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Invoices").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Taxable").Bold().FontSize(8);
                                h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Collected").Bold().FontSize(8);
                            });
                            foreach (var r in taxRows)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(r.TaxName).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{r.Rate:0.##}%").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text(r.InvoiceCount.ToString()).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{currency}{r.TaxableAmount:N2}").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{currency}{r.TaxCollected:N2}").FontSize(8);
                            }
                            table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten4).Padding(4).Text("TOTAL").Bold().FontSize(8);
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text($"{currency}{taxRows.Sum(r => r.TaxableAmount):N2}").Bold().FontSize(8);
                            table.Cell().Background(Colors.Grey.Lighten4).Padding(4).AlignRight().Text($"{currency}{totalTax:N2}").Bold().FontSize(8);
                        });
                    }
                    else
                    {
                        col.Item().PaddingBottom(10).Text("No tax data for this period.").FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span("  |  OpenPOS").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            // ===================== PAGE 3: Inventory + Low Stock =====================
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(biz.BusinessName).FontSize(14).Bold();
                    col.Item().PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    // === Inventory Summary ===
                    var totalValue = inventory.Sum(p => p.CurrentStock * p.CostPrice);
                    col.Item().PaddingBottom(8).Text($"5. Inventory Overview ({inventory.Count} products)").FontSize(14).Bold();
                    col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("Total Products").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{inventory.Count}").FontSize(16).Bold();
                        });
                        row.ConstantItem(8);
                        row.RelativeItem().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
                        {
                            c.Item().Text("Total Stock Value").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"{currency}{totalValue:N2}").FontSize(16).Bold();
                        });
                        row.ConstantItem(8);
                        row.RelativeItem().Background(Colors.Red.Lighten5).Padding(10).Column(c =>
                        {
                            c.Item().Text("Low Stock Items").FontSize(9).FontColor(Colors.Red.Medium);
                            c.Item().Text($"{lowStock.Count}").FontSize(16).Bold().FontColor(Colors.Red.Medium);
                        });
                    });

                    // Top 15 products by stock value
                    var topProducts = inventory.OrderByDescending(p => p.CurrentStock * p.CostPrice).Take(15).ToList();
                    col.Item().PaddingBottom(5).Text("Top Products by Stock Value").FontSize(11).Bold();
                    col.Item().PaddingBottom(15).Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(3);
                            cd.RelativeColumn(2);
                            cd.RelativeColumn(1.2f);
                            cd.RelativeColumn(1.2f);
                            cd.RelativeColumn(1.5f);
                        });
                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Product").Bold().FontSize(8);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Category").Bold().FontSize(8);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Stock").Bold().FontSize(8);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Cost").Bold().FontSize(8);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Value").Bold().FontSize(8);
                        });
                        foreach (var p in topProducts)
                        {
                            var isLow = p.CurrentStock <= p.MinStockLevel;
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(p.Name).FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(p.CategoryName).FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight()
                                .Text($"{p.CurrentStock:0.##}").FontSize(8).FontColor(isLow ? Colors.Red.Medium : Colors.Black);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{currency}{p.CostPrice:N2}").FontSize(8);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{currency}{p.CurrentStock * p.CostPrice:N2}").FontSize(8);
                        }
                    });

                    // === Low Stock Alert ===
                    col.Item().PaddingBottom(8).Text($"6. Low Stock Alert ({lowStock.Count} items)").FontSize(14).Bold();
                    col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    if (lowStock.Count > 0)
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(3);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(1.2f);
                                cd.RelativeColumn(1.2f);
                                cd.RelativeColumn(1.2f);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Red.Lighten4).Padding(4).Text("Product").Bold().FontSize(8);
                                h.Cell().Background(Colors.Red.Lighten4).Padding(4).Text("Category").Bold().FontSize(8);
                                h.Cell().Background(Colors.Red.Lighten4).Padding(4).Text("Supplier").Bold().FontSize(8);
                                h.Cell().Background(Colors.Red.Lighten4).Padding(4).AlignRight().Text("Current").Bold().FontSize(8);
                                h.Cell().Background(Colors.Red.Lighten4).Padding(4).AlignRight().Text("Minimum").Bold().FontSize(8);
                                h.Cell().Background(Colors.Red.Lighten4).Padding(4).AlignRight().Text("Deficit").Bold().FontSize(8);
                            });
                            foreach (var p in lowStock)
                            {
                                var deficit = p.MinStockLevel - p.CurrentStock;
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(p.Name).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(p.CategoryName).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(p.SupplierName).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{p.CurrentStock:0.##}").FontSize(8).FontColor(Colors.Red.Medium);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{p.MinStockLevel:0.##}").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight().Text($"{deficit:0.##}").FontSize(8).Bold().FontColor(Colors.Red.Medium);
                            }
                        });
                    }
                    else
                    {
                        col.Item().Text("All stock levels are healthy!").FontSize(9).Italic().FontColor(Colors.Green.Medium);
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span("  |  OpenPOS").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            // ===================== PAGE 4+: Invoice Copies =====================
            if (invoiceItemsMap != null && dailyInvoices.Count > 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30, QuestPDF.Infrastructure.Unit.Point);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text(biz.BusinessName).FontSize(14).Bold();
                        col.Item().Text($"7. Invoice Copies | {from:dd MMM yyyy} - {to:dd MMM yyyy}").FontSize(11).FontColor(Colors.Blue.Medium);
                        col.Item().PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Column(col =>
                    {
                        int invIdx = 0;
                        foreach (var inv in dailyInvoices)
                        {
                            if (invIdx > 0)
                                col.Item().PaddingVertical(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                            // Invoice header row
                            col.Item().PaddingBottom(4).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text($"Invoice #{inv.InvoiceNumber}").FontSize(10).Bold();
                                    c.Item().Text($"{inv.CreatedAt:dd MMM yyyy hh:mm tt}  |  {inv.PaymentMethodDisplay}  |  {inv.StatusDisplay}").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                                    if (!string.IsNullOrEmpty(inv.CustomerName))
                                        c.Item().Text($"Customer: {inv.CustomerName}").FontSize(7.5f);
                                });
                                row.ConstantItem(100).AlignRight().Column(c =>
                                {
                                    c.Item().Text($"{currency}{inv.TotalAmount:N2}").FontSize(12).Bold();
                                });
                            });

                            // Invoice items table
                            if (invoiceItemsMap.TryGetValue(inv.Id, out var items) && items.Count > 0)
                            {
                                col.Item().PaddingBottom(2).Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(0.4f);  // #
                                        cd.RelativeColumn(4f);    // Product
                                        cd.RelativeColumn(0.8f);  // Qty
                                        cd.RelativeColumn(1.2f);  // Price
                                        cd.RelativeColumn(1f);    // Tax
                                        cd.RelativeColumn(1.2f);  // Total
                                    });

                                    var hBg = Colors.Grey.Lighten3;
                                    table.Header(h =>
                                    {
                                        h.Cell().Background(hBg).Padding(2).Text("#").Bold().FontSize(7);
                                        h.Cell().Background(hBg).Padding(2).Text("Product").Bold().FontSize(7);
                                        h.Cell().Background(hBg).Padding(2).AlignRight().Text("Qty").Bold().FontSize(7);
                                        h.Cell().Background(hBg).Padding(2).AlignRight().Text("Price").Bold().FontSize(7);
                                        h.Cell().Background(hBg).Padding(2).AlignRight().Text("Tax").Bold().FontSize(7);
                                        h.Cell().Background(hBg).Padding(2).AlignRight().Text("Total").Bold().FontSize(7);
                                    });

                                    int idx = 1;
                                    foreach (var item in items)
                                    {
                                        table.Cell().Padding(2).Text($"{idx}").FontSize(7);
                                        table.Cell().Padding(2).Text(item.ProductName).FontSize(7);
                                        table.Cell().Padding(2).AlignRight().Text($"{item.Quantity:0.##}").FontSize(7);
                                        table.Cell().Padding(2).AlignRight().Text($"{currency}{item.UnitPrice:N2}").FontSize(7);
                                        table.Cell().Padding(2).AlignRight().Text($"{item.TaxRate:0.##}% ({currency}{item.TaxAmount:N2})").FontSize(6.5f);
                                        table.Cell().Padding(2).AlignRight().Text($"{currency}{item.LineTotal:N2}").FontSize(7).Bold();
                                        idx++;
                                    }
                                });

                                // Compact totals line
                                col.Item().PaddingTop(2).AlignRight().Row(tr =>
                                {
                                    if (inv.DiscountAmount > 0)
                                        tr.ConstantItem(120).AlignRight().Text($"Discount: -{currency}{inv.DiscountAmount:N2}").FontSize(7).FontColor(Colors.Grey.Medium);
                                    if (inv.TaxAmount > 0)
                                        tr.ConstantItem(110).AlignRight().Text($"Tax: {currency}{inv.TaxAmount:N2}").FontSize(7).FontColor(Colors.Grey.Medium);
                                    tr.ConstantItem(120).AlignRight().Text($"Total: {currency}{inv.TotalAmount:N2}").FontSize(8).Bold();
                                });
                            }

                            invIdx++;
                        }
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                        t.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                        t.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                        t.Span("  |  OpenPOS").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }
        }).GeneratePdf(path);

        return path;
    }

    // --- Purchase Order PDF ---
    public static string GeneratePurchaseOrderPdf(BusinessDetails biz, PurchaseOrder po,
        List<PurchaseOrderItem> items, Supplier? supplier, string currency)
    {
        var sanitized = string.Join("_", biz.BusinessName.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{sanitized}_PO_{po.PoNumber}_{po.CreatedAt:yyyy-MM-dd}.pdf";
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, fileName);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30, QuestPDF.Infrastructure.Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Header
                page.Header().PaddingBottom(8).Column(hdr =>
                {
                    hdr.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(biz.BusinessName).FontSize(16).Bold();
                            var addrParts = new List<string>();
                            if (!string.IsNullOrEmpty(biz.AddressLine1)) addrParts.Add(biz.AddressLine1);
                            if (!string.IsNullOrEmpty(biz.City)) addrParts.Add(biz.City);
                            if (!string.IsNullOrEmpty(biz.PostalCode)) addrParts.Add(biz.PostalCode);
                            if (addrParts.Count > 0)
                                c.Item().Text(string.Join(", ", addrParts)).FontSize(8).FontColor(Colors.Grey.Medium);
                            var contactParts = new List<string>();
                            if (!string.IsNullOrEmpty(biz.Phone)) contactParts.Add($"Ph: {biz.Phone}");
                            if (!string.IsNullOrEmpty(biz.Email)) contactParts.Add(biz.Email);
                            if (contactParts.Count > 0)
                                c.Item().Text(string.Join("  |  ", contactParts)).FontSize(8).FontColor(Colors.Grey.Medium);
                            if (!string.IsNullOrEmpty(biz.Gstin))
                                c.Item().Text($"GSTIN: {biz.Gstin}").FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(160).AlignRight().Column(c =>
                        {
                            c.Item().Text("PURCHASE ORDER").FontSize(16).Bold().FontColor(Colors.Teal.Medium);
                            c.Item().Text($"# {po.PoNumber}").FontSize(9).FontColor(Colors.Grey.Medium);
                            c.Item().Text($"Status: {po.StatusDisplay}").FontSize(9).Bold();
                        });
                    });
                    hdr.Item().PaddingTop(6).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);
                });

                // Content
                page.Content().Column(col =>
                {
                    // PO details + Supplier info
                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Date: {po.CreatedAt:dd MMM yyyy}").FontSize(9);
                            if (po.ExpectedDate.HasValue)
                                c.Item().Text($"Expected: {po.ExpectedDate:dd MMM yyyy}").FontSize(9);
                            if (!string.IsNullOrEmpty(po.Notes))
                                c.Item().Text($"Notes: {po.Notes}").FontSize(8.5f).FontColor(Colors.Grey.Medium);
                        });
                        if (supplier != null)
                        {
                            row.ConstantItem(200).AlignRight().Column(c =>
                            {
                                c.Item().Text("Supplier").FontSize(9).Bold().FontColor(Colors.Teal.Medium);
                                c.Item().Text(supplier.Name).FontSize(9).Bold();
                                if (!string.IsNullOrEmpty(supplier.ContactPerson))
                                    c.Item().Text(supplier.ContactPerson).FontSize(8);
                                if (!string.IsNullOrEmpty(supplier.Email))
                                    c.Item().Text(supplier.Email).FontSize(8);
                                if (!string.IsNullOrEmpty(supplier.Phone))
                                    c.Item().Text($"Ph: {supplier.Phone}").FontSize(8);
                                var sAddr = new List<string>();
                                if (!string.IsNullOrEmpty(supplier.Address)) sAddr.Add(supplier.Address);
                                if (!string.IsNullOrEmpty(supplier.City)) sAddr.Add(supplier.City);
                                if (!string.IsNullOrEmpty(supplier.PinCode)) sAddr.Add(supplier.PinCode);
                                if (sAddr.Count > 0)
                                    c.Item().Text(string.Join(", ", sAddr)).FontSize(8).FontColor(Colors.Grey.Medium);
                                if (!string.IsNullOrEmpty(supplier.GstNumber))
                                    c.Item().Text($"GST: {supplier.GstNumber}").FontSize(8);
                            });
                        }
                    });

                    // Items Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(0.4f); // #
                            cd.RelativeColumn(3.5f); // Product
                            cd.RelativeColumn(0.8f); // Qty
                            cd.RelativeColumn(1.2f); // Unit Price
                            cd.RelativeColumn(1.2f); // Tax
                            cd.RelativeColumn(1.3f); // Total
                        });

                        var hdrBg = Colors.Teal.Medium;
                        table.Header(h =>
                        {
                            h.Cell().Background(hdrBg).Padding(4).Text("#").Bold().FontSize(8).FontColor(Colors.White);
                            h.Cell().Background(hdrBg).Padding(4).Text("Product").Bold().FontSize(8).FontColor(Colors.White);
                            h.Cell().Background(hdrBg).Padding(4).AlignRight().Text("Qty").Bold().FontSize(8).FontColor(Colors.White);
                            h.Cell().Background(hdrBg).Padding(4).AlignRight().Text("Unit Price").Bold().FontSize(8).FontColor(Colors.White);
                            h.Cell().Background(hdrBg).Padding(4).AlignRight().Text("Tax").Bold().FontSize(8).FontColor(Colors.White);
                            h.Cell().Background(hdrBg).Padding(4).AlignRight().Text("Total").Bold().FontSize(8).FontColor(Colors.White);
                        });

                        int idx = 1;
                        foreach (var item in items)
                        {
                            var bg = idx % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                            table.Cell().Background(bg).Padding(3).Text($"{idx}").FontSize(8);
                            table.Cell().Background(bg).Padding(3).Text(item.ProductName).FontSize(8);
                            table.Cell().Background(bg).Padding(3).AlignRight().Text($"{item.Quantity:0.##}").FontSize(8);
                            table.Cell().Background(bg).Padding(3).AlignRight().Text($"{currency}{item.UnitPrice:N2}").FontSize(8);
                            table.Cell().Background(bg).Padding(3).AlignRight().Text(item.TaxRate > 0 ? $"{item.TaxRate:0.##}% ({currency}{item.TaxAmount:N2})" : "-").FontSize(7.5f);
                            table.Cell().Background(bg).Padding(3).AlignRight().Text($"{currency}{item.LineTotal:N2}").FontSize(8).Bold();
                            idx++;
                        }
                    });

                    // Totals
                    col.Item().PaddingTop(8).AlignRight().Width(200).Column(totals =>
                    {
                        void AddLine(string label, string value, bool bold = false)
                        {
                            totals.Item().PaddingVertical(1.5f).Row(r =>
                            {
                                var lbl = r.RelativeItem().Text(label).FontSize(9);
                                var val = r.ConstantItem(90).AlignRight().Text(value).FontSize(9);
                                if (bold) { lbl.Bold(); val.Bold(); }
                            });
                        }

                        AddLine("Subtotal", $"{currency}{po.Subtotal:N2}");
                        if (po.TaxAmount > 0)
                            AddLine("Tax", $"{currency}{po.TaxAmount:N2}");
                        totals.Item().PaddingVertical(2).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten2);
                        AddLine("TOTAL", $"{currency}{po.TotalAmount:N2}", true);
                    });

                    // Footer note
                    col.Item().PaddingTop(20).AlignCenter().Text("This is a computer-generated purchase order.").FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span($"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt} | ").FontSize(7).FontColor(Colors.Grey.Medium);
                    t.Span("OpenPOS").FontSize(7).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(path);

        return path;
    }

    // --- Tax Collection PDF ---
    public static string GenerateTaxCollectionReport(BusinessDetails biz, List<ReportService.TaxCollectionRow> rows,
        DateTime from, DateTime to, string currency)
    {
        var path = GetDownloadPath(biz.BusinessName, "TaxCollection", from);
        var totalTax = rows.Sum(r => r.TaxCollected);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(biz.BusinessName).FontSize(18).Bold();
                    col.Item().Text($"Tax Collection Report | {from:dd MMM yyyy} - {to:dd MMM yyyy}").FontSize(12).FontColor(Colors.Grey.Medium);
                    col.Item().PaddingBottom(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().Column(col =>
                {
                    col.Item().PaddingBottom(15).Column(c =>
                    {
                        c.Item().Text("Total Tax Collected").FontSize(9).FontColor(Colors.Grey.Medium);
                        c.Item().Text($"{currency}{totalTax:N2}").FontSize(20).Bold();
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(2.5f);// Tax Name
                            cd.RelativeColumn(1);   // Rate
                            cd.RelativeColumn(1);   // Invoices
                            cd.RelativeColumn(2);   // Taxable
                            cd.RelativeColumn(2);   // Tax Collected
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Tax Slab").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Rate %").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Invoices").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Taxable Amt").Bold().FontSize(9);
                            h.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Tax Collected").Bold().FontSize(9);
                        });

                        foreach (var r in rows)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(r.TaxName).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{r.Rate:0.##}%").FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text(r.InvoiceCount.ToString()).FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{currency}{r.TaxableAmount:N2}").FontSize(9);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"{currency}{r.TaxCollected:N2}").FontSize(9);
                        }

                        // Total
                        table.Cell().ColumnSpan(3).Background(Colors.Grey.Lighten4).Padding(5).Text("TOTAL").Bold().FontSize(9);
                        table.Cell().Background(Colors.Grey.Lighten4).Padding(5).AlignRight().Text($"{currency}{rows.Sum(r => r.TaxableAmount):N2}").Bold().FontSize(9);
                        table.Cell().Background(Colors.Grey.Lighten4).Padding(5).AlignRight().Text($"{currency}{totalTax:N2}").Bold().FontSize(9);
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span($"Generated on {DateTime.Now:dd MMM yyyy hh:mm tt} | ").FontSize(8).FontColor(Colors.Grey.Medium);
                    t.Span("OpenPOS").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf(path);

        return path;
    }
}
