namespace FleetDesk.Api.Domain;

public class Part
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal UnitCost { get; set; }

    public Guid RowVersion { get; set; } = Guid.NewGuid();
}
