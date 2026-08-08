using System.Text;
using InvoiceBuilder.Invoices.Domain;
using InvoiceBuilder.Invoices.Pdf;
using Microsoft.Extensions.Options;
using Xunit;

namespace InvoiceBuilder.Invoices.IntegrationTests;

// Exercises the real IronPDF/Chromium rendering + signing pipeline (not just the HTML string,
// which InvoiceHtmlTemplateTests already covers in InvoiceBuilder.Invoices.UnitTests).
public class InvoicePdfRendererTests
{
    // Skipped: IronPDF throws LicensingException without at least a registered trial key
    // (see DECISIONS.md, Phase 4). Remove the Skip once IronPdf:LicenseKey is configured.
    [Fact(Skip = "Requires an IronPdf license/trial key — none configured yet, see DECISIONS.md")]
    public void Render_ProducesASignedPdf()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Acme Corp",
            AddressLine = "123 Main St",
            City = "Springfield",
            Country = "USA"
        };
        var sender = new Sender
        {
            Id = Guid.NewGuid(),
            Name = "My Company LLC",
            AddressLine = "456 Market St",
            City = "City",
            Country = "USA"
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-2026-0001",
            Currency = "USD",
            InvoiceDate = new DateOnly(2026, 7, 25),
            DueDate = new DateOnly(2026, 8, 8),
            CustomerId = customer.Id,
            Customer = customer,
            SenderId = sender.Id,
            Sender = sender,
            TaxRatePercent = 20,
            Notes = "Thank you for your business!",
            LineItems =
            [
                new InvoiceLineItem { Description = ".NET Book", Quantity = 1, UnitPrice = 29.99m },
                new InvoiceLineItem { Description = ".NET Course", Quantity = 1, UnitPrice = 89.99m }
            ]
        };
        invoice.RecalculateTotals();

        var certificatePath = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "src", "InvoiceBuilder.Api", "certs", "invoice-signing.pfx");

        var options = Options.Create(new IronPdfOptions
        {
            SigningCertificatePath = Path.GetFullPath(certificatePath),
            SigningCertificatePassword = "invoicebuilder-dev-signing"
        });

        var renderer = new InvoicePdfRenderer(options);

        var pdfBytes = renderer.Render(invoice);

        Assert.True(pdfBytes.Length > 1000, "Expected a non-trivial PDF byte count.");
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdfBytes, 0, 4));
    }
}
