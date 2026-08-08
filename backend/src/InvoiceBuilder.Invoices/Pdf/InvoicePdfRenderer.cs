using InvoiceBuilder.Invoices.Domain;
using IronPdf;
using IronPdf.Signing;
using Microsoft.Extensions.Options;

namespace InvoiceBuilder.Invoices.Pdf;

public interface IInvoicePdfRenderer
{
    byte[] Render(Invoice invoice);
}

// RenderHtmlAsPdfUA (not RenderHtmlAsPdf) produces PDF/UA-tagged output — structure tree,
// document title, reading order — required by the PDF/UA accessibility/audit requirement.
public class InvoicePdfRenderer(IOptions<IronPdfOptions> options) : IInvoicePdfRenderer
{
    private readonly IronPdfOptions _options = options.Value;

    public byte[] Render(Invoice invoice)
    {
        var html = InvoiceHtmlTemplate.Build(invoice);

        var renderer = new ChromePdfRenderer();
        var pdf = renderer.RenderHtmlAsPdfUA(html);
        pdf.MetaData.Title = $"Invoice {invoice.InvoiceNumber}";

        var certificatePath = ResolveCertificatePath();
        if (certificatePath is not null)
        {
            var signature = new PdfSignature(certificatePath, _options.SigningCertificatePassword)
            {
                SigningReason = "Invoice issued",
                SigningLocation = "Invoice Builder"
            };
            pdf.Sign(signature);
        }

        return pdf.BinaryData;
    }

    private string? ResolveCertificatePath()
    {
        if (string.IsNullOrWhiteSpace(_options.SigningCertificatePath))
        {
            return null;
        }

        var fullPath = Path.Combine(AppContext.BaseDirectory, _options.SigningCertificatePath);
        return File.Exists(fullPath) ? fullPath : null;
    }
}
