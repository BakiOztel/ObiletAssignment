namespace BusJourney.Application.Models;

/// <summary>
/// Session credentials issued by the provider API. Every call after session creation must carry this pair.
/// </summary>
public sealed record DeviceSession(string SessionId, string DeviceId);

/// <summary>A bus location (town) that can be used as origin or destination.</summary>
public sealed record Location(int Id, string Name);

/// <summary>A journey search request made by the end user.</summary>
public sealed record JourneyQuery(int OriginId, int DestinationId, DateOnly DepartureDate);

/// <summary>A single bus journey, reduced to the fields the application displays.</summary>
public sealed record Journey(
    long Id,
    string PartnerName,
    string OriginStation,
    string DestinationStation,
    DateTime Departure,
    DateTime Arrival,
    decimal Price,
    string Currency,
    string OriginLocation,
    string DestinationLocation);

/// <summary>Journeys for a query, plus the route names shown in the page header.</summary>
public sealed record JourneySearchResult(
    string OriginName,
    string DestinationName,
    DateOnly DepartureDate,
    IReadOnlyList<Journey> Journeys);

/// <summary>Initial values of the search form.</summary>
public sealed record SearchDefaults(
    IReadOnlyList<Location> Locations,
    Location? Origin,
    Location? Destination,
    DateOnly DepartureDate);
