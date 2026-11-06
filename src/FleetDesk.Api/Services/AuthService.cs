using FleetDesk.Api.Auth;
using FleetDesk.Api.Data;
using FleetDesk.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FleetDesk.Api.Services;

public class AuthService : IAuthService
{
    private readonly FleetDeskDbContext _db;
    private readonly IJwtTokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(FleetDeskDbContext db, IJwtTokenService tokenService, IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return null;
        }

        var token = _tokenService.CreateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);

        return new LoginResponse(token, user.Username, user.Role, expiresAt);
    }
}
