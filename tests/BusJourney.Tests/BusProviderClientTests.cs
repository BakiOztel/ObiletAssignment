using System.Net;
using System.Text.Json;
using BusJourney.Application.Abstractions;
using BusJourney.Application.Exceptions;
using BusJourney.Application.Models;
using BusJourney.Infrastructure;
using BusJourney.Infrastructure.Provider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BusJourney.Tests;

public class BusProviderClientTests
{
    private static readonly DeviceSession Session = new("session-1", "device-1");

    // Shape copied from the provider documentation's sample response.
    private const string JourneysResponse = """
        {
          "status": "Success",
          "data": [
            {
              "id": 73083886,
              "partner-id": 330,
              "partner-name": "Partner A",
              "journey": {
                "origin": "Alibeyköy Otogarı",
                "destination": "Ankara (Aşti) Otogarı",
                "departure": "2019-01-12T00:00:00",
                "arrival": "2019-01-12T07:00:00",
                "currency": "TRY",
                "internet-price": 70
              },
              "origin-location": "İstanbul Avrupa",
              "destination-location": "Ankara"
            },
            {
              "id": "73083887",
              "partner-name": "Partner B",
              "journey": {
                "origin": "Esenler Otogarı",
                "destination": "Ankara (Aşti) Otogarı",
                "departure": "2019-01-12T09:30:00",
                "arrival": "2019-01-12T15:30:00",
                "currency": "TRY",
                "internet-price": "75.5"
              },
              "origin-location": "İstanbul Avrupa",
              "destination-location": "Ankara"
            }
          ],
          "message": null,
          "user-message": null
        }
        """;

    [Fact]
    public async Task Sends_basic_token_and_documented_envelope()
    {
        var handler = new StubHttpMessageHandler("""{ "status": "Success", "data": [] }""");
        var client = CreateClient(handler);

        await client.GetLocationsAsync(Session, "ank", CancellationToken.None);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://provider.test/api/location/getbuslocations", request.RequestUri?.ToString());
        Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
        Assert.Equal("test-token", request.Headers.Authorization?.Parameter);

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        Assert.Equal("ank", root.GetProperty("data").GetString());
        Assert.Equal("session-1", root.GetProperty("device-session").GetProperty("session-id").GetString());
        Assert.Equal("device-1", root.GetProperty("device-session").GetProperty("device-id").GetString());
        Assert.Equal("2026-09-26T12:00:00", root.GetProperty("date").GetString());
        Assert.Equal("tr-TR", root.GetProperty("language").GetString());
    }

    [Fact]
    public async Task Maps_documented_journey_response_including_string_numbers()
    {
        var handler = new StubHttpMessageHandler(JourneysResponse);
        var client = CreateClient(handler);

        var journeys = await client.GetJourneysAsync(Session, new JourneyQuery(349, 356, new DateOnly(2019, 1, 12)), CancellationToken.None);

        Assert.Equal(2, journeys.Count);
        Assert.Equal(new Journey(
            73083886, "Partner A", "Alibeyköy Otogarı", "Ankara (Aşti) Otogarı",
            new DateTime(2019, 1, 12, 0, 0, 0), new DateTime(2019, 1, 12, 7, 0, 0),
            70m, "TRY", "İstanbul Avrupa", "Ankara"), journeys[0]);
        Assert.Equal(73083887, journeys[1].Id);
        Assert.Equal(75.5m, journeys[1].Price);

        using var json = JsonDocument.Parse(handler.Requests[0].Body);
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(349, data.GetProperty("origin-id").GetInt32());
        Assert.Equal(356, data.GetProperty("destination-id").GetInt32());
        Assert.Equal("2019-01-12T00:00:00", data.GetProperty("departure-date").GetString());
    }

    [Fact]
    public async Task Non_success_status_becomes_provider_exception()
    {
        var handler = new StubHttpMessageHandler("""
            { "status": "InvalidRoute", "data": null, "message": "no route", "user-message": "Sefer yok" }
            """);
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<ProviderException>(() =>
            client.GetJourneysAsync(Session, new JourneyQuery(1, 2, new DateOnly(2026, 9, 27)), CancellationToken.None));

        Assert.Equal("InvalidRoute", exception.Status);
        Assert.Equal("Sefer yok", exception.UserMessage);
    }

    [Theory]
    [InlineData("not json", HttpStatusCode.OK)]
    [InlineData("""{ "status": "Success" }""", HttpStatusCode.InternalServerError)]
    public async Task Transport_failures_become_provider_exception(string responseBody, HttpStatusCode statusCode)
    {
        var client = CreateClient(new StubHttpMessageHandler(responseBody, statusCode));

        var exception = await Assert.ThrowsAsync<ProviderException>(() =>
            client.GetLocationsAsync(Session, null, CancellationToken.None));

        Assert.Equal(ProviderException.TransportErrorStatus, exception.Status);
    }

    [Fact]
    public async Task Creates_session_from_response()
    {
        var handler = new StubHttpMessageHandler("""
            { "status": "Success", "data": { "session-id": "s-42", "device-id": "d-42" } }
            """);
        var client = CreateClient(handler);

        var session = await client.CreateSessionAsync(CancellationToken.None);

        Assert.Equal(new DeviceSession("s-42", "d-42"), session);
        using var json = JsonDocument.Parse(handler.Requests[0].Body);
        var root = json.RootElement;
        Assert.Equal(1, root.GetProperty("type").GetInt32());
        Assert.Equal("10.0.0.1", root.GetProperty("connection").GetProperty("ip-address").GetString());
        Assert.Equal("5117", root.GetProperty("connection").GetProperty("port").GetString());
        Assert.Equal("Chrome", root.GetProperty("browser").GetProperty("name").GetString());
    }

    /// <summary>Builds the client through the real DI registration, swapping only the network handler.</summary>
    private static IBusProviderClient CreateClient(StubHttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Provider:BaseUrl"] = "https://provider.test/api",
                ["Provider:ApiClientToken"] = "test-token",
                ["Provider:ClientIpAddress"] = "10.0.0.1",
                ["Provider:ClientPort"] = "5117",
                ["Provider:BrowserName"] = "Chrome",
                ["Provider:BrowserVersion"] = "47.0.0.12",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(TestData.Noon));
        services.AddInfrastructure(configuration);
        services.AddHttpClient<IBusProviderClient, BusProviderClient>()
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider().GetRequiredService<IBusProviderClient>();
    }
}
