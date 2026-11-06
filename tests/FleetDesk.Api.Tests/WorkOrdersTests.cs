using System.Net;
using System.Net.Http.Json;
using FleetDesk.Api.Domain;
using FleetDesk.Api.Dtos;

namespace FleetDesk.Api.Tests;

public class WorkOrdersTests : ApiTestBase
{
    private static async Task<int> GetVehicleIdByPlateAsync(HttpClient client, string plate)
    {
        var response = await client.GetAsync("/api/vehicles?pageSize=100");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<VehicleDto>>();
        return page!.Items.First(v => v.PlateNumber == plate).Id;
    }

    private static async Task<int> GetPartIdBySkuAsync(HttpClient client, string sku)
    {
        var response = await client.GetAsync("/api/parts?pageSize=100");
        var page = await response.Content.ReadFromJsonAsync<PagedResult<PartDto>>();
        return page!.Items.First(p => p.Sku == sku).Id;
    }

    private static async Task<PartDto> GetPartAsync(HttpClient client, int id)
    {
        var response = await client.GetAsync($"/api/parts/{id}");
        return (await response.Content.ReadFromJsonAsync<PartDto>())!;
    }

    [Fact]
    public async Task Create_OnActiveVehicle_PutsVehicleInShop()
    {
        var client = await AsMechanic1Async();
        var vehicleId = await GetVehicleIdByPlateAsync(client, "FLT-101");

        var response = await client.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Servicio mayor"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var vehicle = await (await client.GetAsync($"/api/vehicles/{vehicleId}")).Content.ReadFromJsonAsync<VehicleDto>();
        Assert.Equal(VehicleStatus.InShop, vehicle!.Status);
    }

