using System.Net;
using System.Net.Http.Json;
using FleetDesk.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace FleetDesk.Web.Services;

/// Cliente tipado hacia el microservicio FleetDesk.Api, usado por las Razor Pages via IHttpClientFactory.
public class FleetDeskApiClient
{
    private readonly HttpClient _http;

    public FleetDeskApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<LoginResponse?> LoginAsync(string username, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest(username, password));
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<LoginResponse>();
    }

    public Task<PagedResult<VehicleDto>> GetVehiclesAsync(int page, int pageSize, bool overdueOnly = false) =>
        GetAsync<PagedResult<VehicleDto>>($"api/vehicles?page={page}&pageSize={pageSize}&overdueOnly={overdueOnly}");

    public Task<VehicleDto> GetVehicleAsync(int id) => GetAsync<VehicleDto>($"api/vehicles/{id}");

    public Task<VehicleDto> CreateVehicleAsync(CreateVehicleRequest request) =>
        PostAsync<VehicleDto>("api/vehicles", request);

    public Task<PagedResult<PartDto>> GetPartsAsync(int page, int pageSize) =>
        GetAsync<PagedResult<PartDto>>($"api/parts?page={page}&pageSize={pageSize}");

    public Task<PartDto> GetPartAsync(int id) => GetAsync<PartDto>($"api/parts/{id}");

    public Task<PartDto> CreatePartAsync(CreatePartRequest request) => PostAsync<PartDto>("api/parts", request);

    public Task<PartDto> RestockPartAsync(int id, RestockPartRequest request) =>
        PostAsync<PartDto>($"api/parts/{id}/restock", request);

    public Task<PagedResult<WorkOrderDto>> GetWorkOrdersAsync(int page, int pageSize, WorkOrderStatus? status = null)
    {
        var query = $"api/workorders?page={page}&pageSize={pageSize}";
        if (status is not null)
        {
            query += $"&status={status}";
        }

        return GetAsync<PagedResult<WorkOrderDto>>(query);
    }

    public Task<WorkOrderDto> GetWorkOrderAsync(int id) => GetAsync<WorkOrderDto>($"api/workorders/{id}");

    public Task<WorkOrderDto> CreateWorkOrderAsync(CreateWorkOrderRequest request) =>
        PostAsync<WorkOrderDto>("api/workorders", request);

    public Task<WorkOrderDto> StartWorkOrderAsync(int id) =>
        PostAsync<WorkOrderDto>($"api/workorders/{id}/start", null);

    public Task<WorkOrderDto> AddPartToWorkOrderAsync(int id, AddPartRequest request) =>
        PostAsync<WorkOrderDto>($"api/workorders/{id}/parts", request);

    public Task<WorkOrderDto> CompleteWorkOrderAsync(int id, CompleteWorkOrderRequest request) =>
        PostAsync<WorkOrderDto>($"api/workorders/{id}/complete", request);

    public Task<WorkOrderDto> CancelWorkOrderAsync(int id) =>
        PostAsync<WorkOrderDto>($"api/workorders/{id}/cancel", null);

    public Task<PagedResult<VehicleDto>> GetOverdueVehiclesAsync(int page, int pageSize) =>
        GetAsync<PagedResult<VehicleDto>>($"api/reports/overdue-vehicles?page={page}&pageSize={pageSize}");

    private async Task<T> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<T> PostAsync<T>(string url, object? body)
    {
        var response = await _http.PostAsJsonAsync(url, body);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        var message = problem?.Detail ?? $"La solicitud al servicio de flota fallo ({(int)response.StatusCode}).";
        throw new ApiException(response.StatusCode, message);
    }
}
