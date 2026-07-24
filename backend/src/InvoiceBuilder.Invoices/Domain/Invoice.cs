using InvoiceBuilder.Shared;

namespace InvoiceBuilder.Invoices.Domain;

public class Invoice : Entity, ISoftDeletable
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid SenderId { get; set; }
    public Sender Sender { get; set; } = null!;

    public decimal TaxRatePercent { get; set; }
    public string? Notes { get; set; }

    public decimal SubtotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }

    public List<InvoiceLineItem> LineItems { get; set; } = [];

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public void RecalculateTotals()
    {
        SubtotalAmount = LineItems.Sum(li => li.LineTotal);
        TaxAmount = Math.Round(SubtotalAmount * TaxRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        TotalAmount = SubtotalAmount + TaxAmount;
    }
}
