using FleetDesk.Web.Models;
using FleetDesk.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FleetDesk.Web.Pages.Reports;

[Authorize(Roles = $"{Roles.Admin},{Roles.FleetManager}")]
public class OverdueModel : PageModel
{
    private readonly FleetDeskApiClient _apiClient;

    public OverdueModel(FleetDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public PagedResult<VehicleDto> Vehicles { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Vehicles = await _apiClient.GetOverdueVehiclesAsync(1, 50);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
