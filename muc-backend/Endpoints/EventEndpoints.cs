using MucBackend.Events;
using MucBackend.Extraction;
using MucBackend.Parsing;

namespace MucBackend.Endpoints;

public static class EventEndpoints
{
    public const int MaxTextLength = 20_000;
    public const string ParseRateLimit = "parse";

    public record ParseRequest(string? Text);

    public record ParseResponse(IReadOnlyList<CalendarEvent> Events, IReadOnlyList<string> SkippedLines, string Source);

    public static void MapEventEndpoints(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/events/parse", ParseAsync).RequireRateLimiting(ParseRateLimit);

    private static async Task<IResult> ParseAsync(
        ParseRequest request, MessageParser parser, IEventExtractor extractor, TimeProvider time, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return Results.Problem("Paste a message first.", statusCode: StatusCodes.Status400BadRequest);
        if (request.Text.Length > MaxTextLength)
            return Results.Problem($"The message can be at most {MaxTextLength} characters.", statusCode: StatusCodes.Status400BadRequest);

        var parsed = parser.Parse(request.Text);
        if (parsed.Events.Count > 0)
            return Results.Ok(new ParseResponse(parsed.Events, parsed.SkippedLines, "rules"));

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var extracted = (await extractor.ExtractAsync(request.Text, today, ct))
            .Where(e => EventValidator.Validate(e) is null)
            .ToList();

        return extracted.Count > 0
            ? Results.Ok(new ParseResponse(extracted, [], "llm"))
            : Results.Ok(new ParseResponse([], parsed.SkippedLines, "rules"));
    }
}
