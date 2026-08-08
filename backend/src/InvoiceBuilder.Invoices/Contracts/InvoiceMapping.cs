using InvoiceBuilder.Invoices.Domain;

namespace InvoiceBuilder.Invoices.Contracts;

// Callers must have loaded Customer/Sender (and LineItems, for ToDto) via Include — these throw on unloaded navigations.
public static class InvoiceMapping
{
    public static InvoiceLineItemDto ToDto(this InvoiceLineItem lineItem) => new(
        lineItem.Id,
        lineItem.Description,
        lineItem.Quantity,
        lineItem.UnitPrice,
        lineItem.LineTotal);

    public static InvoiceSummaryDto ToSummaryDto(this Invoice invoice) => new(
        invoice.Id,
        invoice.InvoiceNumber,
        invoice.Customer.Name,
        invoice.Sender.Name,
        invoice.InvoiceDate,
        invoice.DueDate,
        invoice.Currency,
        invoice.TotalAmount);

    public static InvoiceDto ToDto(this Invoice invoice) => new(
        invoice.Id,
        invoice.InvoiceNumber,
        invoice.Currency,
        invoice.InvoiceDate,
        invoice.DueDate,
        invoice.CustomerId,
        invoice.Customer.Name,
        invoice.SenderId,
        invoice.Sender.Name,
        invoice.TaxRatePercent,
        invoice.Notes,
        invoice.SubtotalAmount,
        invoice.TaxAmount,
        invoice.TotalAmount,
        invoice.LineItems.Select(li => li.ToDto()).ToList(),
        invoice.CreatedAtUtc,
        invoice.UpdatedAtUtc);
}
