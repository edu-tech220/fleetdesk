using FleetDesk.Web.Models;
using FleetDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FleetDesk.Web.Pages.Vehicles;

public class IndexModel : PageModel
{
    private readonly FleetDeskApiClient _apiClient;

    public IndexModel(FleetDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public PagedResult<VehicleDto> Vehicles { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true, Name = "page")]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public bool OverdueOnly { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            Vehicles = await _apiClient.GetVehiclesAsync(CurrentPage, 10, OverdueOnly);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
