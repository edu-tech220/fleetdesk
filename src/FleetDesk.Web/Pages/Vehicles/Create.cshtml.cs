using System.ComponentModel.DataAnnotations;
using FleetDesk.Web.Models;
using FleetDesk.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FleetDesk.Web.Pages.Vehicles;

[Authorize(Roles = Roles.Admin)]
public class CreateModel : PageModel
{
    private readonly FleetDeskApiClient _apiClient;

    public CreateModel(FleetDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required, StringLength(16)]
        public string PlateNumber { get; set; } = string.Empty;

        [Required, StringLength(60)]
        public string Brand { get; set; } = string.Empty;

        [Required, StringLength(60)]
        public string Model { get; set; } = string.Empty;

        [Range(1980, 2100)]
        public int Year { get; set; } = DateTime.UtcNow.Year;

        [Range(0, int.MaxValue)]
        public int Odometer { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "El intervalo en kilometros debe ser mayor a cero.")]
        public int IntervalKm { get; set; } = 10000;

        [Range(1, 60, ErrorMessage = "El intervalo en meses debe ser mayor a cero.")]
        public int IntervalMonths { get; set; } = 6;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var vehicle = await _apiClient.CreateVehicleAsync(new CreateVehicleRequest(
                Input.PlateNumber, Input.Brand, Input.Model, Input.Year, Input.Odometer, Input.IntervalKm, Input.IntervalMonths));

            TempData["StatusMessage"] = $"Vehiculo {vehicle.PlateNumber} creado correctamente.";
            return RedirectToPage("/Vehicles/Details", new { id = vehicle.Id });
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}
