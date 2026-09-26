using System.Text.Json;
using BusJourney.Application.Abstractions;
using BusJourney.Application.Models;

namespace BusJourney.Web.Session;

/// <summary>Stores the provider session in the visitor's ASP.NET Core Session.</summary>
public sealed class HttpSessionProviderSessionStore(IHttpContextAccessor httpContextAccessor) : IProviderSessionStore
{
    private const string Key = "ProviderSession";

    private ISession Session =>
        httpContextAccessor.HttpContext?.Session
        ?? throw new InvalidOperationException("The provider session store requires an active HTTP request.");

    public DeviceSession? Get()
    {
        var json = Session.GetString(Key);
        return json is null ? null : JsonSerializer.Deserialize<DeviceSession>(json);
    }

    public void Set(DeviceSession session) => Session.SetString(Key, JsonSerializer.Serialize(session));
}
