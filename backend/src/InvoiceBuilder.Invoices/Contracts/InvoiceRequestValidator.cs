using FluentValidation;
using InvoiceBuilder.Invoices.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Invoices.Contracts;

public class InvoiceRequestValidator : AbstractValidator<InvoiceRequest>
{
    public InvoiceRequestValidator(InvoicesDbContext db)
    {
        RuleFor(x => x.InvoiceDate).NotEqual(default(DateOnly));
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.InvoiceDate);

        RuleFor(x => x.Currency).NotEmpty().Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a 3-letter uppercase ISO code, e.g. USD.");

        RuleFor(x => x.TaxRatePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.CustomerId)
            .MustAsync(async (id, cancellationToken) => await db.Customers.AnyAsync(c => c.Id == id, cancellationToken))
            .WithMessage("Customer does not exist.");

        RuleFor(x => x.SenderId)
            .MustAsync(async (id, cancellationToken) => await db.Senders.AnyAsync(s => s.Id == id, cancellationToken))
            .WithMessage("Sender does not exist.");

        RuleFor(x => x.LineItems).NotEmpty().WithMessage("At least one line item is required.");
        RuleForEach(x => x.LineItems).SetValidator(new InvoiceLineItemRequestValidator());
    }
}
