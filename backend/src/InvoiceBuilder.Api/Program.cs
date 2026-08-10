using InvoiceBuilder.Invoices;
using InvoiceBuilder.Payments;
using InvoiceBuilder.Reports;
using InvoiceBuilder.Users;

var builder = WebApplication.CreateBuilder(args);

const string frontendCorsPolicy = "Frontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services
    .AddInvoicesModule(builder.Configuration)
    .AddUsersModule()
    .AddPaymentsModule()
    .AddReportsModule();

var app = builder.Build();

app.UseCors(frontendCorsPolicy);

app.MigrateInvoicesDatabase();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapInvoicesModule();
app.MapUsersModule();
app.MapPaymentsModule();
app.MapReportsModule();

app.Run();
