using System.Text.RegularExpressions;

namespace MucBackend.Parsing;

public record DateLine(int Day, int Month, int? Year, DayOfWeek? Weekday);

public static class DateLineParser
{
    private static readonly Dictionary<string, DayOfWeek> Weekdays = new()
    {
        ["maandag"] = DayOfWeek.Monday, ["ma"] = DayOfWeek.Monday,
        ["monday"] = DayOfWeek.Monday, ["mon"] = DayOfWeek.Monday,
        ["dinsdag"] = DayOfWeek.Tuesday, ["di"] = DayOfWeek.Tuesday,
        ["tuesday"] = DayOfWeek.Tuesday, ["tue"] = DayOfWeek.Tuesday, ["tues"] = DayOfWeek.Tuesday,
        ["woensdag"] = DayOfWeek.Wednesday, ["wo"] = DayOfWeek.Wednesday, ["woe"] = DayOfWeek.Wednesday,
        ["wednesday"] = DayOfWeek.Wednesday, ["wed"] = DayOfWeek.Wednesday,
        ["donderdag"] = DayOfWeek.Thursday, ["do"] = DayOfWeek.Thursday,
        ["thursday"] = DayOfWeek.Thursday, ["thu"] = DayOfWeek.Thursday,
        ["thur"] = DayOfWeek.Thursday, ["thurs"] = DayOfWeek.Thursday,
        ["vrijdag"] = DayOfWeek.Friday, ["vr"] = DayOfWeek.Friday, ["vrij"] = DayOfWeek.Friday,
        ["friday"] = DayOfWeek.Friday, ["fri"] = DayOfWeek.Friday,
        ["zaterdag"] = DayOfWeek.Saturday, ["za"] = DayOfWeek.Saturday, ["zat"] = DayOfWeek.Saturday,
        ["saturday"] = DayOfWeek.Saturday, ["sat"] = DayOfWeek.Saturday,
        ["zondag"] = DayOfWeek.Sunday, ["zo"] = DayOfWeek.Sunday, ["zon"] = DayOfWeek.Sunday,
        ["sunday"] = DayOfWeek.Sunday, ["sun"] = DayOfWeek.Sunday,
    };

    private static readonly Dictionary<string, int> Months = new()
    {
        ["januari"] = 1, ["january"] = 1, ["jan"] = 1,
        ["februari"] = 2, ["february"] = 2, ["feb"] = 2,
        ["maart"] = 3, ["march"] = 3, ["mrt"] = 3, ["mar"] = 3,
        ["april"] = 4, ["apr"] = 4,
        ["mei"] = 5, ["may"] = 5,
        ["juni"] = 6, ["june"] = 6, ["jun"] = 6,
        ["juli"] = 7, ["july"] = 7, ["jul"] = 7,
        ["augustus"] = 8, ["august"] = 8, ["aug"] = 8,
        ["september"] = 9, ["sept"] = 9, ["sep"] = 9,
        ["oktober"] = 10, ["october"] = 10, ["okt"] = 10, ["oct"] = 10,
        ["november"] = 11, ["nov"] = 11,
        ["december"] = 12, ["dec"] = 12,
    };

    // Characters chat apps wrap around a heading: **bold**, __underline__, # heading, "date:".
    private static readonly char[] Decoration = ['*', '_', '#', '~', '>', ':', ' '];

    private const string Weekday = @"(?:(?<wd>[a-z]+)\.?,?\s+)?";
    private const string Year = @"(?:,?\s+(?<y>\d{4}))?";

    // Every pattern is anchored at both ends: a date line is nothing but a date.
    private static readonly Regex[] Patterns =
    [
        // [maandag] 28 september [2026]
        new($@"^{Weekday}(?<d>\d{{1,2}})(?:ste|de|e|st|nd|rd|th)?\.?\s+(?:of\s+)?(?<m>[a-z]+)\.?{Year}$"),
        // [monday,] september 28[th][, 2026]
        new($@"^{Weekday}(?<m>[a-z]+)\.?\s+(?<d>\d{{1,2}})(?:st|nd|rd|th)?{Year}$"),
        // [maandag] 28/09[/2026], day first
        new($@"^{Weekday}(?<d>\d{{1,2}})[/.\-](?<m>\d{{1,2}})(?:[/.\-](?<y>\d{{4}}|\d{{2}}))?$"),
        // 2026-09-28
        new($@"^{Weekday}(?<y>\d{{4}})-(?<m>\d{{1,2}})-(?<d>\d{{1,2}})$"),
    ];

    public static DateLine? TryParse(string line)
    {
        var text = Regex.Replace(line.Trim().Trim(Decoration), @"\s+", " ").ToLowerInvariant();

        foreach (var pattern in Patterns)
        {
            var match = pattern.Match(text);
            if (!match.Success) continue;

            DayOfWeek? weekday = null;
            if (match.Groups["wd"].Success)
            {
                if (!Weekdays.TryGetValue(match.Groups["wd"].Value, out var parsed)) continue;
                weekday = parsed;
            }

            var monthText = match.Groups["m"].Value;
            if (!int.TryParse(monthText, out var month) && !Months.TryGetValue(monthText, out month)) continue;

            var day = int.Parse(match.Groups["d"].Value);
            int? year = match.Groups["y"].Success ? int.Parse(match.Groups["y"].Value) : null;
            if (year < 100) year += 2000;

            // 2024 is a leap year, so 29 February passes when the year is still unknown.
            if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year ?? 2024, month)) continue;

            return new DateLine(day, month, year, weekday);
        }

        return null;
    }
}
