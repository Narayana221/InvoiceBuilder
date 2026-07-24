using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceBuilder.Reports;

public static class ReportsModule
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapReportsModule(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
