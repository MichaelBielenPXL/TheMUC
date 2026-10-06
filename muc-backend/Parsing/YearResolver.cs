namespace MucBackend.Parsing;

public static class YearResolver
{
    public static DateOnly? Resolve(DateLine line, DateOnly today)
    {
        if (line.Year is { } year) return new DateOnly(year, line.Month, line.Day);

        var candidates = Enumerable.Range(today.Year - 3, 7)
            .Where(y => line.Day <= DateTime.DaysInMonth(y, line.Month))
            .Select(y => new DateOnly(y, line.Month, line.Day))
            .ToList();

        var matchingWeekday = candidates.Where(date => date.DayOfWeek == line.Weekday).ToList();

        return (matchingWeekday.Count > 0 ? matchingWeekday : candidates)
            .OrderBy(date => Math.Abs(date.DayNumber - today.DayNumber))
            .Cast<DateOnly?>()
            .FirstOrDefault();
    }
}
