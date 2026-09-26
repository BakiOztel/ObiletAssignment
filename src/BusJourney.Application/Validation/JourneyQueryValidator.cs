using System.ComponentModel.DataAnnotations;
using BusJourney.Application.Common;
using BusJourney.Application.Models;

namespace BusJourney.Application.Validation;

/// <summary>
/// Business rules of a journey search. The same rules are mirrored on the client for instant feedback,
/// but this class is the source of truth.
/// </summary>
public sealed class JourneyQueryValidator(TimeProvider timeProvider)
{
    public const string SameLocationMessage = "Kalkış ve varış noktası aynı olamaz.";
    public const string PastDateMessage = "Geçmiş bir tarih seçilemez.";

    public IReadOnlyList<ValidationResult> Validate(JourneyQuery query)
    {
        var errors = new List<ValidationResult>();

        if (query.OriginId == query.DestinationId)
        {
            errors.Add(new ValidationResult(SameLocationMessage, [nameof(JourneyQuery.DestinationId)]));
        }

        if (query.DepartureDate < timeProvider.GetTurkeyToday())
        {
            errors.Add(new ValidationResult(PastDateMessage, [nameof(JourneyQuery.DepartureDate)]));
        }

        return errors;
    }
}
