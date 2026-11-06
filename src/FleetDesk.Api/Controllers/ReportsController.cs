using FleetDesk.Api.Dtos;
using FleetDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IVehiclesService _vehiclesService;

    public ReportsController(IVehiclesService vehiclesService)
    {
        _vehiclesService = vehiclesService;
    }

    [HttpGet("overdue-vehicles")]
    public async Task<ActionResult<PagedResult<VehicleDto>>> OverdueVehicles(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await _vehiclesService.GetPagedAsync(page, pageSize, overdueOnly: true));
    }
}
