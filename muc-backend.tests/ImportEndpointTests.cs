using System.Net;
using System.Net.Http.Json;
using System.Text;
using MucBackend.Events;

namespace MucBackend.Tests;

public class ImportEndpointTests
{
    private record SavedImport(int Id, DateTimeOffset CreatedAt, int EventCount);

    private record ImportSummary(int Id, DateTimeOffset CreatedAt, int EventCount, DateOnly FirstDate, DateOnly LastDate, string Source);

    private record ImportDetail(int Id, DateTimeOffset CreatedAt, string Source, string SourceText, List<CalendarEvent> Events);

    private static readonly CalendarEvent Doop = new(new DateOnly(2026, 10, 8), "Doop - ASG", "https://example.com/doop");
    private static readonly CalendarEvent Fakbar = new(new DateOnly(2026, 9, 28), "Fakbar - Dionysia", null);

    private static Task<HttpResponseMessage> Save(HttpClient client, params CalendarEvent[] events) =>
        client.PostAsJsonAsync("/api/imports", new { sourceText = "pasted text", source = "rules", events });

    private static async Task<int> SaveAndGetId(HttpClient client, params CalendarEvent[] events) =>
        (await (await Save(client, events)).Content.ReadFromJsonAsync<SavedImport>())!.Id;

    [Fact]
    public async Task Saves_an_import()
    {
        using var app = new MucApp();

        var response = await Save(app.CreateClient(), Doop, Fakbar);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<SavedImport>();
        Assert.True(saved!.Id > 0);
        Assert.Equal(2, saved.EventCount);
        Assert.Equal(FixedTime.October6.GetUtcNow(), saved.CreatedAt);
    }

    [Fact]
    public async Task Returns_a_saved_import_with_events_sorted_by_date()
    {
        using var app = new MucApp();
        var client = app.CreateClient();
        var id = await SaveAndGetId(client, Doop, Fakbar);

        var detail = await client.GetFromJsonAsync<ImportDetail>($"/api/imports/{id}");

        Assert.Equal("pasted text", detail!.SourceText);
        Assert.Equal("rules", detail.Source);
        Assert.Equal([Fakbar, Doop], detail.Events);
    }

    [Fact]
    public async Task Lists_imports_newest_first()
    {
        using var app = new MucApp();
        var client = app.CreateClient();
        var first = await SaveAndGetId(client, Fakbar);
        var second = await SaveAndGetId(client, Doop, Fakbar);

        var list = (await client.GetFromJsonAsync<List<ImportSummary>>("/api/imports"))!;

        Assert.Equal([second, first], list.Select(i => i.Id));
        Assert.Equal(2, list[0].EventCount);
        Assert.Equal(new DateOnly(2026, 9, 28), list[0].FirstDate);
        Assert.Equal(new DateOnly(2026, 10, 8), list[0].LastDate);
        Assert.Equal("rules", list[0].Source);
    }

    [Fact]
    public async Task Lists_nothing_when_history_is_empty()
    {
        using var app = new MucApp();

        Assert.Empty((await app.CreateClient().GetFromJsonAsync<List<ImportSummary>>("/api/imports"))!);
    }

    [Fact]
    public async Task Downloads_an_import_as_ics()
    {
        using var app = new MucApp();
        var client = app.CreateClient();
        var id = await SaveAndGetId(client, Doop, Fakbar);

        var response = await client.GetAsync($"/api/imports/{id}/ics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/calendar", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
        Assert.Contains("mercurius-2026-09-28.ics", response.Content.Headers.ContentDisposition.ToString());
        var ics = await response.Content.ReadAsStringAsync();
        Assert.Contains("SUMMARY:Fakbar - Dionysia\r\n", ics);
        Assert.Contains("SUMMARY:Doop - ASG\r\n", ics);
        Assert.Contains("DTSTAMP:20261006T120000Z\r\n", ics);
    }

    [Fact]
    public async Task Deletes_an_import()
    {
        using var app = new MucApp();
        var client = app.CreateClient();
        var id = await SaveAndGetId(client, Fakbar);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/imports/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/imports/{id}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<ImportSummary>>("/api/imports"))!);
    }

    [Fact]
    public async Task Unknown_ids_are_not_found()
    {
        using var app = new MucApp();
        var client = app.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/imports/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/imports/999/ics")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/api/imports/999")).StatusCode);
    }

    [Fact]
    public async Task Rejects_an_import_without_events()
    {
        using var app = new MucApp();
        var client = app.CreateClient();

        Assert.Equal(HttpStatusCode.BadRequest, (await Save(client)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/imports", new { sourceText = "x", source = "rules" })).StatusCode);
    }

    [Fact]
    public async Task Rejects_an_import_with_an_invalid_event()
    {
        using var app = new MucApp();
        var client = app.CreateClient();

        Assert.Equal(HttpStatusCode.BadRequest, (await Save(client, Fakbar with { Title = " " })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Save(client, Fakbar with { Url = "javascript:alert(1)" })).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<List<ImportSummary>>("/api/imports"))!);
    }

    [Fact]
    public async Task Rejects_an_event_with_an_impossible_date()
    {
        using var app = new MucApp();
        var body = """{"sourceText":"x","source":"rules","events":[{"date":"2026-02-30","title":"Fakbar","url":null}]}""";

        var response = await app.CreateClient().PostAsync("/api/imports", new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_more_than_200_events_and_accepts_200()
    {
        using var app = new MucApp();
        var client = app.CreateClient();
        CalendarEvent[] Many(int count) =>
            Enumerable.Range(0, count).Select(i => new CalendarEvent(new DateOnly(2026, 1, 1).AddDays(i), $"Event {i}", null)).ToArray();

        Assert.Equal(HttpStatusCode.Created, (await Save(client, Many(200))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Save(client, Many(201))).StatusCode);
    }

    [Fact]
    public async Task Rejects_an_unknown_source_and_oversized_text()
    {
        using var app = new MucApp();
        var client = app.CreateClient();
        CalendarEvent[] events = [Fakbar];

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/imports", new { sourceText = "x", source = "magic", events })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/imports", new { sourceText = new string('a', 20_001), source = "rules", events })).StatusCode);
    }

    [Fact]
    public async Task History_survives_an_application_restart()
    {
        using var app = new MucApp();
        var id = await SaveAndGetId(app.CreateClient(), Fakbar);

        // A second host on the same database file, as after a container restart.
        using var restarted = app.WithWebHostBuilder(_ => { });
        var detail = await restarted.CreateClient().GetFromJsonAsync<ImportDetail>($"/api/imports/{id}");

        Assert.Equal([Fakbar], detail!.Events);
    }
}
