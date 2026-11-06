using FleetDesk.Api.Auth;
using FleetDesk.Api.Domain;

namespace FleetDesk.Api.Data;

public static class DbSeeder
{
    public static void Seed(FleetDeskDbContext db)
    {
        if (!db.Users.Any())
        {
            db.Users.AddRange(CreateUser("admin", "Admin123!", Roles.Admin),
                CreateUser("mecanico1", "Mecanico123!", Roles.Mechanic),
                CreateUser("mecanico2", "Mecanico123!", Roles.Mechanic),
                CreateUser("gerente", "Gerente123!", Roles.FleetManager));
        }

        if (!db.Parts.Any())
        {
            db.Parts.AddRange(
                new Part { Sku = "FLT-001", Name = "Filtro de aceite", StockQuantity = 25, UnitCost = 180m },
                new Part { Sku = "FLT-002", Name = "Filtro de aire", StockQuantity = 15, UnitCost = 220m },
                new Part { Sku = "BRK-010", Name = "Juego de balatas delanteras", StockQuantity = 8, UnitCost = 950m },
                new Part { Sku = "OIL-005", Name = "Aceite de motor sintetico 5L", StockQuantity = 30, UnitCost = 650m },
                new Part { Sku = "TIR-020", Name = "Llanta 195/65R15", StockQuantity = 4, UnitCost = 1450m });
        }

        if (!db.Vehicles.Any())
        {
            db.Vehicles.AddRange(
                new Vehicle
                {
                    PlateNumber = "FLT-100", Brand = "Nissan", Model = "NP300", Year = 2021,
                    Odometer = 48000, IntervalKm = 10000, IntervalMonths = 6,
                    LastServiceOdometer = 38500, LastServiceDate = DateTime.UtcNow.AddMonths(-7)
                },
                new Vehicle
                {
                    PlateNumber = "FLT-101", Brand = "Chevrolet", Model = "Tornado", Year = 2022,
                    Odometer = 21000, IntervalKm = 10000, IntervalMonths = 6,
                    LastServiceOdometer = 15000, LastServiceDate = DateTime.UtcNow.AddMonths(-2)
                },
                new Vehicle
                {
                    PlateNumber = "FLT-102", Brand = "Ford", Model = "Transit", Year = 2020,
                    Odometer = 92000, IntervalKm = 10000, IntervalMonths = 6,
                    LastServiceOdometer = null, LastServiceDate = null
                });
        }

        db.SaveChanges();
    }

    private static AppUser CreateUser(string username, string password, string role)
    {
        var (hash, salt) = PasswordHasher.Hash(password);
        return new AppUser { Username = username, PasswordHash = hash, PasswordSalt = salt, Role = role };
    }
}
