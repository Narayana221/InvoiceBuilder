using System.Net;
using System.Net.Http.Json;
using InvoiceBuilder.Invoices.Contracts;
using InvoiceBuilder.Invoices.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceBuilder.Invoices.IntegrationTests;

public class SenderEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SenderEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task CreateGetUpdateDelete_Sender_RoundTripsCorrectly()
    {
        var createRequest = new SenderRequest(
            "My Company LLC", "Alice Smith", "456 Market St", "City", null, "USA", null, "TAX-987654",
            "IBAN XX00 0000 0000 0000 00");

        var createResponse = await _client.PostAsJsonAsync("/api/senders", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<SenderDto>();
        Assert.NotNull(created);
        Assert.Equal("My Company LLC", created!.Name);

        var getResponse = await _client.GetAsync($"/api/senders/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var updateRequest = createRequest with { Name = "My Renamed Company LLC" };
        var updateResponse = await _client.PutAsJsonAsync($"/api/senders/{created.Id}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SenderDto>();
        Assert.Equal("My Renamed Company LLC", updated!.Name);

        var deleteResponse = await _client.DeleteAsync($"/api/senders/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Soft delete: confirms the query filter actually excludes it, not just that the row is gone.
        var getAfterDeleteResponse = await _client.GetAsync($"/api/senders/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateSender_WithMissingName_ReturnsValidationProblem()
    {
        var invalidRequest = new SenderRequest("", null, "456 Market St", "City", null, "USA", null, null, null);

        var response = await _client.PostAsJsonAsync("/api/senders", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
