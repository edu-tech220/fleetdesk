using System.Net;
using System.Net.Http.Json;
using FleetDesk.Api.Dtos;

namespace FleetDesk.Api.Tests;

public class PartsTests : ApiTestBase
{
    [Fact]
    public async Task GetPaged_ReturnsSeededParts()
    {
        var client = await AsMechanic1Async();

        var response = await client.GetAsync("/api/parts?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PartDto>>();
        Assert.Equal(5, page!.TotalCount);
    }

    [Fact]
    public async Task Restock_WithStaleRowVersion_ReturnsConflict()
    {
        var client = await AsAdminAsync();
        var listResponse = await client.GetAsync("/api/parts?pageSize=100");
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<PartDto>>();
        var part = page!.Items.First(p => p.Sku == "FLT-001");

        // Primer reabastecimiento: avanza la RowVersion en el servidor.
        var firstAttempt = await client.PostAsJsonAsync($"/api/parts/{part.Id}/restock",
            new RestockPartRequest(5, part.RowVersion));
        Assert.Equal(HttpStatusCode.OK, firstAttempt.StatusCode);

        // Segundo intento reutilizando la RowVersion vieja: debe fallar por concurrencia.
        var staleAttempt = await client.PostAsJsonAsync($"/api/parts/{part.Id}/restock",
            new RestockPartRequest(5, part.RowVersion));
        Assert.Equal(HttpStatusCode.Conflict, staleAttempt.StatusCode);
    }

    [Fact]
    public async Task Create_AsMechanic_IsForbidden()
    {
        var client = await AsMechanic1Async();

        var response = await client.PostAsJsonAsync("/api/parts", new CreatePartRequest("NEW-001", "Banda", 10, 50m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
