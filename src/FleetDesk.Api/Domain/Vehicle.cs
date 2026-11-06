namespace FleetDesk.Api.Domain;

public enum VehicleStatus
{
    Active,
    InShop,
    Retired
}

public class Vehicle
{
    public int Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Odometer { get; set; }
    public VehicleStatus Status { get; set; } = VehicleStatus.Active;

    /// Reglas del plan de mantenimiento preventivo del vehiculo.
    public int IntervalKm { get; set; }
    public int IntervalMonths { get; set; }

    public int? LastServiceOdometer { get; set; }
    public DateTime? LastServiceDate { get; set; }

    public Guid RowVersion { get; set; } = Guid.NewGuid();

    public List<WorkOrder> WorkOrders { get; set; } = new();

    public bool IsOverdue(DateTime today)
    {
        var odometerDue = LastServiceOdometer is null || Odometer - LastServiceOdometer.Value >= IntervalKm;
        var dateDue = LastServiceDate is null || today >= LastServiceDate.Value.AddMonths(IntervalMonths);
        return odometerDue || dateDue;
    }
}
