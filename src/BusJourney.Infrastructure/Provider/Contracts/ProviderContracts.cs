using System.Text.Json.Serialization;

namespace BusJourney.Infrastructure.Provider.Contracts;

// Wire format of the provider API. Only the fields used by the application are declared; the rest are ignored.

/// <summary>Envelope posted to every endpoint except session creation.</summary>
internal sealed record ApiRequest<T>(
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("device-session")] DeviceSessionDto DeviceSession,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("language")] string Language);

/// <summary>Envelope returned by every endpoint.</summary>
internal sealed record ApiResponse<T>(
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("user-message")] string? UserMessage);

internal sealed record DeviceSessionDto(
    [property: JsonPropertyName("session-id")] string SessionId,
    [property: JsonPropertyName("device-id")] string DeviceId);

// The documented "type 7 + application" body is rejected by the live API; the body of the sample
// Postman collection (browser client) is the one that works.
internal sealed record GetSessionRequest(
    [property: JsonPropertyName("type")] int Type,
    [property: JsonPropertyName("connection")] ConnectionDto Connection,
    [property: JsonPropertyName("browser")] BrowserDto Browser);

internal sealed record ConnectionDto(
    [property: JsonPropertyName("ip-address")] string IpAddress,
    [property: JsonPropertyName("port")] string Port);

internal sealed record BrowserDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version);

internal sealed record LocationDto(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name);

internal sealed record GetJourneysData(
    [property: JsonPropertyName("origin-id")] int OriginId,
    [property: JsonPropertyName("destination-id")] int DestinationId,
    [property: JsonPropertyName("departure-date")] string DepartureDate);

internal sealed record JourneyDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("partner-name")] string? PartnerName,
    [property: JsonPropertyName("journey")] JourneyDetailDto Journey,
    [property: JsonPropertyName("origin-location")] string? OriginLocation,
    [property: JsonPropertyName("destination-location")] string? DestinationLocation);

internal sealed record JourneyDetailDto(
    [property: JsonPropertyName("origin")] string? Origin,
    [property: JsonPropertyName("destination")] string? Destination,
    [property: JsonPropertyName("departure")] DateTime Departure,
    [property: JsonPropertyName("arrival")] DateTime Arrival,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("internet-price")] decimal InternetPrice);
