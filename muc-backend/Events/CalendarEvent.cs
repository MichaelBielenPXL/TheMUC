namespace MucBackend.Events;

public record CalendarEvent(DateOnly Date, string Title, string? Url);
