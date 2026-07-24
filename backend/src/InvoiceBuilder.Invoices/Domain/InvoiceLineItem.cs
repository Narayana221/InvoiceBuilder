using InvoiceBuilder.Shared;

namespace InvoiceBuilder.Invoices.Domain;

public class InvoiceLineItem : Entity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;
}
