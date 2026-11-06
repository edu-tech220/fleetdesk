using FleetDesk.Api.Domain;

namespace FleetDesk.Api.Dtos;

public record WorkOrderPartDto(int PartId, string PartName, int Quantity, decimal UnitCostAtUse);

public record WorkOrderDto(
    int Id,
    int VehicleId,
    string PlateNumber,
    string MechanicUsername,
    WorkOrderStatus Status,
    string Description,
    int OdometerAtOpen,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    int? ClosingOdometer,
    Guid RowVersion,
    IReadOnlyList<WorkOrderPartDto> Parts);

public record CreateWorkOrderRequest(int VehicleId, string Description);

public record AddPartRequest(int PartId, int Quantity, Guid PartRowVersion);

public record CompleteWorkOrderRequest(int ClosingOdometer);
