using MucBackend.Parsing;

namespace MucBackend.Tests;

public class DateLineParserTests
{
    [Theory]
    [InlineData("Maandag 28 september", 28, 9, null, DayOfWeek.Monday)]
    [InlineData("maandag 28 september", 28, 9, null, DayOfWeek.Monday)]
    [InlineData("Donderdag 1 oktober", 1, 10, null, DayOfWeek.Thursday)]
    [InlineData("ma 28 sept", 28, 9, null, DayOfWeek.Monday)]
    [InlineData("di. 29 sep.", 29, 9, null, DayOfWeek.Tuesday)]
    [InlineData("Woe 30 sep", 30, 9, null, DayOfWeek.Wednesday)]
    [InlineData("zaterdag 3 okt", 3, 10, null, DayOfWeek.Saturday)]
    [InlineData("Vrijdag 2 oktober 2026", 2, 10, 2026, DayOfWeek.Friday)]
    [InlineData("28 september", 28, 9, null, null)]
    [InlineData("1e mei", 1, 5, null, null)]
    [InlineData("3 maart", 3, 3, null, null)]
    [InlineData("3 mrt", 3, 3, null, null)]
    [InlineData("Monday 28 September", 28, 9, null, DayOfWeek.Monday)]
    [InlineData("Mon, 28 Sep 2026", 28, 9, 2026, DayOfWeek.Monday)]
    [InlineData("Monday, September 28th", 28, 9, null, DayOfWeek.Monday)]
    [InlineData("September 28, 2026", 28, 9, 2026, null)]
    [InlineData("28th of September", 28, 9, null, null)]
    [InlineData("28/09", 28, 9, null, null)]
    [InlineData("28-9", 28, 9, null, null)]
    [InlineData("28.09", 28, 9, null, null)]
    [InlineData("04/10/2026", 4, 10, 2026, null)]
    [InlineData("4/10/26", 4, 10, 2026, null)]
    [InlineData("maandag 28/09", 28, 9, null, DayOfWeek.Monday)]
    [InlineData("2026-09-28", 28, 9, 2026, null)]
    public void Recognises_date_lines(string line, int day, int month, int? year, DayOfWeek? weekday)
    {
        Assert.Equal(new DateLine(day, month, year, weekday), DateLineParser.TryParse(line));
    }

    [Theory]
    [InlineData("**Maandag 28 september**")]
    [InlineData("Maandag 28 september:")]
    [InlineData("__Maandag 28 september__")]
    [InlineData("## Maandag 28 september")]
    [InlineData("**Maandag 28 september:**")]
    [InlineData("  Maandag  28   september  ")]
    [InlineData("Maandag 28 september")]
    [InlineData("📅 Maandag 28 september")]
    [InlineData("Maandag 28 september 🍻")]
    [InlineData("- Maandag 28 september")]
    [InlineData("• Maandag 28 september")]
    [InlineData("(Maandag 28 september)")]
    public void Ignores_chat_decoration_and_odd_spacing(string line)
    {
        Assert.Equal(new DateLine(28, 9, null, DayOfWeek.Monday), DateLineParser.TryParse(line));
    }

    [Theory]
    [InlineData("Matteo — 04/10/2026, 21:11")]
    [InlineData("AURAfarm Schachtenfakbar - Dionysia")]
    [InlineData("Hexion gaat sowieso aanwezig zijn op de fakbar maandag en de cantus vrijdag ;)")]
    [InlineData("Lintenstoet (kom langs om 21:30 op het dusart)")]
    [InlineData("Feest op 28 september in de fakbar")]
    [InlineData("https://www.facebook.com/events/1660066248783093")]
    [InlineData("Maandag")]
    [InlineData("Doop 2")]
    [InlineData("21:30")]
    [InlineData("21.30")]
    [InlineData("31 februari")]
    [InlineData("32/01")]
    [InlineData("5/13")]
    [InlineData("blursdag 28 september")]
    [InlineData("")]
    public void Rejects_lines_that_are_not_only_a_date(string line)
    {
        Assert.Null(DateLineParser.TryParse(line));
    }

    [Fact]
    public void Accepts_29_february_without_a_year()
    {
        Assert.Equal(new DateLine(29, 2, null, null), DateLineParser.TryParse("29 februari"));
    }
}
