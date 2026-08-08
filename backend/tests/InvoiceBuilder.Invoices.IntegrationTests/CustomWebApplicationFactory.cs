using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace InvoiceBuilder.Invoices.IntegrationTests;

// Points the API host at a dedicated invoicebuilder_test database (same Postgres
// server as dev) so integration tests never read or write the dev database.
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=localhost;Port=5433;Database=invoicebuilder_test;Username=invoicebuilder;Password=invoicebuilder"
            });
        });
    }
}
