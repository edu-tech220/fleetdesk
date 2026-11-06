using FleetDesk.Api.Domain;
using FleetDesk.Api.Dtos;
using FleetDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FleetDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/parts")]
public class PartsController : ControllerBase
{
    private readonly IPartsService _partsService;

    public PartsController(IPartsService partsService)
    {
        _partsService = partsService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PartDto>>> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return Ok(await _partsService.GetPagedAsync(page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PartDto>> GetById(int id)
    {
        return Ok(await _partsService.GetByIdAsync(id));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PartDto>> Create(CreatePartRequest request)
    {
        var part = await _partsService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = part.Id }, part);
    }

    [HttpPost("{id:int}/restock")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<PartDto>> Restock(int id, RestockPartRequest request)
    {
        return Ok(await _partsService.RestockAsync(id, request));
    }
}
