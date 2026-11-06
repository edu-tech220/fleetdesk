namespace FleetDesk.Api.Dtos;

public record PartDto(int Id, string Sku, string Name, int StockQuantity, decimal UnitCost, Guid RowVersion);

public record CreatePartRequest(string Sku, string Name, int StockQuantity, decimal UnitCost);

public record RestockPartRequest(int Quantity, Guid RowVersion);
