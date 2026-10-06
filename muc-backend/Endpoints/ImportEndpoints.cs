using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MucBackend.Calendar;
using MucBackend.Events;
using MucBackend.History;

namespace MucBackend.Endpoints;

public static class ImportEndpoints
{
    public const int MaxEvents = 200;
    public const int MaxListed = 100;

    public record SaveImportRequest(string? SourceText, string? Source, List<CalendarEvent?>? Events);

    public record SavedImport(int Id, DateTimeOffset CreatedAt, int EventCount);

    public record ImportSummary(int Id, DateTimeOffset CreatedAt, int EventCount, DateOnly FirstDate, DateOnly LastDate, string Source);

    public record ImportDetail(int Id, DateTimeOffset CreatedAt, string Source, string SourceText, IReadOnlyList<CalendarEvent> Events);

    public static void MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        var imports = app.MapGroup("/api/imports");
        imports.MapPost("/", SaveAsync);
        imports.MapGet("/", ListAsync);
        imports.MapGet("/{id:int}", GetAsync);
        imports.MapGet("/{id:int}/ics", DownloadAsync);
        imports.MapDelete("/{id:int}", DeleteAsync);
    }

    private static async Task<IResult> SaveAsync(SaveImportRequest request, MucDbContext db, TimeProvider time, CancellationToken ct)
    {
        if (Validate(request) is { } error) return Results.Problem(error, statusCode: StatusCodes.Status400BadRequest);

        var events = request.Events!.Select(e => e! with { Title = e.Title.Trim() }).OrderBy(e => e.Date).ToList();
        var record = new ImportRecord
        {
            CreatedAt = time.GetUtcNow(),
            SourceText = request.SourceText ?? "",
            Source = request.Source!,
            EventsJson = JsonSerializer.Serialize(events, JsonSerializerOptions.Web),
            EventCount = events.Count,
            FirstDate = events[0].Date,
            LastDate = events[^1].Date,
        };
        db.Imports.Add(record);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/imports/{record.Id}", new SavedImport(record.Id, record.CreatedAt, record.EventCount));
    }

    private static string? Validate(SaveImportRequest request)
    {
        if (request.Source is not ("rules" or "llm")) return "The source must be \"rules\" or \"llm\".";
        if (request.SourceText?.Length > EventEndpoints.MaxTextLength)
            return $"The message can be at most {EventEndpoints.MaxTextLength} characters.";
        if (request.Events is null or { Count: 0 }) return "There are no events to save.";
        if (request.Events.Count > MaxEvents) return $"An import can hold at most {MaxEvents} events.";
        return request.Events.Select(EventValidator.Validate).FirstOrDefault(error => error is not null);
    }

    // Ordered by key, not by CreatedAt: SQLite cannot sort DateTimeOffset values.
    private static async Task<IResult> ListAsync(MucDbContext db, CancellationToken ct) =>
        Results.Ok(await db.Imports
            .OrderByDescending(i => i.Id)
            .Take(MaxListed)
            .Select(i => new ImportSummary(i.Id, i.CreatedAt, i.EventCount, i.FirstDate, i.LastDate, i.Source))
            .ToListAsync(ct));

    private static async Task<IResult> GetAsync(int id, MucDbContext db, CancellationToken ct) =>
        await db.Imports.FindAsync([id], ct) is { } record
            ? Results.Ok(new ImportDetail(record.Id, record.CreatedAt, record.Source, record.SourceText, Events(record)))
            : Results.NotFound();

    private static async Task<IResult> DownloadAsync(int id, MucDbContext db, TimeProvider time, CancellationToken ct)
    {
        if (await db.Imports.FindAsync([id], ct) is not { } record) return Results.NotFound();

        var ics = IcsWriter.Write(Events(record), time.GetUtcNow());
        return Results.File(
            Encoding.UTF8.GetBytes(ics), "text/calendar; charset=utf-8", $"mercurius-{record.FirstDate:yyyy-MM-dd}.ics");
    }

    private static async Task<IResult> DeleteAsync(int id, MucDbContext db, CancellationToken ct) =>
        await db.Imports.Where(i => i.Id == id).ExecuteDeleteAsync(ct) > 0 ? Results.NoContent() : Results.NotFound();

    private static List<CalendarEvent> Events(ImportRecord record) =>
        JsonSerializer.Deserialize<List<CalendarEvent>>(record.EventsJson, JsonSerializerOptions.Web) ?? [];
}
