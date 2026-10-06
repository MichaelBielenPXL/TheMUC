using MucBackend.Events;

namespace MucBackend.Extraction;

/// <summary>Finds events in text the rule parser could not read. Never throws; returns an empty list on failure.</summary>
public interface IEventExtractor
{
    Task<IReadOnlyList<CalendarEvent>> ExtractAsync(string text, DateOnly today, CancellationToken ct);
}

public class NoEventExtractor : IEventExtractor
{
    public Task<IReadOnlyList<CalendarEvent>> ExtractAsync(string text, DateOnly today, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CalendarEvent>>([]);
}
