using InvoiceBuilder.Invoices.Domain;

namespace InvoiceBuilder.Invoices.Contracts;

public static class CustomerMapping
{
    public static CustomerDto ToDto(this Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.ContactName,
        customer.AddressLine,
        customer.City,
        customer.PostalCode,
        customer.Country,
        customer.Email,
        customer.TaxId,
        customer.CreatedAtUtc,
        customer.UpdatedAtUtc);

    public static void ApplyRequest(this Customer customer, CustomerRequest request)
    {
        customer.Name = request.Name;
        customer.ContactName = request.ContactName;
        customer.AddressLine = request.AddressLine;
        customer.City = request.City;
        customer.PostalCode = request.PostalCode;
        customer.Country = request.Country;
        customer.Email = request.Email;
        customer.TaxId = request.TaxId;
    }
}
