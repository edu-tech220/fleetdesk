using System.Net;
using System.Net.Http.Json;
using FleetDesk.Api.Dtos;

namespace FleetDesk.Api.Tests;

public class AuthTests : ApiTestBase
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "Admin123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Admin", body.Role);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/vehicles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
