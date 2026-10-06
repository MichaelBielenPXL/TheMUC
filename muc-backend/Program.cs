using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MucBackend.Endpoints;
using MucBackend.Extraction;
using MucBackend.History;
using MucBackend.Parsing;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<MessageParser>();
// Without a key the fallback is off and unreadable messages simply give "no events found".
builder.Services.AddSingleton<IEventExtractor>(services =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    var apiKey = configuration["Anthropic:ApiKey"];
    return string.IsNullOrWhiteSpace(apiKey)
        ? new NoEventExtractor()
        : new ClaudeEventExtractor(
            apiKey,
            configuration["Anthropic:Model"] ?? "claude-opus-5-5",
            services.GetRequiredService<ILogger<ClaudeEventExtractor>>());
});

builder.Services.AddDbContext<MucDbContext>((services, options) =>
    options.UseSqlite(services.GetRequiredService<IConfiguration>().GetConnectionString("Muc")));

// One global window, not per client: behind the Cloudflare Tunnel every request has the same address.
// Configuration is read when the options are first used, so tests and containers can override it.
builder.Services.AddRateLimiter(options => options.RejectionStatusCode = StatusCodes.Status429TooManyRequests);
builder.Services.AddOptions<RateLimiterOptions>().Configure<IConfiguration>((options, configuration) =>
    options.AddFixedWindowLimiter(EventEndpoints.ParseRateLimit, limiter =>
    {
        limiter.PermitLimit = configuration.GetValue("RateLimit:ParsePerMinute", 30);
        limiter.Window = TimeSpan.FromMinutes(1);
    }));

var app = builder.Build();

// A single instance owns the database file, so migrating at startup is safe.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<MucDbContext>().Database.Migrate();
}

app.UseRateLimiter();

app.MapDefaultEndpoints();
app.MapEventEndpoints();
app.MapImportEndpoints();

app.Run();

public partial class Program;
