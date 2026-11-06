using System.Net.Http.Headers;
using System.Net.Http.Json;
using FleetDesk.Api.Dtos;

namespace FleetDesk.Api.Tests;

/// Cada prueba crea su propia instancia de fabrica/servidor/BD en memoria, totalmente aislada.
public abstract class ApiTestBase : IDisposable
{
    protected readonly ApiTestFactory Factory;
    protected readonly HttpClient Client;

    protected ApiTestBase()
    {
        Factory = new ApiTestFactory();
        Client = Factory.CreateClient();
    }

    protected async Task<HttpClient> AuthorizedClientAsync(string username, string password)
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    protected Task<HttpClient> AsAdminAsync() => AuthorizedClientAsync("admin", "Admin123!");
    protected Task<HttpClient> AsMechanic1Async() => AuthorizedClientAsync("mecanico1", "Mecanico123!");
    protected Task<HttpClient> AsMechanic2Async() => AuthorizedClientAsync("mecanico2", "Mecanico123!");
    protected Task<HttpClient> AsFleetManagerAsync() => AuthorizedClientAsync("gerente", "Gerente123!");

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
