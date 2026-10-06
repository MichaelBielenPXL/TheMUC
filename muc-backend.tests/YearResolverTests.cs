using MucBackend.Parsing;

namespace MucBackend.Tests;

public class YearResolverTests
{
    private static readonly DateOnly Today = new(2026, 10, 6);

    [Fact]
    public void Uses_an_explicit_year_as_is()
    {
        var line = new DateLine(28, 9, 2019, DayOfWeek.Monday);

        Assert.Equal(new DateOnly(2019, 9, 28), YearResolver.Resolve(line, Today));
    }

    [Fact]
    public void Picks_the_year_in_which_the_weekday_matches()
    {
        // 28 September is a Monday in 2026 and a Saturday in 2024.
        Assert.Equal(new DateOnly(2026, 9, 28), YearResolver.Resolve(new DateLine(28, 9, null, DayOfWeek.Monday), Today));
        Assert.Equal(new DateOnly(2024, 9, 28), YearResolver.Resolve(new DateLine(28, 9, null, DayOfWeek.Saturday), Today));
    }

    [Fact]
    public void Without_a_weekday_picks_the_nearest_occurrence()
    {
        Assert.Equal(new DateOnly(2026, 9, 28), YearResolver.Resolve(new DateLine(28, 9, null, null), Today));
    }

    [Fact]
    public void Crosses_into_next_year_in_late_december()
    {
        var today = new DateOnly(2026, 12, 30);

        Assert.Equal(new DateOnly(2027, 1, 4), YearResolver.Resolve(new DateLine(4, 1, null, DayOfWeek.Monday), today));
        Assert.Equal(new DateOnly(2027, 1, 4), YearResolver.Resolve(new DateLine(4, 1, null, null), today));
    }

    [Fact]
    public void Crosses_back_into_last_year_in_early_january()
    {
        var today = new DateOnly(2027, 1, 2);

        Assert.Equal(new DateOnly(2026, 12, 31), YearResolver.Resolve(new DateLine(31, 12, null, DayOfWeek.Thursday), today));
        Assert.Equal(new DateOnly(2026, 12, 31), YearResolver.Resolve(new DateLine(31, 12, null, null), today));
    }

    [Fact]
    public void Falls_back_to_the_nearest_year_when_the_weekday_never_matches()
    {
        // 28 September is not a Wednesday in any year from 2023 to 2029.
        Assert.Equal(new DateOnly(2026, 9, 28), YearResolver.Resolve(new DateLine(28, 9, null, DayOfWeek.Wednesday), Today));
    }

    [Fact]
    public void Resolves_29_february_to_the_nearest_leap_year()
    {
        Assert.Equal(new DateOnly(2028, 2, 29), YearResolver.Resolve(new DateLine(29, 2, null, null), Today));
    }
}
