using FleetDesk.Web.Models;
using FleetDesk.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FleetDesk.Web.Pages;

public class IndexModel : PageModel
{
    private readonly FleetDeskApiClient _apiClient;

    public IndexModel(FleetDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public int TotalVehicles { get; set; }
    public int OverdueVehicles { get; set; }
    public int TotalParts { get; set; }
    public int PendingOrders { get; set; }
    public int InProgressOrders { get; set; }
    public int WaitingPartsOrders { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var vehicles = await _apiClient.GetVehiclesAsync(1, 1);
            TotalVehicles = vehicles.TotalCount;

            var overdue = await _apiClient.GetOverdueVehiclesAsync(1, 1);
            OverdueVehicles = overdue.TotalCount;

            var parts = await _apiClient.GetPartsAsync(1, 1);
            TotalParts = parts.TotalCount;

            PendingOrders = (await _apiClient.GetWorkOrdersAsync(1, 1, WorkOrderStatus.Pending)).TotalCount;
            InProgressOrders = (await _apiClient.GetWorkOrdersAsync(1, 1, WorkOrderStatus.InProgress)).TotalCount;
            WaitingPartsOrders = (await _apiClient.GetWorkOrdersAsync(1, 1, WorkOrderStatus.WaitingParts)).TotalCount;
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
