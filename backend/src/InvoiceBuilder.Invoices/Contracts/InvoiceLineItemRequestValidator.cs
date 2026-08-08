using FluentValidation;

namespace InvoiceBuilder.Invoices.Contracts;

public class InvoiceLineItemRequestValidator : AbstractValidator<InvoiceLineItemRequest>
{
    public InvoiceLineItemRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}
