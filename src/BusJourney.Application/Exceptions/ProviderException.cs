namespace BusJourney.Application.Exceptions;

/// <summary>
/// Raised when the provider API cannot fulfil a request, either because it returned a non-success
/// status (e.g. <c>InvalidRoute</c>) or because the call itself failed (network, timeout, bad payload).
/// </summary>
public sealed class ProviderException : Exception
{
    public const string TransportErrorStatus = "TransportError";
    public const string TimeoutStatus = "Timeout";

    public ProviderException(string status, string? userMessage, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Status = status;
        UserMessage = userMessage;
    }

    /// <summary>Provider response status, or one of the local status constants.</summary>
    public string Status { get; }

    /// <summary>User-friendly message sent by the provider, if any.</summary>
    public string? UserMessage { get; }
}
