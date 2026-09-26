using BusJourney.Application.Abstractions;
using BusJourney.Application.Models;

namespace BusJourney.Application.Services;

/// <summary>
/// Single entry point for obtaining the current user's provider session.
/// The session is created lazily on first use and reused for all subsequent requests of that user.
/// </summary>
public sealed class ProviderSessionAccessor(IBusProviderClient client, IProviderSessionStore store)
{
    public async Task<DeviceSession> GetAsync(CancellationToken cancellationToken)
    {
        var session = store.Get();
        if (session is not null)
        {
            return session;
        }

        // ponytail: two parallel first requests of the same user may both create a session; the last one wins. Harmless, so no lock.
        session = await client.CreateSessionAsync(cancellationToken);
        store.Set(session);
        return session;
    }
}
