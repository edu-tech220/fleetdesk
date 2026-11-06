namespace FleetDesk.Api.Dtos;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, string Username, string Role, DateTime ExpiresAtUtc);
