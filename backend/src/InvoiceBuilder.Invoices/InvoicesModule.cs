using FluentValidation;
using InvoiceBuilder.Invoices.Contracts;
using InvoiceBuilder.Invoices.Data;
using InvoiceBuilder.Invoices.Endpoints;
using InvoiceBuilder.Invoices.Pdf;
using InvoiceBuilder.Invoices.Services;
using IronPdf;
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

        services.AddScoped<IInvoiceNumberGenerator, InvoiceNumberGenerator>();

        services.AddScoped<IValidator<CustomerRequest>, CustomerRequestValidator>();
        services.AddScoped<IValidator<SenderRequest>, SenderRequestValidator>();
        services.AddScoped<IValidator<InvoiceRequest>, InvoiceRequestValidator>();

        services.Configure<IronPdfOptions>(configuration.GetSection("IronPdf"));
        services.AddScoped<IInvoicePdfRenderer, InvoicePdfRenderer>();

        var licenseKey = configuration["IronPdf:LicenseKey"];
        if (!string.IsNullOrWhiteSpace(licenseKey))
        {
            License.LicenseKey = licenseKey;
        }

        return services;
    }

    public static IApplicationBuilder MigrateInvoicesDatabase(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InvoicesDbContext>();
        db.Database.Migrate();
        return app;
    }

    public static IEndpointRouteBuilder MapInvoicesModule(this IEndpointRouteBuilder app)
    {
        app.MapCustomerEndpoints();
        app.MapSenderEndpoints();
        app.MapInvoiceEndpoints();

        return app;
    }
}
