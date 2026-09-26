using BusJourney.Application.Abstractions;
using BusJourney.Application.Models;

namespace BusJourney.Application.Services;

public sealed class JourneyService(IBusProviderClient client, ProviderSessionAccessor sessionAccessor)
{
    /// <summary>
    /// Returns the journeys of a (validated) query, ordered by departure time.
    /// </summary>
    public async Task<JourneySearchResult> SearchAsync(JourneyQuery query, CancellationToken cancellationToken)
    {
        var session = await sessionAccessor.GetAsync(cancellationToken);
        var journeys = await client.GetJourneysAsync(session, query, cancellationToken);
        var ordered = journeys.OrderBy(j => j.Departure).ToList();

        var (originName, destinationName) = ordered.Count > 0
            ? (ordered[0].OriginLocation, ordered[0].DestinationLocation)
            : await ResolveRouteNamesAsync(session, query, cancellationToken);

        return new JourneySearchResult(originName, destinationName, query.DepartureDate, ordered);
    }

    // Journeys carry the route names; with no journeys we look them up so the header can still show the route.
    private async Task<(string Origin, string Destination)> ResolveRouteNamesAsync(
        DeviceSession session,
        JourneyQuery query,
        CancellationToken cancellationToken)
    {
        var locations = await client.GetLocationsAsync(session, null, cancellationToken);
        string NameOf(int id) => locations.FirstOrDefault(l => l.Id == id)?.Name ?? string.Empty;
        return (NameOf(query.OriginId), NameOf(query.DestinationId));
    }
}
