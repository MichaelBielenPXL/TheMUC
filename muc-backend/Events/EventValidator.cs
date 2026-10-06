namespace MucBackend.Events;

public static class EventValidator
{
    public const int MaxTitleLength = 500;
    private const int MaxUrlLength = 2000;

    public static string? Validate(CalendarEvent? e)
    {
        if (e is null) return "An event is missing.";
        if (string.IsNullOrWhiteSpace(e.Title)) return "Every event needs a title.";
        if (e.Title.Length > MaxTitleLength) return $"A title can be at most {MaxTitleLength} characters.";
        if (e.Url is null) return null;

        var isWebUrl = e.Url.Length <= MaxUrlLength
            && !e.Url.Any(char.IsWhiteSpace)
            && Uri.TryCreate(e.Url, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https";

        return isWebUrl ? null : $"\"{e.Title}\" has a link that is not an http(s) address.";
    }
}
