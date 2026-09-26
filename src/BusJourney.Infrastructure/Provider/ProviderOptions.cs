using System.ComponentModel.DataAnnotations;

namespace BusJourney.Infrastructure.Provider;

/// <summary>
/// Settings of the provider API, bound from the "Provider" configuration section.
/// Secrets (token, base URL) come from user-secrets or environment variables, never from source control.
/// </summary>
public sealed class ProviderOptions
{
    public const string SectionName = "Provider";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string ApiClientToken { get; set; } = string.Empty;

    [Required]
    public string Language { get; set; } = "tr-TR";

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 15;

    // Client identity sent while creating a session. The API rejects sessions without a port and browser info.

    [Required]
    public string ClientIpAddress { get; set; } = string.Empty;

    [Required]
    public string ClientPort { get; set; } = string.Empty;

    [Required]
    public string BrowserName { get; set; } = string.Empty;

    [Required]
    public string BrowserVersion { get; set; } = string.Empty;
}
