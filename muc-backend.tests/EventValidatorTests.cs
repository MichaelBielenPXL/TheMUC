using MucBackend.Events;

namespace MucBackend.Tests;

public class EventValidatorTests
{
    private static readonly DateOnly Date = new(2026, 9, 28);

    [Theory]
    [InlineData("Fakbar", null)]
    [InlineData("Fakbar", "https://www.facebook.com/events/1")]
    [InlineData("Fakbar", "http://example.com")]
    public void Accepts_valid_events(string title, string? url)
    {
        Assert.Null(EventValidator.Validate(new CalendarEvent(Date, title, url)));
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("Fakbar", "")]
    [InlineData("Fakbar", "facebook.com/events/1")]
    [InlineData("Fakbar", "/events/1")]
    [InlineData("Fakbar", "javascript:alert(1)")]
    [InlineData("Fakbar", "ftp://example.com/file")]
    [InlineData("Fakbar", "https://example.com/a b")]
    public void Rejects_invalid_events(string title, string? url)
    {
        Assert.NotNull(EventValidator.Validate(new CalendarEvent(Date, title, url)));
    }

    [Fact]
    public void Rejects_a_missing_event_or_title()
    {
        Assert.NotNull(EventValidator.Validate(null));
        Assert.NotNull(EventValidator.Validate(new CalendarEvent(Date, null!, null)));
    }

    [Fact]
    public void Enforces_the_title_limit()
    {
        Assert.Null(EventValidator.Validate(new CalendarEvent(Date, new string('a', 500), null)));
        Assert.NotNull(EventValidator.Validate(new CalendarEvent(Date, new string('a', 501), null)));
    }
}
