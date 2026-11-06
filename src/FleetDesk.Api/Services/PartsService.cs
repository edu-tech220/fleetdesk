using FleetDesk.Api.Data;
using FleetDesk.Api.Domain;
using FleetDesk.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FleetDesk.Api.Services;

public class PartsService : IPartsService
{
    private readonly FleetDeskDbContext _db;

    public PartsService(FleetDeskDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<PartDto>> GetPagedAsync(int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Parts.AsNoTracking().OrderBy(p => p.Name);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<PartDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PartDto> GetByIdAsync(int id)
    {
        var part = await _db.Parts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException($"No existe la refaccion con id {id}.");
        return ToDto(part);
    }

    public async Task<PartDto> CreateAsync(CreatePartRequest request)
    {
        if (await _db.Parts.AnyAsync(p => p.Sku == request.Sku))
        {
            throw new BusinessRuleException($"Ya existe una refaccion con SKU {request.Sku}.");
        }

        if (request.StockQuantity < 0 || request.UnitCost < 0)
        {
            throw new BusinessRuleException("La cantidad y el costo deben ser mayores o iguales a cero.");
        }

        var part = new Part
        {
            Sku = request.Sku,
            Name = request.Name,
            StockQuantity = request.StockQuantity,
            UnitCost = request.UnitCost
        };

        _db.Parts.Add(part);
        await _db.SaveChangesAsync();

        return ToDto(part);
    }

    public async Task<PartDto> RestockAsync(int id, RestockPartRequest request)
    {
        if (request.Quantity <= 0)
        {
            throw new BusinessRuleException("La cantidad a reabastecer debe ser mayor a cero.");
        }

        var part = await _db.Parts.FindAsync(id)
            ?? throw new NotFoundException($"No existe la refaccion con id {id}.");

        _db.Entry(part).Property(p => p.RowVersion).OriginalValue = request.RowVersion;

        part.StockQuantity += request.Quantity;
        part.RowVersion = Guid.NewGuid();

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "La refaccion fue modificada por otro usuario. Vuelve a cargarla e intenta de nuevo.");
        }

        return ToDto(part);
    }

    private static PartDto ToDto(Part p) => new(p.Id, p.Sku, p.Name, p.StockQuantity, p.UnitCost, p.RowVersion);
}
