namespace MucBackend.History;

public class ImportRecord
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required string SourceText { get; set; }
    public required string Source { get; set; }

    /// <summary>The saved events as JSON. They are only ever read and written together with their import.</summary>
    public required string EventsJson { get; set; }

    // Copied out of the events so the history list does not have to load and parse every import.
    public int EventCount { get; set; }
    public DateOnly FirstDate { get; set; }
    public DateOnly LastDate { get; set; }
}
