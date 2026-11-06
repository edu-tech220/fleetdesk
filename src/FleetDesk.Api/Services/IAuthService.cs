using FleetDesk.Api.Dtos;

namespace FleetDesk.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
