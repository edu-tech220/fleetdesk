using FleetDesk.Api.Domain;
using FleetDesk.Api.Dtos;
using FleetDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly IVehiclesService _vehiclesService;

    public VehiclesController(IVehiclesService vehiclesService)
    {
        _vehiclesService = vehiclesService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<VehicleDto>>> GetPaged(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool overdueOnly = false)
    {
        return Ok(await _vehiclesService.GetPagedAsync(page, pageSize, overdueOnly));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VehicleDto>> GetById(int id)
    {
        return Ok(await _vehiclesService.GetByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<VehicleDto>> Create(CreateVehicleRequest request)
    {
        var vehicle = await _vehiclesService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = vehicle.Id }, vehicle);
    }
}
