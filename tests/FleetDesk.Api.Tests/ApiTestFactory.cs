using FleetDesk.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FleetDesk.Api.Tests;

/// Reemplaza SQL Server por SQLite en memoria para que las pruebas corran sin dependencias externas.
/// Usa EnsureCreated en vez de Migrate: las migraciones se generaron para SQL Server y algunos
/// tipos de columna (p.ej. nvarchar(max)) no son validos en SQLite.
public class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Microsoft.EntityFrameworkCore.DbContextOptions<FleetDeskDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            _connection.Open();
            services.AddDbContext<FleetDeskDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FleetDeskDbContext>();
        db.Database.EnsureCreated();
        DbSeeder.Seed(db);

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
