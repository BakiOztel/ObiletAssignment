using System.Net.Http.Headers;
using BusJourney.Application.Abstractions;
using BusJourney.Infrastructure.Provider;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BusJourney.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ProviderOptions>()
            .Bind(configuration.GetSection(ProviderOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Typed client: IHttpClientFactory manages handler lifetimes; base address, timeout and the
        // Basic auth token are configured once here instead of on every request.
        services.AddHttpClient<IBusProviderClient, BusProviderClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ProviderOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", options.ApiClientToken);
        });

        return services;
    }
}
