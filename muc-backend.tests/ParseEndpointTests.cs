using System.Net;
using System.Net.Http.Json;
using MucBackend.Events;

namespace MucBackend.Tests;

public class ParseEndpointTests
{
    private record ParseResponse(List<CalendarEvent> Events, List<string> SkippedLines, string Source);

    private static Task<HttpResponseMessage> Parse(HttpClient client, string? text) =>
        client.PostAsJsonAsync("/api/events/parse", new { text });

    [Fact]
    public async Task Returns_events_found_by_the_rules()
    {
        using var app = new MucApp();

        var response = await Parse(app.CreateClient(), "Intro\n\nMaandag 28 september\nFakbar - Dionysia\nhttps://example.com/a");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ParseResponse>();
        Assert.Equal([new CalendarEvent(new DateOnly(2026, 9, 28), "Fakbar - Dionysia", "https://example.com/a")], body!.Events);
        Assert.Equal(["Intro"], body.SkippedLines);
        Assert.Equal("rules", body.Source);
        Assert.Equal(0, app.Extractor.Calls);
    }

    [Fact]
    public async Task Serialises_dates_as_iso_and_properties_as_camel_case()
    {
        using var app = new MucApp();

        var json = await (await Parse(app.CreateClient(), "Maandag 28 september\nFakbar")).Content.ReadAsStringAsync();

        Assert.Contains("\"date\":\"2026-09-28\"", json);
        Assert.Contains("\"skippedLines\":[]", json);
        Assert.Contains("\"url\":null", json);
    }

    [Fact]
    public async Task Falls_back_to_the_extractor_when_the_rules_find_nothing()
    {
        using var app = new MucApp();
        app.Extractor.Result.Add(new CalendarEvent(new DateOnly(2026, 10, 9), "Cantus", null));

        var body = await (await Parse(app.CreateClient(), "volgende week vrijdag cantus")).Content.ReadFromJsonAsync<ParseResponse>();

        Assert.Equal("llm", body!.Source);
        Assert.Equal([new CalendarEvent(new DateOnly(2026, 10, 9), "Cantus", null)], body.Events);
        Assert.Equal(1, app.Extractor.Calls);
    }

    [Fact]
    public async Task Drops_invalid_events_from_the_extractor()
    {
        using var app = new MucApp();
        app.Extractor.Result.Add(new CalendarEvent(new DateOnly(2026, 10, 9), "Cantus", "javascript:alert(1)"));
        app.Extractor.Result.Add(new CalendarEvent(new DateOnly(2026, 10, 9), "  ", null));

        var body = await (await Parse(app.CreateClient(), "volgende week vrijdag cantus")).Content.ReadFromJsonAsync<ParseResponse>();

        Assert.Empty(body!.Events);
        Assert.Equal("rules", body.Source);
        Assert.Equal(["volgende week vrijdag cantus"], body.SkippedLines);
    }

    [Fact]
    public async Task Reports_no_events_when_the_extractor_finds_nothing_either()
    {
        using var app = new MucApp();

        var body = await (await Parse(app.CreateClient(), "Hallo allemaal")).Content.ReadFromJsonAsync<ParseResponse>();

        Assert.Empty(body!.Events);
        Assert.Equal("rules", body.Source);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \n ")]
    public async Task Rejects_blank_text(string? text)
    {
        using var app = new MucApp();

        Assert.Equal(HttpStatusCode.BadRequest, (await Parse(app.CreateClient(), text)).StatusCode);
    }

    [Fact]
    public async Task Rejects_text_over_the_limit_and_accepts_text_at_the_limit()
    {
        using var app = new MucApp();
        var client = app.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await Parse(client, new string('a', 20_000))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Parse(client, new string('a', 20_001))).StatusCode);
    }

    [Fact]
    public async Task Rejects_requests_over_the_rate_limit()
    {
        using var app = new MucApp { ParseLimit = 2 };
        var client = app.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await Parse(client, "Maandag 28 september\nFakbar")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Parse(client, "Maandag 28 september\nFakbar")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await Parse(client, "Maandag 28 september\nFakbar")).StatusCode);
    }
}
