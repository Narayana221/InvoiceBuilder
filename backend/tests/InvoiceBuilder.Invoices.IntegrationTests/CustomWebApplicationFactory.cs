using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

// xUnit runs separate test classes in parallel by default. Every class here wipes and
// recreates the whole invoicebuilder_test schema in IAsyncLifetime, sharing one physical
// database — running two classes at once means one class's EnsureDeleted can drop tables
// out from under another class's in-flight test. Disabled for the whole assembly rather
// than per-class, since any new integration test class would hit the same problem.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

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
