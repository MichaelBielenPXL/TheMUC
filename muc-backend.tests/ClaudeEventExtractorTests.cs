using MucBackend.Events;
using MucBackend.Extraction;

namespace MucBackend.Tests;

public class ClaudeEventExtractorTests
{
    [Fact]
    public void Reads_events_from_the_model_output()
    {
        var json = """
            {"events":[
              {"date":"2026-10-09","title":"Cantus","url":null},
              {"date":"2026-10-08","title":"Doop - ASG","url":"https://example.com/a"}
            ]}
            """;

        Assert.Equal(
            [
                new CalendarEvent(new DateOnly(2026, 10, 9), "Cantus", null),
                new CalendarEvent(new DateOnly(2026, 10, 8), "Doop - ASG", "https://example.com/a"),
            ],
            ClaudeEventExtractor.ReadEvents(json));
    }

    [Fact]
    public void Skips_entries_without_a_usable_date_or_title()
    {
        var json = """
            {"events":[
              {"date":"next friday","title":"Cantus","url":null},
              {"date":"2026-02-30","title":"Cantus","url":null},
              {"date":"2026-10-09","title":null,"url":null},
              {"date":"2026-10-09","title":"Doop","url":null}
            ]}
            """;

        Assert.Equal([new CalendarEvent(new DateOnly(2026, 10, 9), "Doop", null)], ClaudeEventExtractor.ReadEvents(json));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"events\":null}")]
    [InlineData("{\"events\":\"none\"}")]
    [InlineData("[]")]
    public void Returns_nothing_for_output_it_cannot_read(string json)
    {
        Assert.Empty(ClaudeEventExtractor.ReadEvents(json));
    }
}
