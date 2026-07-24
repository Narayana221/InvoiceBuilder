using InvoiceBuilder.Invoices;
using InvoiceBuilder.Payments;
using InvoiceBuilder.Reports;
using InvoiceBuilder.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInvoicesModule(builder.Configuration)
    .AddUsersModule()
    .AddPaymentsModule()
    .AddReportsModule();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapInvoicesModule();
app.MapUsersModule();
app.MapPaymentsModule();
app.MapReportsModule();

app.Run();
