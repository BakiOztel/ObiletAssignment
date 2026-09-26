using BusJourney.Application.Models;

namespace BusJourney.Application.Abstractions;

/// <summary>
/// Port to the external bus ticketing API. Implemented in the Infrastructure layer.
/// Implementations throw <see cref="Exceptions.ProviderException"/> when the API reports a failure.
/// </summary>
public interface IBusProviderClient
{
    Task<DeviceSession> CreateSessionAsync(CancellationToken cancellationToken);

    /// <param name="keyword">Search keyword; <c>null</c> or empty returns every location.</param>
    Task<IReadOnlyList<Location>> GetLocationsAsync(DeviceSession session, string? keyword, CancellationToken cancellationToken);

    Task<IReadOnlyList<Journey>> GetJourneysAsync(DeviceSession session, JourneyQuery query, CancellationToken cancellationToken);
}
