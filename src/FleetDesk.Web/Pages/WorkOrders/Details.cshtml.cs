using System.ComponentModel.DataAnnotations;
using FleetDesk.Web.Models;
using FleetDesk.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FleetDesk.Web.Pages.WorkOrders;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly FleetDeskApiClient _apiClient;

    public DetailsModel(FleetDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public WorkOrderDto? WorkOrder { get; set; }
    public List<SelectListItem> AvailableParts { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public AddPartInput NewPart { get; set; } = new();

    [BindProperty]
    public CompleteInput Complete { get; set; } = new();

    public class AddPartInput
    {
        [Required(ErrorMessage = "Selecciona una refaccion.")]
        public int PartId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public int Quantity { get; set; } = 1;
    }

    public class CompleteInput
    {
        [Range(0, int.MaxValue)]
        public int ClosingOdometer { get; set; }
    }

    public bool CanManage => WorkOrder is not null &&
        (User.IsInRole(Roles.Admin) || string.Equals(WorkOrder.MechanicUsername, User.Identity?.Name, StringComparison.OrdinalIgnoreCase));

    public async Task<IActionResult> OnGetAsync(int id)
    {
        try
        {
            WorkOrder = await _apiClient.GetWorkOrderAsync(id);
            Complete.ClosingOdometer = WorkOrder.OdometerAtOpen;
            await LoadPartsAsync();
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

    public async Task<IActionResult> OnPostStartAsync(int id)
    {
        try
        {
            await _apiClient.StartWorkOrderAsync(id);
            TempData["StatusMessage"] = "Orden iniciada.";
        }
        catch (ApiException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddPartAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Selecciona una refaccion y una cantidad valida.";
            return RedirectToPage(new { id });
        }

        try
        {
            var part = await _apiClient.GetPartAsync(NewPart.PartId);
            await _apiClient.AddPartToWorkOrderAsync(id, new AddPartRequest(NewPart.PartId, NewPart.Quantity, part.RowVersion));
            TempData["StatusMessage"] = "Refaccion agregada a la orden.";
        }
        catch (ApiException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        try
        {
            await _apiClient.CompleteWorkOrderAsync(id, new CompleteWorkOrderRequest(Complete.ClosingOdometer));
            TempData["StatusMessage"] = "Orden completada.";
        }
        catch (ApiException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        try
        {
            await _apiClient.CancelWorkOrderAsync(id);
            TempData["StatusMessage"] = "Orden cancelada.";
        }
        catch (ApiException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    private async Task LoadPartsAsync()
    {
        var parts = await _apiClient.GetPartsAsync(1, 100);
        AvailableParts = parts.Items
            .Select(p => new SelectListItem($"{p.Sku} - {p.Name} (stock: {p.StockQuantity})", p.Id.ToString()))
            .ToList();
    }
}
