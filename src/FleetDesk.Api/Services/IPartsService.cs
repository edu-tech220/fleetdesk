using FleetDesk.Api.Dtos;

namespace FleetDesk.Api.Services;

public interface IPartsService
{
    Task<PagedResult<PartDto>> GetPagedAsync(int page, int pageSize);
    Task<PartDto> GetByIdAsync(int id);
    Task<PartDto> CreateAsync(CreatePartRequest request);
    Task<PartDto> RestockAsync(int id, RestockPartRequest request);
}
