using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceBuilder.Payments;

public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapPaymentsModule(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
