using BusJourney.Application.Models;
using BusJourney.Application.Services;

namespace BusJourney.Tests;

public class ProviderSessionAccessorTests
{
    [Fact]
    public async Task Creates_a_session_once_and_reuses_it()
    {
        var client = new FakeBusProviderClient();
        var store = new InMemorySessionStore();
        var accessor = new ProviderSessionAccessor(client, store);

        var first = await accessor.GetAsync(CancellationToken.None);
        var second = await accessor.GetAsync(CancellationToken.None);

        Assert.Equal(1, client.CreatedSessions);
        Assert.Same(first, second);
        Assert.Same(first, store.Session);
    }

    [Fact]
    public async Task Different_users_get_different_sessions()
    {
        var client = new FakeBusProviderClient();

        var userA = await new ProviderSessionAccessor(client, new InMemorySessionStore()).GetAsync(CancellationToken.None);
        var userB = await new ProviderSessionAccessor(client, new InMemorySessionStore()).GetAsync(CancellationToken.None);

        Assert.NotEqual(userA.SessionId, userB.SessionId);
    }
}

public class LocationServiceTests
{
    private readonly FakeBusProviderClient _client = new()
    {
        Locations = { new Location(349, "İstanbul Avrupa"), new Location(356, "Ankara"), new Location(357, "İzmir") },
    };

    private LocationService CreateService() =>
        new(_client, new ProviderSessionAccessor(_client, new InMemorySessionStore()), new FixedTimeProvider(TestData.Noon));

    [Fact]
    public async Task Defaults_follow_provider_order_and_tomorrow()
    {
        var defaults = await CreateService().GetSearchDefaultsAsync(null, null, null, CancellationToken.None);

        Assert.Equal(349, defaults.Origin?.Id);
        Assert.Equal(356, defaults.Destination?.Id);
        Assert.Equal(new DateOnly(2026, 9, 27), defaults.DepartureDate);
    }

    [Fact]
    public async Task Requested_values_are_kept_when_known()
    {
        var defaults = await CreateService().GetSearchDefaultsAsync(357, 349, new DateOnly(2026, 10, 1), CancellationToken.None);

        Assert.Equal("İzmir", defaults.Origin?.Name);
        Assert.Equal("İstanbul Avrupa", defaults.Destination?.Name);
        Assert.Equal(new DateOnly(2026, 10, 1), defaults.DepartureDate);
    }

    [Fact]
    public async Task Unknown_ids_fall_back_to_defaults()
    {
        var defaults = await CreateService().GetSearchDefaultsAsync(-1, -2, null, CancellationToken.None);

        Assert.Equal(349, defaults.Origin?.Id);
        Assert.Equal(356, defaults.Destination?.Id);
    }
}

public class JourneyServiceTests
{
    private static readonly JourneyQuery Query = new(349, 356, new DateOnly(2026, 9, 27));

    [Fact]
    public async Task Journeys_are_sorted_by_departure_time()
    {
        var client = new FakeBusProviderClient
        {
            Journeys =
            {
                TestData.Journey(1, new DateTime(2026, 9, 27, 23, 30, 0)),
                TestData.Journey(2, new DateTime(2026, 9, 27, 8, 0, 0)),
                TestData.Journey(3, new DateTime(2026, 9, 27, 13, 15, 0)),
            },
        };

        var result = await CreateService(client).SearchAsync(Query, null, null, CancellationToken.None);

        Assert.Equal([2L, 3L, 1L], result.Journeys.Select(j => j.Id));
        Assert.Equal("Origin", result.OriginName);
        Assert.Equal("Destination", result.DestinationName);
    }

    [Fact]
    public async Task Empty_result_keeps_the_given_route_names()
    {
        var result = await CreateService(new FakeBusProviderClient())
            .SearchAsync(Query, "İstanbul Avrupa", "Bodrum", CancellationToken.None);

        Assert.Empty(result.Journeys);
        Assert.Equal("İstanbul Avrupa", result.OriginName);
        Assert.Equal("Bodrum", result.DestinationName);
    }

    private static JourneyService CreateService(FakeBusProviderClient client) =>
        new(client, new ProviderSessionAccessor(client, new InMemorySessionStore()));
}
