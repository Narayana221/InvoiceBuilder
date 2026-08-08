using System.Net;
using System.Net.Http.Json;
using InvoiceBuilder.Invoices.Contracts;
using InvoiceBuilder.Invoices.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceBuilder.Invoices.IntegrationTests;

public class CustomerEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomerEndpointsTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task CreateGetUpdateDelete_Customer_RoundTripsCorrectly()
    {
        var createRequest = new CustomerRequest(
            "Acme Corp", "John Doe", "123 Main St", "Springfield", "12345", "USA", "john@acme.test", "VAT-123");

        var createResponse = await _client.PostAsJsonAsync("/api/customers", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.NotNull(created);
        Assert.Equal("Acme Corp", created!.Name);

        var getResponse = await _client.GetAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateRequest = createRequest with { Name = "Acme Corporation" };
        var updateResponse = await _client.PutAsJsonAsync($"/api/customers/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.Equal("Acme Corporation", updated!.Name);

        var deleteResponse = await _client.DeleteAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Soft delete: confirms the query filter actually excludes it, not just that the row is gone.
        var getAfterDeleteResponse = await _client.GetAsync($"/api/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_WithMissingName_ReturnsValidationProblem()
    {
        var invalidRequest = new CustomerRequest("", null, "123 Main St", "Springfield", null, "USA", null, null);

        var response = await _client.PostAsJsonAsync("/api/customers", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
