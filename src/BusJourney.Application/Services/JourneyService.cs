using BusJourney.Application.Abstractions;
using BusJourney.Application.Models;

namespace BusJourney.Application.Services;

public sealed class JourneyService(IBusProviderClient client, ProviderSessionAccessor sessionAccessor)
{
    /// <summary>
    /// Returns the journeys of a (validated) query, ordered by departure time.
    /// The route names come from the caller; the provider cannot look a location up by id,
    /// so blank names fall back to the ones carried by the journeys.
    /// (History: names were once looked up here from the unfiltered location list when there were no journeys.
    /// That list is only ~20 popular towns, so destinations like Bodrum came back blank; hence the caller-supplied names.)
    /// </summary>
    public async Task<JourneySearchResult> SearchAsync(
        JourneyQuery query,
        string? originName,
        string? destinationName,
        CancellationToken cancellationToken)
    {
        var session = await sessionAccessor.GetAsync(cancellationToken);
        var journeys = await client.GetJourneysAsync(session, query, cancellationToken);
        var ordered = journeys.OrderBy(j => j.Departure).ToList();

        return new JourneySearchResult(
            Pick(originName, ordered.FirstOrDefault()?.OriginLocation),
            Pick(destinationName, ordered.FirstOrDefault()?.DestinationLocation),
            query.DepartureDate,
            ordered);
    }

    private static string Pick(string? given, string? fallback) =>
        !string.IsNullOrWhiteSpace(given) ? given.Trim() : fallback ?? string.Empty;
}
