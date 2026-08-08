using InvoiceBuilder.Invoices.Domain;
using Xunit;

namespace InvoiceBuilder.Invoices.UnitTests;

public class InvoiceRecalculateTotalsTests
{
    [Fact]
    public void RecalculateTotals_WithNoLineItems_SetsAllAmountsToZero()
    {
        var invoice = new Invoice { TaxRatePercent = 20 };

        invoice.RecalculateTotals();

        Assert.Equal(0m, invoice.SubtotalAmount);
        Assert.Equal(0m, invoice.TaxAmount);
        Assert.Equal(0m, invoice.TotalAmount);
    }

    [Fact]
    public void RecalculateTotals_SumsLineItemsAndAppliesTaxRate()
    {
        var invoice = new Invoice
        {
            TaxRatePercent = 20,
            LineItems =
            [
                new InvoiceLineItem { Quantity = 2, UnitPrice = 29.99m },
                new InvoiceLineItem { Quantity = 1, UnitPrice = 89.99m }
            ]
        };

        invoice.RecalculateTotals();

        Assert.Equal(149.97m, invoice.SubtotalAmount);
        Assert.Equal(29.99m, invoice.TaxAmount); // 149.97 * 0.20 = 29.994 -> rounds to 29.99
        Assert.Equal(179.96m, invoice.TotalAmount);
    }

    [Fact]
    public void RecalculateTotals_WithZeroTaxRate_TaxAmountIsZero()
    {
        var invoice = new Invoice
        {
            TaxRatePercent = 0,
            LineItems = [new InvoiceLineItem { Quantity = 1, UnitPrice = 100m }]
        };

        invoice.RecalculateTotals();

        Assert.Equal(100m, invoice.SubtotalAmount);
        Assert.Equal(0m, invoice.TaxAmount);
        Assert.Equal(100m, invoice.TotalAmount);
    }
}

public class InvoiceLineItemTests
{
    [Fact]
    public void LineTotal_IsQuantityTimesUnitPrice()
    {
        var lineItem = new InvoiceLineItem { Quantity = 3, UnitPrice = 12.50m };

        Assert.Equal(37.50m, lineItem.LineTotal);
    }
}
