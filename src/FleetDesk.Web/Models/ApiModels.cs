namespace FleetDesk.Web.Models;

public enum VehicleStatus
{
    Active,
    InShop,
    Retired
}

public enum WorkOrderStatus
{
    Pending,
    InProgress,
    WaitingParts,
    Completed,
    Cancelled
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, string Username, string Role, DateTime ExpiresAtUtc);

public class VehicleDto
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Odometer { get; set; }
    public VehicleStatus Status { get; set; }
    public int IntervalKm { get; set; }
    public int IntervalMonths { get; set; }
    public int? LastServiceOdometer { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public bool IsOverdue { get; set; }
    public Guid RowVersion { get; set; }
}

public record CreateVehicleRequest(
    string PlateNumber, string Brand, string Model, int Year, int Odometer, int IntervalKm, int IntervalMonths);

public class PartDto
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public Guid RowVersion { get; set; }
}

public record CreatePartRequest(string Sku, string Name, int StockQuantity, decimal UnitCost);

public record RestockPartRequest(int Quantity, Guid RowVersion);

public class WorkOrderPartDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCostAtUse { get; set; }
}

public class WorkOrderDto
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string MechanicUsername { get; set; } = string.Empty;
    public WorkOrderStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public int OdometerAtOpen { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? ClosingOdometer { get; set; }
    public Guid RowVersion { get; set; }
    public List<WorkOrderPartDto> Parts { get; set; } = new();
}

public record CreateWorkOrderRequest(int VehicleId, string Description);

public record AddPartRequest(int PartId, int Quantity, Guid PartRowVersion);

public record CompleteWorkOrderRequest(int ClosingOdometer);
