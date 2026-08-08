namespace InvoiceBuilder.Invoices.Contracts;

public record InvoiceLineItemDto(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public record InvoiceLineItemRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice);

public record InvoiceSummaryDto(
    Guid Id,
    string InvoiceNumber,
    string CustomerName,
    string SenderName,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string Currency,
    decimal TotalAmount);

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string Currency,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    Guid CustomerId,
    string CustomerName,
    Guid SenderId,
    string SenderName,
    decimal TaxRatePercent,
    string? Notes,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    IReadOnlyList<InvoiceLineItemDto> LineItems,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record InvoiceRequest(
    DateOnly InvoiceDate,
    DateOnly DueDate,
    Guid CustomerId,
    Guid SenderId,
    string Currency,
    decimal TaxRatePercent,
    string? Notes,
    IReadOnlyList<InvoiceLineItemRequest> LineItems);
