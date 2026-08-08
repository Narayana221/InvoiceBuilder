using InvoiceBuilder.Invoices.Contracts;
using InvoiceBuilder.Invoices.Data;
using InvoiceBuilder.Invoices.Domain;
using InvoiceBuilder.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Invoices.Endpoints;

public static class SenderEndpoints
{
    public static IEndpointRouteBuilder MapSenderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/senders");

        group.MapGet("/", async (int? page, int? pageSize, InvoicesDbContext db) =>
        {
            var (normalizedPage, normalizedPageSize) = PageRequest.Normalize(page, pageSize);

            var query = db.Senders.OrderBy(s => s.Name);
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(s => s.ToDto())
                .ToListAsync();

            return Results.Ok(new PagedResult<SenderDto>
            {
                Items = items,
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                TotalCount = totalCount
            });
        });

        group.MapGet("/{id:guid}", async (Guid id, InvoicesDbContext db) =>
        {
            var sender = await db.Senders.FindAsync(id);
            return sender is null ? Results.NotFound() : Results.Ok(sender.ToDto());
        });

        group.MapPost("/", async (SenderRequest request, InvoicesDbContext db) =>
        {
            var sender = new Sender
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            sender.ApplyRequest(request);

            db.Senders.Add(sender);
            await db.SaveChangesAsync();

            return Results.Created($"/api/senders/{sender.Id}", sender.ToDto());
        }).AddEndpointFilter<ValidationFilter<SenderRequest>>();

        group.MapPut("/{id:guid}", async (Guid id, SenderRequest request, InvoicesDbContext db) =>
        {
            var sender = await db.Senders.FindAsync(id);
            if (sender is null)
            {
                return Results.NotFound();
            }

            sender.ApplyRequest(request);
            sender.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(sender.ToDto());
        }).AddEndpointFilter<ValidationFilter<SenderRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, InvoicesDbContext db) =>
        {
            var sender = await db.Senders.FindAsync(id);
            if (sender is null)
            {
                return Results.NotFound();
            }

            sender.IsDeleted = true;
            sender.DeletedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return app;
    }
}
