using BusJourney.Application.Services;
using BusJourney.Application.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace BusJourney.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProviderSessionAccessor>();
        services.AddScoped<LocationService>();
        services.AddScoped<JourneyService>();
        services.AddSingleton<JourneyQueryValidator>();
        return services;
    }
}
