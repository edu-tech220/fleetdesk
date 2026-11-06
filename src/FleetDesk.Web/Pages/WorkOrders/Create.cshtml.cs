using System.ComponentModel.DataAnnotations;
using FleetDesk.Web.Models;
using FleetDesk.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FleetDesk.Web.Pages.WorkOrders;

[Authorize(Roles = $"{Roles.Admin},{Roles.Mechanic}")]
public class CreateModel : PageModel
{
    private readonly FleetDeskApiClient _apiClient;

    public CreateModel(FleetDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> AvailableVehicles { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Selecciona un vehiculo.")]
        public int VehicleId { get; set; }

        [Required, StringLength(500)]
        public string Description { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(int? vehicleId)
    {
        await LoadVehiclesAsync();
        if (vehicleId is not null)
        {
            Input.VehicleId = vehicleId.Value;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadVehiclesAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var workOrder = await _apiClient.CreateWorkOrderAsync(new CreateWorkOrderRequest(Input.VehicleId, Input.Description));
            TempData["StatusMessage"] = $"Orden #{workOrder.Id} creada correctamente.";
            return RedirectToPage("/WorkOrders/Details", new { id = workOrder.Id });
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    private async Task LoadVehiclesAsync()
    {
        var vehicles = await _apiClient.GetVehiclesAsync(1, 100);
        AvailableVehicles = vehicles.Items
            .Where(v => v.Status == VehicleStatus.Active)
            .Select(v => new SelectListItem($"{v.PlateNumber} - {v.Brand} {v.Model}", v.Id.ToString()))
            .ToList();
    }
}
