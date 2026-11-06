using FleetDesk.Web.Models;
using FleetDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FleetDesk.Web.Pages.Vehicles;

public class DetailsModel : PageModel
{
    private readonly FleetDeskApiClient _apiClient;

    public DetailsModel(FleetDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public VehicleDto? Vehicle { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            Vehicle = await _apiClient.GetVehicleAsync(id);
            return Page();
        }
        catch (ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}
