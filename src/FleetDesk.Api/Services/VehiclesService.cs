using FleetDesk.Api.Data;
using FleetDesk.Api.Domain;
using FleetDesk.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FleetDesk.Api.Services;

public class VehiclesService : IVehiclesService
{
    private readonly FleetDeskDbContext _db;

    public VehiclesService(FleetDeskDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<VehicleDto>> GetPagedAsync(int page, int pageSize, bool overdueOnly)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Vehicles.AsNoTracking().OrderBy(v => v.PlateNumber);
        var today = DateTime.UtcNow.Date;

        var all = await query.ToListAsync();
        var mapped = all.Select(v => ToDto(v, today));
        if (overdueOnly)
        {
            mapped = mapped.Where(v => v.IsOverdue);
        }

        var materialized = mapped.ToList();
        var items = materialized.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<VehicleDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = materialized.Count
        };
    }

    public async Task<VehicleDto> GetByIdAsync(int id)
    {
        var vehicle = await _db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new NotFoundException($"No existe el vehiculo con id {id}.");
        return ToDto(vehicle, DateTime.UtcNow.Date);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleRequest request)
    {
        if (await _db.Vehicles.AnyAsync(v => v.PlateNumber == request.PlateNumber))
        {
            throw new BusinessRuleException($"Ya existe un vehiculo con placas {request.PlateNumber}.");
        }

        if (request.IntervalKm <= 0 || request.IntervalMonths <= 0)
        {
            throw new BusinessRuleException("El intervalo de mantenimiento debe ser mayor a cero.");
        }

        var vehicle = new Vehicle
        {
            PlateNumber = request.PlateNumber,
            Brand = request.Brand,
            Model = request.Model,
            Year = request.Year,
            Odometer = request.Odometer,
            IntervalKm = request.IntervalKm,
            IntervalMonths = request.IntervalMonths,
            Status = VehicleStatus.Active
        };

        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync();

        return ToDto(vehicle, DateTime.UtcNow.Date);
    }

    private static VehicleDto ToDto(Vehicle v, DateTime today) => new(
        v.Id, v.PlateNumber, v.Brand, v.Model, v.Year, v.Odometer, v.Status,
        v.IntervalKm, v.IntervalMonths, v.LastServiceOdometer, v.LastServiceDate,
        v.IsOverdue(today), v.RowVersion);
}
