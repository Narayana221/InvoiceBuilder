using System.Globalization;
using System.Net;
using System.Text;
using InvoiceBuilder.Invoices.Domain;

namespace InvoiceBuilder.Invoices.Pdf;

// Pure string building, no IronPDF dependency here — keeps this unit-testable without invoking Chromium.
// Every interpolated value that can contain user input goes through Encode() first.
public static class InvoiceHtmlTemplate
{
    public static string Build(Invoice invoice)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <title>Invoice {{Encode(invoice.InvoiceNumber)}}</title>
            <style>{{Css}}</style>
        </head>
        <body>
            <header class="invoice-header">
                <div>
                    <h1>INVOICE</h1>
                    <p class="invoice-number">{{Encode(invoice.InvoiceNumber)}}</p>
                </div>
                <div class="dates">
                    <p><span>Invoice Date</span><strong>{{invoice.InvoiceDate:yyyy-MM-dd}}</strong></p>
                    <p><span>Due Date</span><strong class="due-date">{{invoice.DueDate:yyyy-MM-dd}}</strong></p>
                </div>
            </header>

            <section class="parties">
                <div class="party">
                    <h2>From</h2>
                    <p class="party-name">{{Encode(invoice.Sender.Name)}}</p>
                    {{RenderIfPresent(invoice.Sender.ContactName)}}
                    <p>{{Encode(invoice.Sender.AddressLine)}}, {{Encode(invoice.Sender.City)}}</p>
                    {{RenderIfPresent(invoice.Sender.TaxId, "VAT/Tax ID")}}
                    {{RenderIfPresent(invoice.Sender.BankDetails, "Bank Details")}}
                </div>
                <div class="party">
                    <h2>Bill To</h2>
                    <p class="party-name">{{Encode(invoice.Customer.Name)}}</p>
                    {{RenderIfPresent(invoice.Customer.ContactName)}}
                    <p>{{Encode(invoice.Customer.AddressLine)}}, {{Encode(invoice.Customer.City)}}</p>
                    {{RenderIfPresent(invoice.Customer.Email, "Email")}}
                    {{RenderIfPresent(invoice.Customer.TaxId, "VAT/Tax ID")}}
                </div>
            </section>

            <table class="line-items">
                <thead>
                    <tr>
                        <th scope="col">Item Description</th>
                        <th scope="col" class="num">Quantity</th>
                        <th scope="col" class="num">Unit Price</th>
                        <th scope="col" class="num">Total</th>
                    </tr>
                </thead>
                <tbody>
                    {{BuildLineItemRows(invoice)}}
                </tbody>
            </table>

            <section class="summary">
                <div class="summary-row"><span>Subtotal</span><span>{{FormatMoney(invoice.SubtotalAmount, invoice.Currency)}}</span></div>
                <div class="summary-row"><span>Tax ({{invoice.TaxRatePercent.ToString("0.##", CultureInfo.InvariantCulture)}}%)</span><span>{{FormatMoney(invoice.TaxAmount, invoice.Currency)}}</span></div>
                <div class="summary-row total"><span>Total Amount</span><span>{{FormatMoney(invoice.TotalAmount, invoice.Currency)}}</span></div>
            </section>

            {{RenderNotes(invoice.Notes)}}
        </body>
        </html>
        """;
    }

    private static string BuildLineItemRows(Invoice invoice)
    {
        var sb = new StringBuilder();
        foreach (var item in invoice.LineItems)
        {
            sb.Append($"""
                <tr>
                    <td>{Encode(item.Description)}</td>
                    <td class="num">{item.Quantity.ToString("0.##", CultureInfo.InvariantCulture)}</td>
                    <td class="num">{FormatMoney(item.UnitPrice, invoice.Currency)}</td>
                    <td class="num">{FormatMoney(item.LineTotal, invoice.Currency)}</td>
                </tr>

                """);
        }

        return sb.ToString();
    }

    private static string RenderIfPresent(string? value, string? label = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return label is null
            ? $"<p>{Encode(value)}</p>"
            : $"<p><span class=\"label\">{Encode(label)}:</span> {Encode(value)}</p>";
    }

    private static string RenderNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return string.Empty;
        }

        return $"""
            <section class="notes">
                <h3>Notes</h3>
                <p>{Encode(notes)}</p>
            </section>
            """;
    }

    private static string FormatMoney(decimal amount, string currency) =>
        $"{amount.ToString("N2", CultureInfo.InvariantCulture)} {Encode(currency)}";

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    // Repeats <thead> on every printed page and keeps each row from splitting across a page break —
    // the "clean pagination at any length" requirement from the spec, done in plain CSS, not renderer code.
    private const string Css = """
        @page { size: A4; margin: 24mm 16mm; }
        * { box-sizing: border-box; }
        body { font-family: Helvetica, Arial, sans-serif; font-size: 11pt; color: #1f2937; margin: 0; }
        h1 { font-size: 22pt; margin: 0; letter-spacing: 1px; }
        h2 { font-size: 9pt; text-transform: uppercase; color: #6b7280; margin: 0 0 8px; }
        h3 { font-size: 9pt; text-transform: uppercase; color: #92400e; margin: 0 0 4px; }
        .invoice-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; }
        .invoice-number { color: #6b7280; margin: 4px 0 0; }
        .dates { text-align: right; font-size: 9pt; color: #6b7280; }
        .dates p { margin: 0 0 6px; }
        .dates span { display: block; }
        .dates strong { font-size: 11pt; color: #1f2937; }
        .dates .due-date { color: #b91c1c; }
        .parties { display: flex; gap: 16px; margin-bottom: 24px; }
        .party { flex: 1; background: #eff6ff; border-radius: 6px; padding: 12px 14px; }
        .party p { margin: 0 0 4px; }
        .party-name { font-weight: bold; }
        .label { color: #6b7280; }
        table.line-items { width: 100%; border-collapse: collapse; margin-bottom: 16px; }
        table.line-items thead { display: table-header-group; }
        table.line-items tr { page-break-inside: avoid; }
        table.line-items th { background: #111827; color: #fff; text-align: left; padding: 8px 10px; font-size: 9pt; text-transform: uppercase; }
        table.line-items td { padding: 8px 10px; border-bottom: 1px solid #e5e7eb; }
        table.line-items th.num, table.line-items td.num { text-align: right; }
        .summary { width: 260px; margin-left: auto; }
        .summary-row { display: flex; justify-content: space-between; padding: 4px 0; color: #6b7280; }
        .summary-row.total { border-top: 1px solid #e5e7eb; margin-top: 6px; padding-top: 10px; font-weight: bold; font-size: 13pt; color: #1f2937; }
        .notes { background: #fef9c3; border-left: 4px solid #ca8a04; padding: 10px 14px; margin-top: 24px; }
        .notes p { margin: 0; }
        """;
}
