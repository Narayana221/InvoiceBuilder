using InvoiceBuilder.Invoices.Contracts;
using InvoiceBuilder.Invoices.Data;
using InvoiceBuilder.Invoices.Domain;
using InvoiceBuilder.Invoices.Pdf;
using InvoiceBuilder.Invoices.Services;
using InvoiceBuilder.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Invoices.Endpoints;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices");

        group.MapGet("/", async (int? page, int? pageSize, InvoicesDbContext db) =>
        {
            var (normalizedPage, normalizedPageSize) = PageRequest.Normalize(page, pageSize);

            var query = db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Sender)
                .OrderByDescending(i => i.InvoiceDate);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToListAsync();

            return Results.Ok(new PagedResult<InvoiceSummaryDto>
            {
                Items = items.Select(i => i.ToSummaryDto()).ToList(),
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                TotalCount = totalCount
            });
        });

        group.MapGet("/{id:guid}", async (Guid id, InvoicesDbContext db) =>
        {
            var invoice = await db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Sender)
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == id);

            return invoice is null ? Results.NotFound() : Results.Ok(invoice.ToDto());
        });

        group.MapPost("/", async (InvoiceRequest request, InvoicesDbContext db, IInvoiceNumberGenerator numberGenerator) =>
        {
            var customer = await db.Customers.FindAsync(request.CustomerId);
            var sender = await db.Senders.FindAsync(request.SenderId);
            if (customer is null || sender is null)
            {
                return Results.BadRequest("Customer or Sender does not exist.");
            }

            var now = DateTime.UtcNow;
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = await numberGenerator.GenerateAsync(),
                Currency = request.Currency,
                InvoiceDate = request.InvoiceDate,
                DueDate = request.DueDate,
                CustomerId = customer.Id,
                Customer = customer,
                SenderId = sender.Id,
                Sender = sender,
                TaxRatePercent = request.TaxRatePercent,
                Notes = request.Notes,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                LineItems = request.LineItems.Select(li => new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }).ToList()
            };
            invoice.RecalculateTotals();

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            return Results.Created($"/api/invoices/{invoice.Id}", invoice.ToDto());
        }).AddEndpointFilter<ValidationFilter<InvoiceRequest>>();

        group.MapPut("/{id:guid}", async (Guid id, InvoiceRequest request, InvoicesDbContext db) =>
        {
            var invoice = await db.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (invoice is null)
            {
                return Results.NotFound();
            }

            var customer = await db.Customers.FindAsync(request.CustomerId);
            var sender = await db.Senders.FindAsync(request.SenderId);
            if (customer is null || sender is null)
            {
                return Results.BadRequest("Customer or Sender does not exist.");
            }

            var now = DateTime.UtcNow;
            invoice.CustomerId = customer.Id;
            invoice.Customer = customer;
            invoice.SenderId = sender.Id;
            invoice.Sender = sender;
            invoice.Currency = request.Currency;
            invoice.InvoiceDate = request.InvoiceDate;
            invoice.DueDate = request.DueDate;
            invoice.TaxRatePercent = request.TaxRatePercent;
            invoice.Notes = request.Notes;
            invoice.UpdatedAtUtc = now;

            invoice.LineItems.Clear();
            foreach (var lineItemRequest in request.LineItems)
            {
                invoice.LineItems.Add(new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    Description = lineItemRequest.Description,
                    Quantity = lineItemRequest.Quantity,
                    UnitPrice = lineItemRequest.UnitPrice,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
            invoice.RecalculateTotals();

            await db.SaveChangesAsync();

            return Results.Ok(invoice.ToDto());
        }).AddEndpointFilter<ValidationFilter<InvoiceRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, InvoicesDbContext db) =>
        {
            var invoice = await db.Invoices.FindAsync(id);
            if (invoice is null)
            {
                return Results.NotFound();
            }

            invoice.IsDeleted = true;
            invoice.DeletedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        group.MapGet("/{id:guid}/pdf", async (Guid id, InvoicesDbContext db, IInvoicePdfRenderer pdfRenderer) =>
        {
            var invoice = await db.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Sender)
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice is null)
            {
                return Results.NotFound();
            }

            try
            {
                var pdfBytes = pdfRenderer.Render(invoice);
                return Results.File(pdfBytes, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
            }
            catch (Exception ex)
            {
                const int maxDetailLength = 200;
                var detail = ex.Message.Length > maxDetailLength
                    ? ex.Message[..maxDetailLength] + "…"
                    : ex.Message;

                return Results.Problem(
                    title: "PDF generation failed",
                    detail: detail,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        return app;
    }
}
