using BakeryPos.Core.Security;
using BakeryPos.Core.Stock;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BakeryPos.Web;

public sealed record StockWebOptions(
    string Pin,
    int Port = 5080,
    string Host = "0.0.0.0",
    TimeSpan? RefreshEvery = null,
    TimeSpan? SessionLifetime = null,
    TimeSpan? Lockout = null,
    int MaxFailures = 5,
    TimeProvider? Clock = null);

/// <summary>
/// A small, view-only web server that shows live stock to devices on the shop network.
/// Everything except the login page and the page's own styles needs the PIN first.
/// </summary>
public sealed class StockWebServer : IAsyncDisposable
{
    public const string SessionCookie = "bakerypos_session";

    private readonly WebApplication _app;

    public StockWebServer(LiveStockFeed feed, StockWebOptions options)
    {
        ArgumentNullException.ThrowIfNull(feed);
        ArgumentNullException.ThrowIfNull(options);

        var clock = options.Clock ?? TimeProvider.System;
        var lifetime = options.SessionLifetime ?? TimeSpan.FromHours(8);
        var guard = new LoginGuard(options.Pin, clock, options.MaxFailures, options.Lockout);
        var sessions = new SessionStore(clock, lifetime);
        var refreshMs = (int)(options.RefreshEvery ?? TimeSpan.FromSeconds(5)).TotalMilliseconds;

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseUrls($"http://{options.Host}:{options.Port}");
        builder.WebHost.ConfigureKestrel(k =>
        {
            k.AddServerHeader = false;
            k.Limits.MaxRequestBodySize = 4096;
        });
        builder.Services.ConfigureHttpJsonOptions(o =>
            o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Cache-Control"] = "no-store";
            headers["Content-Security-Policy"] =
                "default-src 'none'; script-src 'self'; style-src 'self'; connect-src 'self'; " +
                "form-action 'self'; frame-ancestors 'none'; base-uri 'none'";
            await next();
        });

        bool SignedIn(HttpContext c) => sessions.IsValid(c.Request.Cookies[SessionCookie]);
        string ClientId(HttpContext c) => c.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        app.MapGet("/app.css", () => Results.Text(WebAssets.Css, "text/css; charset=utf-8"));
        app.MapGet("/app.js", () => Results.Text(WebAssets.Script, "text/javascript; charset=utf-8"));

        app.MapGet("/", (HttpContext c) =>
            SignedIn(c)
                ? Results.Content(WebAssets.StockPage(refreshMs), "text/html; charset=utf-8")
                : Results.Redirect("/login"));

        app.MapGet("/login", (HttpContext c) =>
            SignedIn(c)
                ? Results.Redirect("/")
                : Results.Content(WebAssets.LoginPage(null), "text/html; charset=utf-8"));

        app.MapPost("/login", async (HttpContext c) =>
        {
            var form = await c.Request.ReadFormAsync();
            var result = guard.TryLogin(ClientId(c), form["pin"].ToString());

            switch (result)
            {
                case LoginResult.Success:
                    var token = sessions.Create();
                    c.Response.Cookies.Append(SessionCookie, token, new CookieOptions
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Strict,
                        Path = "/",
                        MaxAge = lifetime,
                        IsEssential = true,
                    });
                    return Results.Redirect("/");

                case LoginResult.LockedOut:
                    var minutes = Math.Max(1, (int)Math.Ceiling(guard.RemainingLockout(ClientId(c)).TotalMinutes));
                    return Results.Text(
                        WebAssets.LoginPage($"Too many wrong tries. Try again in {minutes} minute(s)."),
                        "text/html; charset=utf-8",
                        statusCode: StatusCodes.Status429TooManyRequests);

                default:
                    return Results.Text(
                        WebAssets.LoginPage("Wrong PIN."),
                        "text/html; charset=utf-8",
                        statusCode: StatusCodes.Status401Unauthorized);
            }
        });

        app.MapPost("/logout", (HttpContext c) =>
        {
            sessions.Revoke(c.Request.Cookies[SessionCookie]);
            c.Response.Cookies.Delete(SessionCookie, new CookieOptions { Path = "/" });
            return Results.Redirect("/login");
        });

        app.MapGet("/api/stock", (HttpContext c) =>
        {
            if (!SignedIn(c))
            {
                return Results.StatusCode(StatusCodes.Status401Unauthorized);
            }

            var report = feed.Current;
            return report is null
                ? Results.StatusCode(StatusCodes.Status503ServiceUnavailable)
                : Results.Json(report);
        });

        _app = app;
    }

    /// <summary>The address the server is listening on, after it has started.</summary>
    public string? Address =>
        _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();

    public Task StartAsync(CancellationToken cancellationToken = default) => _app.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default) => _app.StopAsync(cancellationToken);

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}
