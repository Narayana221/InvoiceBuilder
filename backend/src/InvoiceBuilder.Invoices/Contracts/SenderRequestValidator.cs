using FluentValidation;

namespace InvoiceBuilder.Invoices.Contracts;

public class SenderRequestValidator : AbstractValidator<SenderRequest>
{
    public SenderRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).MaximumLength(200);
        RuleFor(x => x.AddressLine).NotEmpty().MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.TaxId).MaximumLength(50);
        RuleFor(x => x.BankDetails).MaximumLength(200);
    }
}
