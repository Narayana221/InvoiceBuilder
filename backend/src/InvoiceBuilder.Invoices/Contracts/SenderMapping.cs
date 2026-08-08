using InvoiceBuilder.Invoices.Domain;

namespace InvoiceBuilder.Invoices.Contracts;

public static class SenderMapping
{
    public static SenderDto ToDto(this Sender sender) => new(
        sender.Id,
        sender.Name,
        sender.ContactName,
        sender.AddressLine,
        sender.City,
        sender.PostalCode,
        sender.Country,
        sender.Email,
        sender.TaxId,
        sender.BankDetails,
        sender.CreatedAtUtc,
        sender.UpdatedAtUtc);

    public static void ApplyRequest(this Sender sender, SenderRequest request)
    {
        sender.Name = request.Name;
        sender.ContactName = request.ContactName;
        sender.AddressLine = request.AddressLine;
        sender.City = request.City;
        sender.PostalCode = request.PostalCode;
        sender.Country = request.Country;
        sender.Email = request.Email;
        sender.TaxId = request.TaxId;
        sender.BankDetails = request.BankDetails;
    }
}
