using System.Text;
using MucBackend.Calendar;
using MucBackend.Events;

namespace MucBackend.Tests;

public class IcsWriterTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Date = new(2026, 9, 28);

    private static string[] Lines(string ics) => ics.Split("\r\n");

    // Reverses RFC 5545 line folding.
    private static string Unfold(string ics) => ics.Replace("\r\n ", "");

    [Fact]
    public void Writes_an_all_day_event()
    {
        var ics = IcsWriter.Write([new CalendarEvent(Date, "Fakbar - Dionysia", "https://www.facebook.com/events/1")], Now);

        var lines = Lines(ics);
        Assert.Equal("BEGIN:VCALENDAR", lines[0]);
        Assert.Equal("VERSION:2.0", lines[1]);
        Assert.Contains("PRODID:-//Mercurius Unified Calendar//EN", lines);
        Assert.Contains("CALSCALE:GREGORIAN", lines);
        Assert.Contains("X-WR-CALNAME:Mercurius Unified Calendar", lines);
        Assert.Contains("BEGIN:VEVENT", lines);
        Assert.Contains("DTSTAMP:20261006T123000Z", lines);
        Assert.Contains("DTSTART;VALUE=DATE:20260928", lines);
        Assert.Contains("DTEND;VALUE=DATE:20260929", lines);
        Assert.Contains("SUMMARY:Fakbar - Dionysia", lines);
        Assert.Contains("URL:https://www.facebook.com/events/1", lines);
        Assert.Contains("DESCRIPTION:https://www.facebook.com/events/1", lines);
        Assert.Contains("END:VEVENT", lines);
        Assert.Equal("END:VCALENDAR", lines[^2]);
        Assert.Equal("", lines[^1]);
    }

    [Fact]
    public void Uses_crlf_line_endings_only()
    {
        var ics = IcsWriter.Write([new CalendarEvent(Date, "Fakbar", null)], Now);

        Assert.DoesNotContain("\n", ics.Replace("\r\n", ""));
        Assert.EndsWith("\r\n", ics);
    }

    [Fact]
    public void Omits_url_and_description_when_there_is_no_link()
    {
        var ics = IcsWriter.Write([new CalendarEvent(Date, "Fakbar", null)], Now);

        Assert.DoesNotContain("URL:", ics);
        Assert.DoesNotContain("DESCRIPTION:", ics);
    }

    [Fact]
    public void Ends_an_event_on_the_last_day_of_the_month_in_the_next_month()
    {
        var ics = IcsWriter.Write([new CalendarEvent(new DateOnly(2026, 9, 30), "Fakbar", null)], Now);

        Assert.Contains("DTEND;VALUE=DATE:20261001", Lines(ics));
    }

    [Fact]
    public void Escapes_special_characters_in_the_title()
    {
        var ics = IcsWriter.Write([new CalendarEvent(Date, @"Bier, wijn; en \ meer", null)], Now);

        Assert.Contains(@"SUMMARY:Bier\, wijn\; en \\ meer", Lines(ics));
    }

    [Fact]
    public void A_title_cannot_inject_calendar_lines()
    {
        var ics = IcsWriter.Write([new CalendarEvent(Date, "Fakbar\r\nEND:VEVENT\nBEGIN:VEVENT", null)], Now);

        Assert.Single(Lines(ics), line => line == "BEGIN:VEVENT");
        Assert.Contains(@"SUMMARY:Fakbar\nEND:VEVENT\nBEGIN:VEVENT", Lines(Unfold(ics)));
    }

    [Theory]
    [InlineData('a')]
    [InlineData('é')]
    [InlineData('€')]
    public void Folds_long_lines_at_75_octets_without_splitting_a_character(char c)
    {
        var title = new string(c, 200);

        var ics = IcsWriter.Write([new CalendarEvent(Date, title, null)], Now);

        Assert.All(Lines(ics), line => Assert.True(Encoding.UTF8.GetByteCount(line) <= 75, line));
        Assert.Contains($"SUMMARY:{title}", Lines(Unfold(ics)));
    }

    [Fact]
    public void Folds_a_title_with_emoji()
    {
        var title = string.Concat(Enumerable.Repeat("🍺", 60));

        var ics = IcsWriter.Write([new CalendarEvent(Date, title, null)], Now);

        Assert.All(Lines(ics), line => Assert.True(Encoding.UTF8.GetByteCount(line) <= 75, line));
        Assert.Contains($"SUMMARY:{title}", Lines(Unfold(ics)));
    }

    [Fact]
    public void Uid_is_stable_and_depends_on_date_and_title()
    {
        static string Uid(CalendarEvent e) => Lines(IcsWriter.Write([e], Now)).Single(l => l.StartsWith("UID:"));

        var uid = Uid(new CalendarEvent(Date, "Fakbar", null));

        Assert.Equal(uid, Uid(new CalendarEvent(Date, "Fakbar", "https://example.com")));
        Assert.NotEqual(uid, Uid(new CalendarEvent(Date, "Cantus", null)));
        Assert.NotEqual(uid, Uid(new CalendarEvent(Date.AddDays(1), "Fakbar", null)));
        Assert.EndsWith("@mercurius-unified-calendar", uid);
    }

    [Fact]
    public void Writes_events_with_the_same_date_and_title_once()
    {
        var ics = IcsWriter.Write(
            [new CalendarEvent(Date, "Fakbar", null), new CalendarEvent(Date, "Fakbar", "https://example.com")], Now);

        Assert.Single(Lines(ics), line => line == "BEGIN:VEVENT");
    }
}
