using BusJourney.Application.Common;
using BusJourney.Application.Models;
using BusJourney.Application.Services;
using BusJourney.Application.Validation;
using BusJourney.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BusJourney.Web.Controllers;

public sealed class JourneyController(
    JourneyService journeyService,
    LocationService locationService,
    JourneyQueryValidator validator,
    TimeProvider timeProvider) : Controller
{
    [HttpGet("journey")]
    public async Task<IActionResult> Index([FromQuery] JourneySearchInput input, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var query = new JourneyQuery(input.OriginId!.Value, input.DestinationId!.Value, input.Date!.Value);

            foreach (var error in validator.Validate(query))
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage!);
            }

            if (ModelState.IsValid)
            {
                return View(await journeyService.SearchAsync(query, cancellationToken));
            }
        }

        // Invalid search (only reachable by bypassing client-side validation): show the form again with the errors.
        var defaults = await locationService.GetSearchDefaultsAsync(
            input.OriginId, input.DestinationId, input.Date, cancellationToken);

        return View("~/Views/Home/Index.cshtml", SearchViewModel.Create(defaults, timeProvider.GetTurkeyToday()));
    }
}
