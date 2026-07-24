using InvoiceBuilder.Invoices.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceBuilder.Invoices;

public static class InvoicesModule
{
    public static IServiceCollection AddInvoicesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InvoicesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")));

        return services;
    }

    public static IEndpointRouteBuilder MapInvoicesModule(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
