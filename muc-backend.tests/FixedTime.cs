namespace MucBackend.Tests;

public class FixedTime(DateTimeOffset now) : TimeProvider
{
    public static FixedTime October6 { get; } = new(new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));

    public override DateTimeOffset GetUtcNow() => now;
}
