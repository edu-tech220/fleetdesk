namespace FleetDesk.Api.Domain;

public enum WorkOrderStatus
{
    Pending,
    InProgress,
    WaitingParts,
    Completed,
    Cancelled
}

public class WorkOrder
{
    public int Id { get; set; }

    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public string MechanicUsername { get; set; } = string.Empty;
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Pending;
    public string Description { get; set; } = string.Empty;

    public int OdometerAtOpen { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int? ClosingOdometer { get; set; }

    public Guid RowVersion { get; set; } = Guid.NewGuid();

    public List<WorkOrderPart> Parts { get; set; } = new();

    /// Numero maximo de ordenes abiertas (no finalizadas) que un mecanico puede tener a la vez.
    public const int OpenStatusCapacityPerMechanic = 3;

    public static readonly IReadOnlySet<WorkOrderStatus> OpenStatuses = new HashSet<WorkOrderStatus>
    {
        WorkOrderStatus.Pending,
        WorkOrderStatus.InProgress,
        WorkOrderStatus.WaitingParts
    };
}

public class WorkOrderPart
{
    public int Id { get; set; }

    public int WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = null!;

    public int PartId { get; set; }
    public Part Part { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitCostAtUse { get; set; }
}
