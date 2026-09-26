using BusJourney.Application.Abstractions;
using BusJourney.Application.Common;
using BusJourney.Application.Models;

namespace BusJourney.Application.Services;

public sealed class LocationService(
    IBusProviderClient client,
    ProviderSessionAccessor sessionAccessor,
    TimeProvider timeProvider)
{
    /// <summary>Searches locations by keyword. An empty keyword returns every location.</summary>
    public async Task<IReadOnlyList<Location>> SearchAsync(string? keyword, CancellationToken cancellationToken)
    {
        var session = await sessionAccessor.GetAsync(cancellationToken);
        return await client.GetLocationsAsync(session, keyword?.Trim(), cancellationToken);
    }

    /// <summary>
    /// Builds the initial values of the search form. Requested values are kept when they are known;
    /// otherwise origin and destination fall back to the first two locations in the provider's default order,
    /// and the departure date falls back to tomorrow.
    /// </summary>
    public async Task<SearchDefaults> GetSearchDefaultsAsync(
        int? originId,
        int? destinationId,
        DateOnly? departureDate,
        CancellationToken cancellationToken)
    {
        var locations = await SearchAsync(null, cancellationToken);

        var origin = Find(locations, originId) ?? locations.ElementAtOrDefault(0);
        var destination = Find(locations, destinationId) ?? locations.ElementAtOrDefault(1);
        var date = departureDate ?? timeProvider.GetTurkeyToday().AddDays(1);

        return new SearchDefaults(locations, origin, destination, date);
    }

    private static Location? Find(IReadOnlyList<Location> locations, int? id) =>
        id is null ? null : locations.FirstOrDefault(l => l.Id == id);
}
