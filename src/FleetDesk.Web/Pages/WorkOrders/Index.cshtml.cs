using FleetDesk.Web.Models;
using FleetDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FleetDesk.Web.Pages.WorkOrders;

public class IndexModel : PageModel
{
    private readonly FleetDeskApiClient _apiClient;

    public IndexModel(FleetDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public PagedResult<WorkOrderDto> WorkOrders { get; set; } = new();
    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true, Name = "page")]
    public int CurrentPage { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public WorkOrderStatus? Status { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            WorkOrders = await _apiClient.GetWorkOrdersAsync(CurrentPage, 10, Status);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
