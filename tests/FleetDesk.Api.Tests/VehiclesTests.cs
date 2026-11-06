using System.Net;
using System.Net.Http.Json;
using FleetDesk.Api.Dtos;

namespace FleetDesk.Api.Tests;

public class VehiclesTests : ApiTestBase
{
    [Fact]
    public async Task GetPaged_ReturnsSeededVehicles()
    {
        var client = await AsMechanic1Async();

        var response = await client.GetAsync("/api/vehicles?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<VehicleDto>>();
        Assert.Equal(3, page!.TotalCount);
    }

    [Fact]
    public async Task GetPaged_OverdueOnly_ReturnsOnlyOverdueVehicles()
    {
        var client = await AsFleetManagerAsync();

        var response = await client.GetAsync("/api/vehicles?overdueOnly=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<VehicleDto>>();
        Assert.All(page!.Items, v => Assert.True(v.IsOverdue));
        // FLT-100 (7 meses desde el ultimo servicio) y FLT-102 (nunca atendido) deben salir vencidos.
        Assert.Contains(page.Items, v => v.PlateNumber == "FLT-100");
        Assert.Contains(page.Items, v => v.PlateNumber == "FLT-102");
        Assert.DoesNotContain(page.Items, v => v.PlateNumber == "FLT-101");
    }

    [Fact]
    public async Task Create_AsAdmin_Succeeds()
    {
        var client = await AsAdminAsync();
        var request = new CreateVehicleRequest("FLT-200", "Toyota", "Hilux", 2023, 5000, 10000, 6);

        var response = await client.PostAsJsonAsync("/api/vehicles", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<VehicleDto>();
        Assert.Equal("FLT-200", created!.PlateNumber);
    }

    [Fact]
    public async Task Create_AsMechanic_IsForbidden()
    {
        var client = await AsMechanic1Async();
        var request = new CreateVehicleRequest("FLT-201", "Toyota", "Hilux", 2023, 5000, 10000, 6);

        var response = await client.PostAsJsonAsync("/api/vehicles", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicatePlate_ReturnsConflict()
    {
        var client = await AsAdminAsync();
        var request = new CreateVehicleRequest("FLT-100", "Toyota", "Hilux", 2023, 5000, 10000, 6);

        var response = await client.PostAsJsonAsync("/api/vehicles", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
