using System.Globalization;
using InvoiceBuilder.Invoices.Domain;
using InvoiceBuilder.Invoices.Pdf;
using Xunit;

namespace InvoiceBuilder.Invoices.UnitTests;

public class InvoiceHtmlTemplateTests
{
    private static Invoice BuildInvoice(string customerName = "Acme Corp", string? notes = null)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = customerName,
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
            Notes = notes,
            LineItems =
            [
                new InvoiceLineItem { Description = ".NET Book", Quantity = 1, UnitPrice = 29.99m },
                new InvoiceLineItem { Description = ".NET Course", Quantity = 1, UnitPrice = 89.99m }
            ]
        };
        invoice.RecalculateTotals();

        return invoice;
    }

    [Fact]
    public void Build_IncludesInvoiceNumberCustomerAndLineItems()
    {
        var invoice = BuildInvoice();

        var html = InvoiceHtmlTemplate.Build(invoice);

        Assert.Contains("INV-2026-0001", html);
        Assert.Contains("Acme Corp", html);
        Assert.Contains(".NET Book", html);
        Assert.Contains(".NET Course", html);
        Assert.Contains(invoice.TotalAmount.ToString("N2", CultureInfo.InvariantCulture), html);
    }

    [Fact]
    public void Build_HtmlEncodesUserSuppliedFields()
    {
        var invoice = BuildInvoice(customerName: "<script>alert(1)</script>");

        var html = InvoiceHtmlTemplate.Build(invoice);

        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
    }

    [Fact]
    public void Build_OmitsNotesSectionWhenNotesIsNull()
    {
        var invoice = BuildInvoice(notes: null);

        var html = InvoiceHtmlTemplate.Build(invoice);

        Assert.DoesNotContain("class=\"notes\"", html);
    }

    [Fact]
    public void Build_IncludesNotesSectionWhenNotesProvided()
    {
        var invoice = BuildInvoice(notes: "Thank you for your business!");

        var html = InvoiceHtmlTemplate.Build(invoice);

        Assert.Contains("Thank you for your business!", html);
        Assert.Contains("class=\"notes\"", html);
    }
}
