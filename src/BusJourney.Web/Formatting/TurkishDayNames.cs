namespace BusJourney.Web.Formatting;

/// <summary>
/// Turkish day names with the case suffixes needed for "X'i Y'ye bağlayan gece" (the night between X and Y).
/// Suffixes follow vowel harmony, so they are listed rather than derived.
/// </summary>
public static class TurkishDayNames
{
    private static readonly Dictionary<DayOfWeek, (string Accusative, string Dative)> Names = new()
    {
        [DayOfWeek.Monday] = ("Pazartesi'yi", "Pazartesi'ye"),
        [DayOfWeek.Tuesday] = ("Salı'yı", "Salı'ya"),
        [DayOfWeek.Wednesday] = ("Çarşamba'yı", "Çarşamba'ya"),
        [DayOfWeek.Thursday] = ("Perşembe'yi", "Perşembe'ye"),
        [DayOfWeek.Friday] = ("Cuma'yı", "Cuma'ya"),
        [DayOfWeek.Saturday] = ("Cumartesi'yi", "Cumartesi'ye"),
        [DayOfWeek.Sunday] = ("Pazar'ı", "Pazar'a"),
    };

    /// <summary>E.g. for a Sunday: "Pazar'ı Pazartesi'ye bağlayan gece".</summary>
    public static string NightAfter(DateOnly date) =>
        $"{Names[date.DayOfWeek].Accusative} {Names[date.AddDays(1).DayOfWeek].Dative} bağlayan gece";
}
