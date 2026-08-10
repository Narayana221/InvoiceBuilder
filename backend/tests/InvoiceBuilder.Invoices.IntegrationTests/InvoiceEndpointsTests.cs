using System.Net;
using System.Net.Http.Json;
using InvoiceBuilder.Invoices.Contracts;
using InvoiceBuilder.Invoices.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceBuilder.Invoices.IntegrationTests;

public class InvoiceEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public InvoiceEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InvoicesDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InvoicesDbContext>();
        await db.Database.EnsureDeletedAsync();
    }

    private async Task<(Guid CustomerId, Guid SenderId)> CreateCustomerAndSenderAsync()
    {
        var customerResponse = await _client.PostAsJsonAsync("/api/customers", new CustomerRequest(
            "Acme Corp", "John Doe", "123 Main St", "Springfield", "12345", "USA", "john@acme.test", "VAT-123"));
        var customer = await customerResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var senderResponse = await _client.PostAsJsonAsync("/api/senders", new SenderRequest(
            "My Company LLC", "Alice Smith", "456 Market St", "City", null, "USA", null, "TAX-987654", null));
        var sender = await senderResponse.Content.ReadFromJsonAsync<SenderDto>();

        return (customer!.Id, sender!.Id);
    }

    private static InvoiceRequest ValidRequest(Guid customerId, Guid senderId) => new(
        InvoiceDate: new DateOnly(2026, 8, 1),
        DueDate: new DateOnly(2026, 8, 15),
        CustomerId: customerId,
        SenderId: senderId,
        Currency: "USD",
        TaxRatePercent: 20,
        Notes: "Thanks for your business!",
        LineItems:
        [
            new InvoiceLineItemRequest("Widget", 2, 50m),
            new InvoiceLineItemRequest("Gadget", 1, 25m),
        ]);

    [Fact]
    public async Task CreateGetUpdateDelete_Invoice_RoundTripsCorrectlyWithCorrectTotals()
    {
        var (customerId, senderId) = await CreateCustomerAndSenderAsync();
        var createRequest = ValidRequest(customerId, senderId);

        var createResponse = await _client.PostAsJsonAsync("/api/invoices", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.NotNull(created);
        Assert.False(string.IsNullOrWhiteSpace(created!.InvoiceNumber));

        // subtotal = 2*50 + 1*25 = 125, tax = 125 * 0.20 = 25, total = 150
        Assert.Equal(125m, created.SubtotalAmount);
        Assert.Equal(25m, created.TaxAmount);
        Assert.Equal(150m, created.TotalAmount);
        Assert.Equal(2, created.LineItems.Count);

        var getResponse = await _client.GetAsync($"/api/invoices/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateRequest = createRequest with { TaxRatePercent = 0 };
        var updateResponse = await _client.PutAsJsonAsync($"/api/invoices/{created.Id}", updateRequest);
        var updateBody = await updateResponse.Content.ReadAsStringAsync();
        Assert.True(updateResponse.StatusCode == HttpStatusCode.OK, $"Got {updateResponse.StatusCode}: {updateBody}");
        var updated = await updateResponse.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.Equal(0m, updated!.TaxAmount);
        Assert.Equal(125m, updated.TotalAmount);

        var deleteResponse = await _client.DeleteAsync($"/api/invoices/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDeleteResponse = await _client.GetAsync($"/api/invoices/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateInvoice_SecondInvoiceInSameYear_GetsIncrementingNumber()
    {
        var (customerId, senderId) = await CreateCustomerAndSenderAsync();

        var first = await _client.PostAsJsonAsync("/api/invoices", ValidRequest(customerId, senderId));
        var firstInvoice = await first.Content.ReadFromJsonAsync<InvoiceDto>();

        var second = await _client.PostAsJsonAsync("/api/invoices", ValidRequest(customerId, senderId));
        var secondInvoice = await second.Content.ReadFromJsonAsync<InvoiceDto>();

        Assert.NotEqual(firstInvoice!.InvoiceNumber, secondInvoice!.InvoiceNumber);

        // Numbers are zero-padded and sequential within a year, e.g. INV-2026-0001, INV-2026-0002.
        var firstSequence = int.Parse(firstInvoice.InvoiceNumber.Split('-')[^1]);
        var secondSequence = int.Parse(secondInvoice.InvoiceNumber.Split('-')[^1]);
        Assert.Equal(firstSequence + 1, secondSequence);
    }

    [Fact]
    public async Task CreateInvoice_WithDueDateBeforeInvoiceDate_ReturnsValidationProblem()
    {
        var (customerId, senderId) = await CreateCustomerAndSenderAsync();
        var invalidRequest = ValidRequest(customerId, senderId) with
        {
            InvoiceDate = new DateOnly(2026, 8, 15),
            DueDate = new DateOnly(2026, 8, 1),
        };

        var response = await _client.PostAsJsonAsync("/api/invoices", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvoice_WithNonexistentCustomerId_ReturnsValidationProblem()
    {
        var (_, senderId) = await CreateCustomerAndSenderAsync();
        var invalidRequest = ValidRequest(Guid.NewGuid(), senderId);

        var response = await _client.PostAsJsonAsync("/api/invoices", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvoice_WithNoLineItems_ReturnsValidationProblem()
    {
        var (customerId, senderId) = await CreateCustomerAndSenderAsync();
        var invalidRequest = ValidRequest(customerId, senderId) with { LineItems = [] };

        var response = await _client.PostAsJsonAsync("/api/invoices", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvoice_WithLowercaseCurrency_ReturnsValidationProblem()
    {
        var (customerId, senderId) = await CreateCustomerAndSenderAsync();
        var invalidRequest = ValidRequest(customerId, senderId) with { Currency = "usd" };

        var response = await _client.PostAsJsonAsync("/api/invoices", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
