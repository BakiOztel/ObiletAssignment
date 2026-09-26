using System.ComponentModel.DataAnnotations;
using BusJourney.Application.Models;

namespace BusJourney.Web.Models;

/// <summary>Query string of the journey list page: <c>/journey?originId=..&amp;destinationId=..&amp;date=yyyy-MM-dd</c>.</summary>
public sealed class JourneySearchInput
{
    [Required(ErrorMessage = "Lütfen kalkış noktası seçin.")]
    public int? OriginId { get; set; }

    [Required(ErrorMessage = "Lütfen varış noktası seçin.")]
    public int? DestinationId { get; set; }

    [Required(ErrorMessage = "Lütfen bir tarih seçin.")]
    public DateOnly? Date { get; set; }
}

public sealed record SearchViewModel(
    Location? Origin,
    Location? Destination,
    DateOnly DepartureDate,
    DateOnly Today,
    IReadOnlyList<Location> InitialSuggestions)
{
    // Suggestions shown when a location field is focused but empty; typing queries the backend instead.
    private const int InitialSuggestionCount = 10;

    public DateOnly Tomorrow => Today.AddDays(1);

    public static SearchViewModel Create(SearchDefaults defaults, DateOnly today) => new(
        defaults.Origin,
        defaults.Destination,
        defaults.DepartureDate,
        today,
        defaults.Locations.Take(InitialSuggestionCount).ToList());
}

/// <summary>Model of the reusable origin / destination field partial.</summary>
public sealed record LocationFieldModel(string Key, string InputName, string Label, Location? Value);
