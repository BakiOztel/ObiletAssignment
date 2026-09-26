using System.Net;
using System.Text;
using BusJourney.Application.Abstractions;
using BusJourney.Application.Models;

namespace BusJourney.Tests;

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

internal sealed class InMemorySessionStore : IProviderSessionStore
{
    public DeviceSession? Session { get; set; }

    public DeviceSession? Get() => Session;

    public void Set(DeviceSession session) => Session = session;
}

internal sealed class FakeBusProviderClient : IBusProviderClient
{
    public List<Location> Locations { get; } = [];

    public List<Journey> Journeys { get; } = [];

    public int CreatedSessions { get; private set; }

    public Task<DeviceSession> CreateSessionAsync(CancellationToken cancellationToken)
    {
        CreatedSessions++;
        return Task.FromResult(new DeviceSession($"session-{CreatedSessions}", $"device-{CreatedSessions}"));
    }

    public Task<IReadOnlyList<Location>> GetLocationsAsync(DeviceSession session, string? keyword, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Location>>(Locations);

    public Task<IReadOnlyList<Journey>> GetJourneysAsync(DeviceSession session, JourneyQuery query, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Journey>>(Journeys);
}

/// <summary>Returns a canned JSON response and records every request (with its body) it receives.</summary>
internal sealed class StubHttpMessageHandler(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
{
    public List<(HttpRequestMessage Request, string Body)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
        };
    }
}

internal static class TestData
{
    // 2026-09-26 12:00 in Turkey (UTC+3).
    public static readonly DateTimeOffset Noon = new(2026, 9, 26, 9, 0, 0, TimeSpan.Zero);

    public static Journey Journey(long id, DateTime departure) => new(
        id, "Partner", "Origin Station", "Destination Station", departure, departure.AddHours(6), 100m, "TRY", "Origin", "Destination");
}
