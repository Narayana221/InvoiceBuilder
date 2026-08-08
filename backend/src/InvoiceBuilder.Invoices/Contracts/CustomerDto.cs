namespace InvoiceBuilder.Invoices.Contracts;

public record CustomerDto(
    Guid Id,
    string Name,
    string? ContactName,
    string AddressLine,
    string City,
    string? PostalCode,
    string Country,
    string? Email,
    string? TaxId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CustomerRequest(
    string Name,
    string? ContactName,
    string AddressLine,
    string City,
    string? PostalCode,
    string Country,
    string? Email,
    string? TaxId);
