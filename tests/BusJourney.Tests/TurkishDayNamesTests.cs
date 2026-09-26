using BusJourney.Web.Formatting;

namespace BusJourney.Tests;

public class TurkishDayNamesTests
{
    [Theory]
    [InlineData("2026-09-27", "Pazar'ı Pazartesi'ye bağlayan gece")]
    [InlineData("2026-09-28", "Pazartesi'yi Salı'ya bağlayan gece")]
    [InlineData("2026-09-29", "Salı'yı Çarşamba'ya bağlayan gece")]
    [InlineData("2026-09-30", "Çarşamba'yı Perşembe'ye bağlayan gece")]
    [InlineData("2026-10-01", "Perşembe'yi Cuma'ya bağlayan gece")]
    [InlineData("2026-10-02", "Cuma'yı Cumartesi'ye bağlayan gece")]
    [InlineData("2026-10-03", "Cumartesi'yi Pazar'a bağlayan gece")]
    public void Describes_the_night_after_a_date(string date, string expected)
    {
        Assert.Equal(expected, TurkishDayNames.NightAfter(DateOnly.Parse(date)));
    }
}
