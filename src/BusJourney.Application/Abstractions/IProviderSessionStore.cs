using BusJourney.Application.Models;

namespace BusJourney.Application.Abstractions;

/// <summary>
/// Keeps the provider session of the current end user. Each visitor gets their own session.
/// </summary>
public interface IProviderSessionStore
{
    DeviceSession? Get();

    void Set(DeviceSession session);
}
