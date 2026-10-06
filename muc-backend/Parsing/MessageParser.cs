using System.Text.RegularExpressions;
using MucBackend.Events;

namespace MucBackend.Parsing;

public record ParseResult(IReadOnlyList<CalendarEvent> Events, IReadOnlyList<string> SkippedLines);

public class MessageParser(TimeProvider time)
{
    // Zero-width characters and a byte-order mark survive copy-paste but are not whitespace to Trim().
    private static readonly Regex Invisible = new("[\u200B\u200C\u200D\uFEFF]");
    private static readonly Regex ListMarker = new(@"^[-*•]\s+");

    public ParseResult Parse(string text)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var events = new List<CalendarEvent>();
        var skipped = new List<string>();
        DateOnly? day = null;
        var dayHasEvent = false;

        foreach (var raw in text.Split('\n'))
        {
            var line = ListMarker.Replace(Invisible.Replace(raw, "").Trim(), "");

            if (line.Length == 0)
            {
                day = null;
                continue;
            }

            if (DateLineParser.TryParse(line) is { } dateLine && YearResolver.Resolve(dateLine, today) is { } date)
            {
                day = date;
                dayHasEvent = false;
                continue;
            }

            if (day is null)
            {
                skipped.Add(line);
                continue;
            }

            // Discord writes <https://...> to suppress the link preview.
            var link = line.Trim('<', '>');
            if (IsUrl(link))
            {
                if (dayHasEvent && events[^1].Url is null) events[^1] = events[^1] with { Url = link };
                else skipped.Add(line);
                continue;
            }

            events.Add(new CalendarEvent(day.Value, line, null));
            dayHasEvent = true;
        }

        // Two overlapping weekly messages pasted together repeat the shared days.
        return new ParseResult(events.DistinctBy(e => (e.Date, e.Title)).ToList(), skipped);
    }

    private static bool IsUrl(string line) =>
        !line.Contains(' ')
        && Uri.TryCreate(line, UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https";
}
