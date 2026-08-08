namespace InvoiceBuilder.Invoices.Contracts;

public record SenderDto(
    Guid Id,
    string Name,
    string? ContactName,
    string AddressLine,
    string City,
    string? PostalCode,
    string Country,
    string? Email,
    string? TaxId,
    string? BankDetails,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record SenderRequest(
    string Name,
    string? ContactName,
    string AddressLine,
    string City,
    string? PostalCode,
    string Country,
    string? Email,
    string? TaxId,
    string? BankDetails);
