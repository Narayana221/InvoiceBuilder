using InvoiceBuilder.Invoices.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Invoices.Services;

public interface IInvoiceNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}

public class InvoiceNumberGenerator(InvoicesDbContext db) : IInvoiceNumberGenerator
{
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var prefix = $"INV-{DateTime.UtcNow.Year}-";

        var lastNumber = await db.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.InvoiceNumber.StartsWith(prefix))
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSequence = 1;
        if (lastNumber is not null && int.TryParse(lastNumber.AsSpan(prefix.Length), out var lastSequence))
        {
            nextSequence = lastSequence + 1;
        }

        return $"{prefix}{nextSequence:D4}";
    }
}
