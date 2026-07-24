using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceBuilder.Invoices;

public static class InvoicesModule
{
    public static IServiceCollection AddInvoicesModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapInvoicesModule(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
