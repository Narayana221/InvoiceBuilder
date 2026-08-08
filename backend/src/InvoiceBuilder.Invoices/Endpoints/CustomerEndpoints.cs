using InvoiceBuilder.Invoices.Contracts;
using InvoiceBuilder.Invoices.Data;
using InvoiceBuilder.Invoices.Domain;
using InvoiceBuilder.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace InvoiceBuilder.Invoices.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers");

        group.MapGet("/", async (int? page, int? pageSize, InvoicesDbContext db) =>
        {
            var (normalizedPage, normalizedPageSize) = PageRequest.Normalize(page, pageSize);

            var query = db.Customers.OrderBy(c => c.Name);
            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(c => c.ToDto())
                .ToListAsync();

            return Results.Ok(new PagedResult<CustomerDto>
            {
                Items = items,
                Page = normalizedPage,
                PageSize = normalizedPageSize,
                TotalCount = totalCount
            });
        });

        group.MapGet("/{id:guid}", async (Guid id, InvoicesDbContext db) =>
        {
            var customer = await db.Customers.FindAsync(id);
            return customer is null ? Results.NotFound() : Results.Ok(customer.ToDto());
        });

        group.MapPost("/", async (CustomerRequest request, InvoicesDbContext db) =>
        {
            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            customer.ApplyRequest(request);

            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            return Results.Created($"/api/customers/{customer.Id}", customer.ToDto());
        }).AddEndpointFilter<ValidationFilter<CustomerRequest>>();

        group.MapPut("/{id:guid}", async (Guid id, CustomerRequest request, InvoicesDbContext db) =>
        {
            var customer = await db.Customers.FindAsync(id);
            if (customer is null)
            {
                return Results.NotFound();
            }

            customer.ApplyRequest(request);
            customer.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(customer.ToDto());
        }).AddEndpointFilter<ValidationFilter<CustomerRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, InvoicesDbContext db) =>
        {
            var customer = await db.Customers.FindAsync(id);
            if (customer is null)
            {
                return Results.NotFound();
            }

            customer.IsDeleted = true;
            customer.DeletedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.NoContent();
        });

        return app;
    }
}
