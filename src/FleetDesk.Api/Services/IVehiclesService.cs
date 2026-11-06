using FleetDesk.Api.Dtos;

namespace FleetDesk.Api.Services;

public interface IVehiclesService
{
    Task<PagedResult<VehicleDto>> GetPagedAsync(int page, int pageSize, bool overdueOnly);
    Task<VehicleDto> GetByIdAsync(int id);
    Task<VehicleDto> CreateAsync(CreateVehicleRequest request);
}
