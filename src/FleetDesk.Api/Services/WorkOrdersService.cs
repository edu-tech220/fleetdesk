using FleetDesk.Api.Data;
using FleetDesk.Api.Domain;
using FleetDesk.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FleetDesk.Api.Services;

public class WorkOrdersService : IWorkOrdersService
{
    private readonly FleetDeskDbContext _db;

    public WorkOrdersService(FleetDeskDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<WorkOrderDto>> GetPagedAsync(int page, int pageSize, WorkOrderStatus? status)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.WorkOrders
            .Include(w => w.Vehicle)
            .Include(w => w.Parts).ThenInclude(p => p.Part)
            .AsNoTracking()
            .OrderByDescending(w => w.OpenedAt)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(w => w.Status == status);
        }

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<WorkOrderDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<WorkOrderDto> GetByIdAsync(int id)
    {
        var workOrder = await FindWithDetailsAsync(id, tracking: false)
            ?? throw new NotFoundException($"No existe la orden de trabajo con id {id}.");
        return ToDto(workOrder);
    }

    public async Task<WorkOrderDto> CreateAsync(CreateWorkOrderRequest request, string mechanicUsername)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == request.VehicleId)
            ?? throw new NotFoundException($"No existe el vehiculo con id {request.VehicleId}.");

        if (vehicle.Status != VehicleStatus.Active)
        {
            throw new BusinessRuleException(
                $"El vehiculo {vehicle.PlateNumber} no esta disponible para una nueva orden (estado actual: {vehicle.Status}).");
        }

        var openStatuses = WorkOrder.OpenStatuses;
        var openCount = await _db.WorkOrders.CountAsync(w =>
            w.MechanicUsername == mechanicUsername && openStatuses.Contains(w.Status));

        if (openCount >= WorkOrder.OpenStatusCapacityPerMechanic)
        {
            throw new BusinessRuleException(
                $"El mecanico {mechanicUsername} ya tiene {openCount} ordenes abiertas (limite {WorkOrder.OpenStatusCapacityPerMechanic}).");
        }

        var workOrder = new WorkOrder
        {
            VehicleId = vehicle.Id,
            MechanicUsername = mechanicUsername,
            Status = WorkOrderStatus.Pending,
            Description = request.Description,
            OdometerAtOpen = vehicle.Odometer,
            OpenedAt = DateTime.UtcNow
        };

        vehicle.Status = VehicleStatus.InShop;
        _db.WorkOrders.Add(workOrder);
        await _db.SaveChangesAsync();

        return ToDto((await FindWithDetailsAsync(workOrder.Id, tracking: false))!);
    }

    public async Task<WorkOrderDto> StartAsync(int id, string actingUsername, bool isAdmin)
    {
        var workOrder = await FindWithDetailsAsync(id, tracking: true)
            ?? throw new NotFoundException($"No existe la orden de trabajo con id {id}.");

        EnsureOwnership(workOrder, actingUsername, isAdmin);

        if (workOrder.Status != WorkOrderStatus.Pending)
        {
            throw new BusinessRuleException(
                $"Solo una orden Pendiente puede iniciarse (estado actual: {workOrder.Status}).");
        }

        workOrder.Status = WorkOrderStatus.InProgress;
        await _db.SaveChangesAsync();

        return ToDto(workOrder);
    }

    public async Task<WorkOrderDto> AddPartAsync(int id, AddPartRequest request, string actingUsername, bool isAdmin)
    {
        if (request.Quantity <= 0)
        {
            throw new BusinessRuleException("La cantidad de refaccion debe ser mayor a cero.");
        }

        var workOrder = await FindWithDetailsAsync(id, tracking: true)
            ?? throw new NotFoundException($"No existe la orden de trabajo con id {id}.");

        EnsureOwnership(workOrder, actingUsername, isAdmin);

        if (workOrder.Status is not (WorkOrderStatus.InProgress or WorkOrderStatus.WaitingParts))
        {
            throw new BusinessRuleException(
                $"Solo se pueden agregar refacciones a una orden en progreso o esperando refacciones (estado actual: {workOrder.Status}).");
        }

        var part = await _db.Parts.FirstOrDefaultAsync(p => p.Id == request.PartId)
            ?? throw new NotFoundException($"No existe la refaccion con id {request.PartId}.");

        _db.Entry(part).Property(p => p.RowVersion).OriginalValue = request.PartRowVersion;

        if (part.StockQuantity < request.Quantity)
        {
            workOrder.Status = WorkOrderStatus.WaitingParts;
            await _db.SaveChangesAsync();
            throw new BusinessRuleException(
                $"Stock insuficiente de '{part.Name}' (disponible: {part.StockQuantity}, solicitado: {request.Quantity}). " +
                "La orden quedo en espera de refacciones.");
        }

        part.StockQuantity -= request.Quantity;
        part.RowVersion = Guid.NewGuid();

        workOrder.Parts.Add(new WorkOrderPart
        {
            WorkOrderId = workOrder.Id,
            PartId = part.Id,
            Quantity = request.Quantity,
            UnitCostAtUse = part.UnitCost
        });

        if (workOrder.Status == WorkOrderStatus.WaitingParts)
        {
            workOrder.Status = WorkOrderStatus.InProgress;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "La refaccion fue modificada por otro usuario. Vuelve a cargarla e intenta de nuevo.");
        }

        return ToDto((await FindWithDetailsAsync(id, tracking: false))!);
    }

    public async Task<WorkOrderDto> CompleteAsync(int id, CompleteWorkOrderRequest request, string actingUsername, bool isAdmin)
    {
        var workOrder = await FindWithDetailsAsync(id, tracking: true)
            ?? throw new NotFoundException($"No existe la orden de trabajo con id {id}.");

        EnsureOwnership(workOrder, actingUsername, isAdmin);

        if (workOrder.Status != WorkOrderStatus.InProgress)
        {
            throw new BusinessRuleException(
                $"Solo una orden En Progreso puede completarse (estado actual: {workOrder.Status}).");
        }

        if (request.ClosingOdometer < workOrder.OdometerAtOpen)
        {
            throw new BusinessRuleException(
                $"El odometro de cierre ({request.ClosingOdometer}) no puede ser menor al de apertura ({workOrder.OdometerAtOpen}).");
        }

        var now = DateTime.UtcNow;
        workOrder.Status = WorkOrderStatus.Completed;
        workOrder.ClosedAt = now;
        workOrder.ClosingOdometer = request.ClosingOdometer;

        workOrder.Vehicle.Odometer = Math.Max(workOrder.Vehicle.Odometer, request.ClosingOdometer);
        workOrder.Vehicle.LastServiceOdometer = request.ClosingOdometer;
        workOrder.Vehicle.LastServiceDate = now.Date;
        workOrder.Vehicle.Status = VehicleStatus.Active;

        await _db.SaveChangesAsync();

        return ToDto(workOrder);
    }

    public async Task<WorkOrderDto> CancelAsync(int id, string actingUsername, bool isAdmin)
    {
        var workOrder = await FindWithDetailsAsync(id, tracking: true)
            ?? throw new NotFoundException($"No existe la orden de trabajo con id {id}.");

        EnsureOwnership(workOrder, actingUsername, isAdmin);

        if (workOrder.Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled)
        {
            throw new BusinessRuleException($"La orden ya esta finalizada (estado actual: {workOrder.Status}).");
        }

        workOrder.Status = WorkOrderStatus.Cancelled;
        workOrder.ClosedAt = DateTime.UtcNow;
        workOrder.Vehicle.Status = VehicleStatus.Active;

        await _db.SaveChangesAsync();

        return ToDto(workOrder);
    }

    private static void EnsureOwnership(WorkOrder workOrder, string actingUsername, bool isAdmin)
    {
        if (!isAdmin && !string.Equals(workOrder.MechanicUsername, actingUsername, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("No tienes permiso para modificar una orden asignada a otro mecanico.");
        }
    }

    private Task<WorkOrder?> FindWithDetailsAsync(int id, bool tracking)
    {
        var query = _db.WorkOrders
            .Include(w => w.Vehicle)
            .Include(w => w.Parts).ThenInclude(p => p.Part)
            .AsQueryable();

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(w => w.Id == id);
    }

    private static WorkOrderDto ToDto(WorkOrder w) => new(
        w.Id,
        w.VehicleId,
        w.Vehicle.PlateNumber,
        w.MechanicUsername,
        w.Status,
        w.Description,
        w.OdometerAtOpen,
        w.OpenedAt,
        w.ClosedAt,
        w.ClosingOdometer,
        w.RowVersion,
        w.Parts.Select(p => new WorkOrderPartDto(p.PartId, p.Part.Name, p.Quantity, p.UnitCostAtUse)).ToList());
}
