using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MucBackend.Events;
using MucBackend.Extraction;

namespace MucBackend.Tests;

public class FakeExtractor : IEventExtractor
{
    public List<CalendarEvent> Result { get; } = [];
    public int Calls { get; private set; }

    public Task<IReadOnlyList<CalendarEvent>> ExtractAsync(string text, DateOnly today, CancellationToken ct)
    {
        Calls++;
        return Task.FromResult<IReadOnlyList<CalendarEvent>>(Result.ToList());
    }
}

public class MucApp : WebApplicationFactory<Program>
{
    public FakeExtractor Extractor { get; } = new();
    public int ParseLimit { get; init; } = 10_000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimit:ParsePerMinute", ParseLimit.ToString());
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(FixedTime.October6);
            services.AddSingleton<IEventExtractor>(Extractor);
        });
    }
}
