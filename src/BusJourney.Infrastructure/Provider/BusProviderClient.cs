using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using BusJourney.Application.Abstractions;
using BusJourney.Application.Common;
using BusJourney.Application.Exceptions;
using BusJourney.Application.Models;
using BusJourney.Infrastructure.Provider.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusJourney.Infrastructure.Provider;

/// <summary>
/// HTTP implementation of <see cref="IBusProviderClient"/>. Builds the request envelopes, unwraps the
/// response envelope and maps wire DTOs to application models. Authentication and base address are set
/// on the injected <see cref="HttpClient"/> (see <see cref="DependencyInjection"/>).
/// </summary>
internal sealed class BusProviderClient(
    HttpClient httpClient,
    IOptions<ProviderOptions> options,
    TimeProvider timeProvider,
    ILogger<BusProviderClient> logger) : IBusProviderClient
{
    private const string SuccessStatus = "Success";
    private const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

    // Device type of a browser client, as used in the provider's sample requests.
    private const int BrowserDeviceType = 1;

    // Web defaults also accept numbers sent as strings; the provider documentation is inconsistent about numeric types.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ProviderOptions _options = options.Value;

    public async Task<DeviceSession> CreateSessionAsync(CancellationToken cancellationToken)
    {
        var request = new GetSessionRequest(
            BrowserDeviceType,
            new ConnectionDto(_options.ClientIpAddress, _options.ClientPort),
            new BrowserDto(_options.BrowserName, _options.BrowserVersion));

        var data = await PostAsync<GetSessionRequest, DeviceSessionDto>("client/getsession", request, cancellationToken)
            ?? throw MissingData("client/getsession");

        return new DeviceSession(data.SessionId, data.DeviceId);
    }

    public async Task<IReadOnlyList<Location>> GetLocationsAsync(
        DeviceSession session,
        string? keyword,
        CancellationToken cancellationToken)
    {
        var request = Envelope(session, string.IsNullOrWhiteSpace(keyword) ? null : keyword);
        var data = await PostAsync<ApiRequest<string>, List<LocationDto>>("location/getbuslocations", request, cancellationToken);

        return data?.Select(l => new Location(l.Id, l.Name)).ToList() ?? [];
    }

    public async Task<IReadOnlyList<Journey>> GetJourneysAsync(
        DeviceSession session,
        JourneyQuery query,
        CancellationToken cancellationToken)
    {
        var request = Envelope(session, new GetJourneysData(
            query.OriginId,
            query.DestinationId,
            query.DepartureDate.ToDateTime(TimeOnly.MinValue).ToString(DateTimeFormat, CultureInfo.InvariantCulture)));

        var data = await PostAsync<ApiRequest<GetJourneysData>, List<JourneyDto>>("journey/getbusjourneys", request, cancellationToken);

        return data?.Select(ToJourney).ToList() ?? [];
    }

    private ApiRequest<T> Envelope<T>(DeviceSession session, T? data) => new(
        data,
        new DeviceSessionDto(session.SessionId, session.DeviceId),
        timeProvider.GetTurkeyNow().ToString(DateTimeFormat, CultureInfo.InvariantCulture),
        _options.Language);

    private static Journey ToJourney(JourneyDto dto) => new(
        dto.Id,
        dto.PartnerName ?? string.Empty,
        dto.Journey.Origin ?? string.Empty,
        dto.Journey.Destination ?? string.Empty,
        dto.Journey.Departure,
        dto.Journey.Arrival,
        dto.Journey.InternetPrice,
        dto.Journey.Currency ?? string.Empty,
        dto.OriginLocation ?? string.Empty,
        dto.DestinationLocation ?? string.Empty);

    /// <summary>
    /// Posts a request and returns the <c>data</c> of the response envelope.
    /// Every failure is surfaced as a <see cref="ProviderException"/> so callers handle a single exception type.
    /// </summary>
    private async Task<TData?> PostAsync<TRequest, TData>(string path, TRequest body, CancellationToken cancellationToken)
    {
        ApiResponse<TData>? response;
        try
        {
            using var httpResponse = await httpClient.PostAsJsonAsync(path, body, JsonOptions, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new ProviderException(
                    ProviderException.TransportErrorStatus,
                    null,
                    $"Provider returned HTTP {(int)httpResponse.StatusCode} for '{path}'.");
            }

            response = await httpResponse.Content.ReadFromJsonAsync<ApiResponse<TData>>(JsonOptions, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout surfaces as a cancellation that the caller did not request.
            throw new ProviderException(ProviderException.TimeoutStatus, null, $"Provider call '{path}' timed out.", ex);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            throw new ProviderException(ProviderException.TransportErrorStatus, null, $"Provider call '{path}' failed.", ex);
        }

        if (response?.Status != SuccessStatus)
        {
            logger.LogWarning(
                "Provider call {Path} returned status {Status}: {Message}",
                path, response?.Status, response?.Message);

            throw new ProviderException(
                response?.Status ?? ProviderException.TransportErrorStatus,
                response?.UserMessage,
                $"Provider call '{path}' returned status '{response?.Status}': {response?.Message}");
        }

        return response.Data;
    }

    private static ProviderException MissingData(string path) =>
        new(ProviderException.TransportErrorStatus, null, $"Provider call '{path}' returned no data.");
}
