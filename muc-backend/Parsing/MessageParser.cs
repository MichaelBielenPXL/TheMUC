using MucBackend.Events;

namespace MucBackend.Parsing;

public record ParseResult(IReadOnlyList<CalendarEvent> Events, IReadOnlyList<string> SkippedLines);

public class MessageParser(TimeProvider time)
{
    public ParseResult Parse(string text)
    {
        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var events = new List<CalendarEvent>();
        var skipped = new List<string>();
        DateOnly? day = null;
        var dayHasEvent = false;

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();

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

            if (IsUrl(line))
            {
                if (dayHasEvent && events[^1].Url is null) events[^1] = events[^1] with { Url = line };
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
