using InvoiceBuilder.Shared;

namespace InvoiceBuilder.Invoices.Domain;

public class Customer : Entity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? TaxId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
