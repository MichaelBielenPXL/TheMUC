using System.Security.Cryptography;
using System.Text;
using MucBackend.Events;

namespace MucBackend.Calendar;

public static class IcsWriter
{
    private const int MaxLineOctets = 75;

    public static string Write(IEnumerable<CalendarEvent> events, DateTimeOffset now)
    {
        var ics = new StringBuilder();
        Append(ics, "BEGIN:VCALENDAR");
        Append(ics, "VERSION:2.0");
        Append(ics, "PRODID:-//Mercurius Unified Calendar//EN");
        Append(ics, "CALSCALE:GREGORIAN");

        // Two events sharing a UID would be one entry to a calendar anyway.
        foreach (var e in events.DistinctBy(Uid))
        {
            Append(ics, "BEGIN:VEVENT");
            Append(ics, $"UID:{Uid(e)}");
            Append(ics, $"DTSTAMP:{now.UtcDateTime:yyyyMMdd'T'HHmmss'Z'}");
            Append(ics, $"DTSTART;VALUE=DATE:{e.Date:yyyyMMdd}");
            Append(ics, $"DTEND;VALUE=DATE:{e.Date.AddDays(1):yyyyMMdd}");
            Append(ics, $"SUMMARY:{Escape(e.Title)}");
            if (e.Url is not null)
            {
                // AbsoluteUri is percent-encoded, so it cannot carry a line break into the file.
                var url = new Uri(e.Url).AbsoluteUri;
                Append(ics, $"URL:{url}");
                Append(ics, $"DESCRIPTION:{Escape(url)}");
            }
            Append(ics, "END:VEVENT");
        }

        Append(ics, "END:VCALENDAR");
        return ics.ToString();
    }

    private static string Uid(CalendarEvent e)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{e.Date:yyyy-MM-dd}|{e.Title}"));
        return $"{Convert.ToHexStringLower(hash)[..32]}@mercurius-unified-calendar";
    }

    private static string Escape(string text)
    {
        var escaped = text
            .Replace("\\", "\\\\")
            .Replace(";", "\\;")
            .Replace(",", "\\,")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n");
        return string.Concat(escaped.Where(c => !char.IsControl(c)));
    }

    // RFC 5545 3.1: lines are at most 75 octets; a continuation line starts with one space.
    private static void Append(StringBuilder ics, string line)
    {
        var octets = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            if (octets + rune.Utf8SequenceLength > MaxLineOctets)
            {
                ics.Append("\r\n ");
                octets = 1;
            }
            ics.Append(rune.ToString());
            octets += rune.Utf8SequenceLength;
        }
        ics.Append("\r\n");
    }
}
