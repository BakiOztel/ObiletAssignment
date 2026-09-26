using BusJourney.Application.Models;
using BusJourney.Application.Validation;

namespace BusJourney.Tests;

public class JourneyQueryValidatorTests
{
    private static readonly DateOnly Today = new(2026, 9, 26);
    private readonly JourneyQueryValidator _validator = new(new FixedTimeProvider(TestData.Noon));

    [Fact]
    public void Same_origin_and_destination_is_rejected()
    {
        var errors = _validator.Validate(new JourneyQuery(349, 349, Today));

        Assert.Equal(JourneyQueryValidator.SameLocationMessage, Assert.Single(errors).ErrorMessage);
    }

    [Fact]
    public void Past_date_is_rejected()
    {
        var errors = _validator.Validate(new JourneyQuery(349, 356, Today.AddDays(-1)));

        Assert.Equal(JourneyQueryValidator.PastDateMessage, Assert.Single(errors).ErrorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Today_and_future_dates_are_valid(int daysFromToday)
    {
        Assert.Empty(_validator.Validate(new JourneyQuery(349, 356, Today.AddDays(daysFromToday))));
    }

    [Fact]
    public void Today_is_evaluated_in_Turkish_time()
    {
        // 22:30 UTC on the 26th is already 01:30 on the 27th in Turkey, so the 26th is in the past.
        var validator = new JourneyQueryValidator(new FixedTimeProvider(new DateTimeOffset(2026, 9, 26, 22, 30, 0, TimeSpan.Zero)));

        var errors = validator.Validate(new JourneyQuery(349, 356, Today));

        Assert.Equal(JourneyQueryValidator.PastDateMessage, Assert.Single(errors).ErrorMessage);
    }
}
