using System.ComponentModel.DataAnnotations;
using FleetDesk.Web.Models;
using FleetDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FleetDesk.Web.Pages.Parts;

public class IndexModel : PageModel
{
    private readonly FleetDeskApiClient _apiClient;

    public IndexModel(FleetDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public PagedResult<PartDto> Parts { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true, Name = "page")]
    public int CurrentPage { get; set; } = 1;

    [BindProperty]
    public CreateInput NewPart { get; set; } = new();

    [BindProperty]
    public RestockInput Restock { get; set; } = new();

    public class CreateInput
    {
        [Required, StringLength(40)]
        public string Sku { get; set; } = string.Empty;

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitCost { get; set; }
    }

    public class RestockInput
    {
        public int PartId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
        public int Quantity { get; set; }

        public Guid RowVersion { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            await _apiClient.CreatePartAsync(new CreatePartRequest(NewPart.Sku, NewPart.Name, NewPart.StockQuantity, NewPart.UnitCost));
            TempData["StatusMessage"] = $"Refaccion {NewPart.Sku} creada correctamente.";
        }
        catch (ApiException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { page = CurrentPage });
    }

    public async Task<IActionResult> OnPostRestockAsync()
    {
        try
        {
            await _apiClient.RestockPartAsync(Restock.PartId, new RestockPartRequest(Restock.Quantity, Restock.RowVersion));
            TempData["StatusMessage"] = "Inventario actualizado correctamente.";
        }
        catch (ApiException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage(new { page = CurrentPage });
    }

    private async Task LoadAsync()
    {
        try
        {
            Parts = await _apiClient.GetPartsAsync(CurrentPage, 10);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
