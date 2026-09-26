using BusJourney.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace BusJourney.Web.Controllers;

/// <summary>
/// JSON endpoint behind the location autocomplete. The browser only talks to this backend;
/// the provider API is called server side with the visitor's own session.
/// </summary>
public sealed class LocationsController(LocationService locationService) : Controller
{
    [HttpGet("locations")]
    public async Task<IActionResult> Search(string? q, CancellationToken cancellationToken) =>
        Json(await locationService.SearchAsync(q, cancellationToken));
}
