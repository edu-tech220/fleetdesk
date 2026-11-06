namespace FleetDesk.Api.Domain;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Mechanic = "Mechanic";
    public const string FleetManager = "FleetManager";
}

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Mechanic;
}
