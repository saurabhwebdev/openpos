using System.Drawing.Printing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using MyWinFormsApp.Models;

namespace MyWinFormsApp.Services;

/// <summary>
/// Sends raw ESC/POS commands to thermal receipt printers (58mm / 80mm).
/// </summary>
public static class ThermalPrintService
{
    private static readonly byte[] ESC_INIT = [0x1B, 0x40];
    private static readonly byte[] ESC_ALIGN_CENTER = [0x1B, 0x61, 0x01];
    private static readonly byte[] ESC_ALIGN_LEFT = [0x1B, 0x61, 0x00];
    private static readonly byte[] ESC_BOLD_ON = [0x1B, 0x45, 0x01];
    private static readonly byte[] ESC_BOLD_OFF = [0x1B, 0x45, 0x00];
    private static readonly byte[] ESC_DOUBLE_ON = [0x1B, 0x21, 0x30];
    private static readonly byte[] ESC_DOUBLE_HEIGHT = [0x1B, 0x21, 0x10];
    private static readonly byte[] ESC_NORMAL = [0x1B, 0x21, 0x00];
    private static readonly byte[] ESC_FEED_CUT = [0x1D, 0x56, 0x41, 0x03];
    private static readonly byte[] LF = [0x0A];

    public static List<string> GetInstalledPrinters()
    {
        var printers = new List<string>();
        foreach (string printer in PrinterSettings.InstalledPrinters)
            printers.Add(printer);
        return printers;
    }

    public static (bool Success, string Message) PrintReceipt(
        string printerName,
        BusinessDetails? business,
        Invoice invoice,
        List<InvoiceItem> items,
        int paperWidth = 48)
    {
        try
        {
            using var ms = new MemoryStream();

            ms.Write(ESC_INIT);

            // Header
            ms.Write(ESC_ALIGN_CENTER);
            ms.Write(ESC_DOUBLE_ON);
            WriteText(ms, business?.BusinessName ?? "Business");
            ms.Write(ESC_NORMAL);
            ms.Write(ESC_ALIGN_CENTER);

            if (business != null)
            {
                if (!string.IsNullOrEmpty(business.AddressLine1))
                    WriteText(ms, $"{business.AddressLine1}, {business.City} {business.PostalCode}");
                if (!string.IsNullOrEmpty(business.Phone))
                    WriteText(ms, $"Ph: {business.Phone}");
                if (!string.IsNullOrEmpty(business.Gstin))
                    WriteText(ms, $"GSTIN: {business.Gstin}");
            }

            WriteSep(ms, paperWidth);

            // Invoice info
            ms.Write(ESC_ALIGN_LEFT);
            WriteText(ms, $"Invoice: {invoice.InvoiceNumber}");
            WriteText(ms, $"Date: {invoice.CreatedAt:dd MMM yyyy hh:mm tt}");
            if (!string.IsNullOrEmpty(invoice.CustomerName))
                WriteText(ms, $"Customer: {invoice.CustomerName}");
            WriteText(ms, $"Payment: {invoice.PaymentMethodDisplay}");

            WriteSep(ms, paperWidth);

            // Items header
            ms.Write(ESC_BOLD_ON);
            WriteText(ms, PadRow("Item", "Qty", "Amount", paperWidth));
            ms.Write(ESC_BOLD_OFF);
            WriteText(ms, new string('-', paperWidth));

            foreach (var item in items)
            {
                WriteText(ms, item.ProductName);
                WriteText(ms, PadRow(
                    $"  {item.Quantity:0.##} x {item.UnitPrice:N2}", "",
                    $"{item.LineTotal:N2}", paperWidth));
            }

            WriteSep(ms, paperWidth);

            // Totals
            WriteRow(ms, "Subtotal", $"{invoice.Subtotal:N2}", paperWidth);
            if (invoice.DiscountAmount > 0)
            {
                var dl = invoice.DiscountType == "PERCENTAGE"
                    ? $"Discount ({invoice.DiscountValue}%)" : "Discount";
                WriteRow(ms, dl, $"-{invoice.DiscountAmount:N2}", paperWidth);
            }
            if (invoice.TaxAmount > 0)
                WriteRow(ms, "Tax", $"{invoice.TaxAmount:N2}", paperWidth);

            WriteSep(ms, paperWidth);

            ms.Write(ESC_BOLD_ON);
            ms.Write(ESC_DOUBLE_HEIGHT);
            WriteRow(ms, "TOTAL", $"{invoice.TotalAmount:N2}", paperWidth);
            ms.Write(ESC_NORMAL);

            if (invoice.PaymentMethod == "CASH" && invoice.AmountTendered.HasValue)
            {
                WriteRow(ms, "Tendered", $"{invoice.AmountTendered:N2}", paperWidth);
                WriteRow(ms, "Change", $"{invoice.ChangeGiven ?? 0:N2}", paperWidth);
            }

            WriteSep(ms, paperWidth);

            // Footer
            ms.Write(ESC_ALIGN_CENTER);
            if (!string.IsNullOrEmpty(business?.InvoiceFooter))
                WriteText(ms, business.InvoiceFooter);
            WriteText(ms, "Thank you for your business!");
            ms.Write(LF);
            ms.Write(LF);
            ms.Write(ESC_FEED_CUT);

            var data = ms.ToArray();
            var sent = RawPrinterHelper.SendBytesToPrinter(printerName, data);

            return sent
                ? (true, "Receipt printed successfully!")
                : (false, "Failed to send data to printer.");
        }
        catch (Exception ex)
        {
            return (false, $"Print error: {ex.Message}");
        }
    }

    private static void WriteText(MemoryStream ms, string text)
    {
        ms.Write(Encoding.GetEncoding(437).GetBytes(text));
        ms.Write(LF);
    }

    private static void WriteSep(MemoryStream ms, int w) =>
        WriteText(ms, new string('=', w));

    private static void WriteRow(MemoryStream ms, string l, string r, int w)
    {
        int sp = w - l.Length - r.Length;
        if (sp < 1) sp = 1;
        WriteText(ms, l + new string(' ', sp) + r);
    }

    private static string PadRow(string l, string m, string r, int w)
    {
        var lm = l + m;
        int sp = w - lm.Length - r.Length;
        if (sp < 1) sp = 1;
        return lm + new string(' ', sp) + r;
    }
}

internal static class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DOCINFOW
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pDataType;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFOW di);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    public static bool SendBytesToPrinter(string printerName, byte[] data)
    {
        IntPtr hPrinter = IntPtr.Zero;
        bool success = false;

        try
        {
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                return false;

            var di = new DOCINFOW
            {
                pDocName = "OpenPOS Receipt",
                pOutputFile = null,
                pDataType = "RAW"
            };

            if (!StartDocPrinter(hPrinter, 1, ref di))
                return false;

            if (!StartPagePrinter(hPrinter))
                return false;

            IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(data.Length);
            try
            {
                Marshal.Copy(data, 0, pUnmanagedBytes, data.Length);
                success = WritePrinter(hPrinter, pUnmanagedBytes, data.Length, out _);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pUnmanagedBytes);
            }

            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);
        }
        finally
        {
            if (hPrinter != IntPtr.Zero)
                ClosePrinter(hPrinter);
        }

        return success;
    }
}
