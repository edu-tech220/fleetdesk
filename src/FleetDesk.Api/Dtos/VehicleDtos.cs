using FleetDesk.Api.Domain;

namespace FleetDesk.Api.Dtos;

public record VehicleDto(
    int Id,
    string PlateNumber,
    string Brand,
    string Model,
    int Year,
    int Odometer,
    VehicleStatus Status,
    int IntervalKm,
    int IntervalMonths,
    int? LastServiceOdometer,
    DateTime? LastServiceDate,
    bool IsOverdue,
    Guid RowVersion);

public record CreateVehicleRequest(
    string PlateNumber,
    string Brand,
    string Model,
    int Year,
    int Odometer,
    int IntervalKm,
    int IntervalMonths);
