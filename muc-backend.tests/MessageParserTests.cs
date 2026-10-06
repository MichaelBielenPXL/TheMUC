using MucBackend.Events;
using MucBackend.Parsing;

namespace MucBackend.Tests;

public class MessageParserTests
{
    private readonly MessageParser _parser = new(FixedTime.October6);

    private static string Sample(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Samples", name));

    private static CalendarEvent Event(int month, int day, string title, string? url = null) =>
        new(new DateOnly(2026, month, day), title, url);

    private static readonly CalendarEvent[] FirstWeek =
    [
        Event(9, 28, "AURAfarm Schachtenfakbar - Dionysia", "https://www.facebook.com/events/1660066248783093"),
        Event(9, 29, "oktoberfest fakbar - ASG", "https://www.facebook.com/events/1399000931701763"),
        Event(9, 30, "drill - Filii", "https://www.facebook.com/events/965976569179019"),
        Event(10, 1, "vettige doop - Agogica", "https://www.facebook.com/events/1794945501745502"),
        Event(10, 1, "Rékinea - Shot till you drop", "https://www.facebook.com/events/2939666633063121"),
        Event(10, 2, "Aprés-Vacantus", "https://www.facebook.com/events/1650977569879809"),
    ];

    private static readonly CalendarEvent[] SecondWeek =
    [
        Event(10, 5, "Lintenstoet (te laat om nog in te schrijven voor de stoet, maar kom langs om 21:30 op het dusart)", "https://www.hasselt.be/nl/jeugdhasselt/lintenstoet"),
        Event(10, 6, "Open repetitie - Ambifaarke", "https://www.facebook.com/events/1320940419973232"),
        Event(10, 6, "Introcantus - Biomedica", "https://www.facebook.com/events/1837667060589331"),
        Event(10, 7, "Student take off (Hexion gaat samen)", "https://www.studenttakeoff.be/"),
        Event(10, 8, "Hexion gaat samen op café in de Happy snooker"),
        Event(10, 8, "Vettige doop - ASG", "https://www.facebook.com/events/1847401669594900"),
        Event(10, 8, "Doop - Filii Lamberti", "https://www.facebook.com/events/1895662258510896"),
        Event(10, 8, "Boiler Bunker TD - Themis", "https://www.facebook.com/events/1755786382143808"),
        Event(10, 9, "Doopcantus - Agogica", "https://www.facebook.com/events/1569160144893398"),
    ];

    [Fact]
    public void Parses_the_first_sample()
    {
        var result = _parser.Parse(Sample("sample1.txt"));

        Assert.Equal(FirstWeek, result.Events);
        Assert.Equal(
            [
                "Het is weer tijd voor een week overzicht!",
                "Hier post ik elke week de evenementen van alle verenigingen en clubs voor de komende week.",
                "Hexion gaat sowieso aanwezig zijn op de fakbar maandag en de cantus vrijdag ;)",
            ],
            result.SkippedLines);
    }

    [Fact]
    public void Parses_the_second_sample()
    {
        var result = _parser.Parse(Sample("sample2.txt"));

        Assert.Equal(SecondWeek, result.Events);
        Assert.Equal(["Wij zijn sowieso op student take off en donderdag in de Happy Snooker ;)"], result.SkippedLines);
    }

    [Fact]
    public void Parses_two_messages_pasted_together()
    {
        var result = _parser.Parse(Sample("sample3.txt"));

        CalendarEvent[] expected =
        [
            .. FirstWeek[..5],
            Event(10, 2, "Aprés-Vacantus - Prominos", "https://www.facebook.com/events/1650977569879809"),
            .. SecondWeek,
        ];
        Assert.Equal(expected, result.Events);
        Assert.Contains("Matteo — 04/10/2026, 21:11", result.SkippedLines);
        Assert.Contains("Hexion gaat sowieso aanwezig zijn op de fakbar maandag en de cantus vrijdag ;)", result.SkippedLines);
    }

    [Fact]
    public void Windows_line_endings_give_the_same_result()
    {
        var unix = Sample("sample1.txt").ReplaceLineEndings("\n");
        var windows = unix.Replace("\n", "\r\n");

        Assert.Equal(_parser.Parse(unix).Events, _parser.Parse(windows).Events);
        Assert.Equal(FirstWeek, _parser.Parse(windows).Events);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void A_whitespace_only_line_closes_the_day(string blank)
    {
        var result = _parser.Parse($"Maandag 28 september\nFakbar\n{blank}\nTot dan!");

        Assert.Equal([Event(9, 28, "Fakbar")], result.Events);
        Assert.Equal(["Tot dan!"], result.SkippedLines);
    }

    [Fact]
    public void A_repeated_event_appears_once()
    {
        var day = "Maandag 28 september\nFakbar - Dionysia\nhttps://example.com/a\n";

        var result = _parser.Parse(day + "\n" + day);

        Assert.Equal([Event(9, 28, "Fakbar - Dionysia", "https://example.com/a")], result.Events);
    }

    [Fact]
    public void A_second_link_under_one_event_is_skipped()
    {
        var result = _parser.Parse("Maandag 28 september\nFakbar\nhttps://example.com/a\nhttps://example.com/b");

        Assert.Equal([Event(9, 28, "Fakbar", "https://example.com/a")], result.Events);
        Assert.Equal(["https://example.com/b"], result.SkippedLines);
    }

    [Fact]
    public void A_link_directly_under_the_date_is_skipped()
    {
        var result = _parser.Parse("Maandag 28 september\nhttps://example.com/a\nFakbar");

        Assert.Equal([Event(9, 28, "Fakbar")], result.Events);
        Assert.Equal(["https://example.com/a"], result.SkippedLines);
    }

    [Fact]
    public void A_link_does_not_attach_to_the_previous_day()
    {
        var result = _parser.Parse("Maandag 28 september\nFakbar\nDinsdag 29 september\nhttps://example.com/a");

        Assert.Equal([Event(9, 28, "Fakbar")], result.Events);
        Assert.Equal(["https://example.com/a"], result.SkippedLines);
    }

    [Fact]
    public void Text_without_dates_gives_no_events()
    {
        var result = _parser.Parse("Hallo allemaal\nTot volgende week");

        Assert.Empty(result.Events);
        Assert.Equal(["Hallo allemaal", "Tot volgende week"], result.SkippedLines);
    }
}
