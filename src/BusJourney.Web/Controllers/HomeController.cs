using System.Diagnostics;
using BusJourney.Application.Common;
using BusJourney.Application.Services;
using BusJourney.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BusJourney.Web.Controllers;

public sealed class HomeController(LocationService locationService, TimeProvider timeProvider) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var defaults = await locationService.GetSearchDefaultsAsync(null, null, null, cancellationToken);
        return View(SearchViewModel.Create(defaults, timeProvider.GetTurkeyToday()));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
