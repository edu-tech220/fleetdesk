using FleetDesk.Api.Domain;
using FleetDesk.Api.Dtos;
using FleetDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/workorders")]
public class WorkOrdersController : ControllerBase
{
    private readonly IWorkOrdersService _workOrdersService;

    public WorkOrdersController(IWorkOrdersService workOrdersService)
    {
        _workOrdersService = workOrdersService;
    }

    private string ActingUsername => User.Identity?.Name
        ?? throw new InvalidOperationException("El token no contiene un nombre de usuario.");

    private bool IsAdmin => User.IsInRole(Roles.Admin);

    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkOrderDto>>> GetPaged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] WorkOrderStatus? status = null)
    {
        return Ok(await _workOrdersService.GetPagedAsync(page, pageSize, status));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkOrderDto>> GetById(int id)
    {
        return Ok(await _workOrdersService.GetByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Mechanic}")]
    public async Task<ActionResult<WorkOrderDto>> Create(CreateWorkOrderRequest request)
    {
        var workOrder = await _workOrdersService.CreateAsync(request, ActingUsername);
        return CreatedAtAction(nameof(GetById), new { id = workOrder.Id }, workOrder);
    }

    [HttpPost("{id:int}/start")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Mechanic}")]
    public async Task<ActionResult<WorkOrderDto>> Start(int id)
    {
        return Ok(await _workOrdersService.StartAsync(id, ActingUsername, IsAdmin));
    }

    [HttpPost("{id:int}/parts")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Mechanic}")]
    public async Task<ActionResult<WorkOrderDto>> AddPart(int id, AddPartRequest request)
    {
        return Ok(await _workOrdersService.AddPartAsync(id, request, ActingUsername, IsAdmin));
    }

    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Mechanic}")]
    public async Task<ActionResult<WorkOrderDto>> Complete(int id, CompleteWorkOrderRequest request)
    {
        return Ok(await _workOrdersService.CompleteAsync(id, request, ActingUsername, IsAdmin));
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Mechanic}")]
    public async Task<ActionResult<WorkOrderDto>> Cancel(int id)
    {
        return Ok(await _workOrdersService.CancelAsync(id, ActingUsername, IsAdmin));
    }
}