    [Fact]
    public async Task Create_OnVehicleAlreadyInShop_ReturnsConflict()
    {
        var client = await AsMechanic1Async();
        var vehicleId = await GetVehicleIdByPlateAsync(client, "FLT-101");
        await client.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Primera orden"));

        var second = await client.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Segunda orden"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_WhenMechanicAtCapacity_ReturnsConflict()
    {
        var admin = await AsAdminAsync();
        await admin.PostAsJsonAsync("/api/vehicles", new CreateVehicleRequest("FLT-300", "Kia", "K2500", 2022, 1000, 10000, 6));

        var mechanic = await AsMechanic1Async();
        foreach (var plate in new[] { "FLT-100", "FLT-101", "FLT-102" })
        {
            var vehicleId = await GetVehicleIdByPlateAsync(mechanic, plate);
            var created = await mechanic.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Orden"));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }

        var extraVehicleId = await GetVehicleIdByPlateAsync(mechanic, "FLT-300");
        var fourth = await mechanic.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(extraVehicleId, "Cuarta orden"));

        Assert.Equal(HttpStatusCode.Conflict, fourth.StatusCode);
    }

    [Fact]
    public async Task Start_TwiceOnSameOrder_SecondCallReturnsConflict()
    {
        var client = await AsMechanic1Async();
        var vehicleId = await GetVehicleIdByPlateAsync(client, "FLT-101");
        var created = await (await client.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Orden")))
            .Content.ReadFromJsonAsync<WorkOrderDto>();

        var first = await client.PostAsync($"/api/workorders/{created!.Id}/start", null);
        var second = await client.PostAsync($"/api/workorders/{created.Id}/start", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task AddPart_InsufficientStock_SetsWaitingPartsAndReturnsConflict()
    {
        var client = await AsMechanic1Async();
        var vehicleId = await GetVehicleIdByPlateAsync(client, "FLT-101");
        var partId = await GetPartIdBySkuAsync(client, "TIR-020"); // stock sembrado: 4
        var workOrder = await (await client.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Cambio de llantas")))
            .Content.ReadFromJsonAsync<WorkOrderDto>();
        await client.PostAsync($"/api/workorders/{workOrder!.Id}/start", null);
        var part = await GetPartAsync(client, partId);

        var response = await client.PostAsJsonAsync($"/api/workorders/{workOrder.Id}/parts",
            new AddPartRequest(partId, 10, part.RowVersion));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var updated = await (await client.GetAsync($"/api/workorders/{workOrder.Id}")).Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.Equal(WorkOrderStatus.WaitingParts, updated!.Status);
    }

    [Fact]
    public async Task AddPart_SufficientStock_DecrementsStock()
    {
        var client = await AsMechanic1Async();
        var vehicleId = await GetVehicleIdByPlateAsync(client, "FLT-101");
        var partId = await GetPartIdBySkuAsync(client, "FLT-001"); // stock sembrado: 25
        var workOrder = await (await client.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Cambio de filtro")))
            .Content.ReadFromJsonAsync<WorkOrderDto>();
        await client.PostAsync($"/api/workorders/{workOrder!.Id}/start", null);
        var part = await GetPartAsync(client, partId);

        var response = await client.PostAsJsonAsync($"/api/workorders/{workOrder.Id}/parts",
            new AddPartRequest(partId, 2, part.RowVersion));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updatedPart = await GetPartAsync(client, partId);
        Assert.Equal(23, updatedPart.StockQuantity);
    }

    [Fact]
    public async Task AddPart_WithStaleRowVersion_ReturnsConflict()
    {
        var client = await AsMechanic1Async();
        var admin = await AsAdminAsync();
        var vehicleId = await GetVehicleIdByPlateAsync(client, "FLT-101");
        var partId = await GetPartIdBySkuAsync(client, "FLT-001");
        var workOrder = await (await client.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Cambio de filtro")))
            .Content.ReadFromJsonAsync<WorkOrderDto>();
        await client.PostAsync($"/api/workorders/{workOrder!.Id}/start", null);

        var stalePart = await GetPartAsync(client, partId);
        // Un administrador reabastece el inventario mientras tanto, avanzando la RowVersion.
        await admin.PostAsJsonAsync($"/api/parts/{partId}/restock", new RestockPartRequest(5, stalePart.RowVersion));

        var response = await client.PostAsJsonAsync($"/api/workorders/{workOrder.Id}/parts",
            new AddPartRequest(partId, 1, stalePart.RowVersion));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Complete_UpdatesVehicleOdometerAndReturnsItToActive()
    {
        var client = await AsMechanic1Async();
        var vehicleId = await GetVehicleIdByPlateAsync(client, "FLT-101");
        var vehicleBefore = await (await client.GetAsync($"/api/vehicles/{vehicleId}")).Content.ReadFromJsonAsync<VehicleDto>();
        var workOrder = await (await client.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Servicio")))
            .Content.ReadFromJsonAsync<WorkOrderDto>();
        await client.PostAsync($"/api/workorders/{workOrder!.Id}/start", null);

        var newOdometer = vehicleBefore!.Odometer + 500;
        var response = await client.PostAsJsonAsync($"/api/workorders/{workOrder.Id}/complete",
            new CompleteWorkOrderRequest(newOdometer));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var vehicleAfter = await (await client.GetAsync($"/api/vehicles/{vehicleId}")).Content.ReadFromJsonAsync<VehicleDto>();
        Assert.Equal(VehicleStatus.Active, vehicleAfter!.Status);
        Assert.Equal(newOdometer, vehicleAfter.Odometer);
        Assert.Equal(newOdometer, vehicleAfter.LastServiceOdometer);
    }

    [Fact]
    public async Task Complete_WithOdometerLowerThanOpening_ReturnsConflict()
    {
        var client = await AsMechanic1Async();
        var vehicleId = await GetVehicleIdByPlateAsync(client, "FLT-101");
        var workOrder = await (await client.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Servicio")))
            .Content.ReadFromJsonAsync<WorkOrderDto>();
        await client.PostAsync($"/api/workorders/{workOrder!.Id}/start", null);

        var response = await client.PostAsJsonAsync($"/api/workorders/{workOrder.Id}/complete",
            new CompleteWorkOrderRequest(workOrder.OdometerAtOpen - 10));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Complete_WhenStillPending_ReturnsConflict()
    {
        var client = await AsMechanic1Async();
        var vehicleId = await GetVehicleIdByPlateAsync(client, "FLT-101");
        var workOrder = await (await client.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Servicio")))
            .Content.ReadFromJsonAsync<WorkOrderDto>();

        var response = await client.PostAsJsonAsync($"/api/workorders/{workOrder!.Id}/complete",
            new CompleteWorkOrderRequest(workOrder.OdometerAtOpen + 100));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ReturnsVehicleToActive()
    {
        var client = await AsMechanic1Async();
        var vehicleId = await GetVehicleIdByPlateAsync(client, "FLT-101");
        var workOrder = await (await client.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Servicio")))
            .Content.ReadFromJsonAsync<WorkOrderDto>();

        var response = await client.PostAsync($"/api/workorders/{workOrder!.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var vehicle = await (await client.GetAsync($"/api/vehicles/{vehicleId}")).Content.ReadFromJsonAsync<VehicleDto>();
        Assert.Equal(VehicleStatus.Active, vehicle!.Status);
    }

    [Fact]
    public async Task ModifyOrder_AsDifferentMechanic_IsForbidden()
    {
        var mechanic1 = await AsMechanic1Async();
        var mechanic2 = await AsMechanic2Async();
        var vehicleId = await GetVehicleIdByPlateAsync(mechanic1, "FLT-101");
        var workOrder = await (await mechanic1.PostAsJsonAsync("/api/workorders", new CreateWorkOrderRequest(vehicleId, "Servicio")))
            .Content.ReadFromJsonAsync<WorkOrderDto>();

        var response = await mechanic2.PostAsync($"/api/workorders/{workOrder!.Id}/start", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
