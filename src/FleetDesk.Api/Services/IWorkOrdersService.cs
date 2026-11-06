using FleetDesk.Api.Domain;
using FleetDesk.Api.Dtos;

namespace FleetDesk.Api.Services;

public interface IWorkOrdersService
{
    Task<PagedResult<WorkOrderDto>> GetPagedAsync(int page, int pageSize, WorkOrderStatus? status);
    Task<WorkOrderDto> GetByIdAsync(int id);
    Task<WorkOrderDto> CreateAsync(CreateWorkOrderRequest request, string mechanicUsername);
    Task<WorkOrderDto> StartAsync(int id, string actingUsername, bool isAdmin);
    Task<WorkOrderDto> AddPartAsync(int id, AddPartRequest request, string actingUsername, bool isAdmin);
    Task<WorkOrderDto> CompleteAsync(int id, CompleteWorkOrderRequest request, string actingUsername, bool isAdmin);
    Task<WorkOrderDto> CancelAsync(int id, string actingUsername, bool isAdmin);
}
