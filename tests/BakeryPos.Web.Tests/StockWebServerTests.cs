using System.Net;
using BakeryPos.Core.Catalogue;
using BakeryPos.Core.Stock;
using BakeryPos.Web;

namespace BakeryPos.Web.Tests;

public sealed class StockWebServerTests : IAsyncLifetime
{
    private const string Pin = "482913";

    private StockWebServer _server = null!;
    private HttpClient _http = null!;

    public async Task InitializeAsync()
    {
        var pie = new Product("Plain Pies", Category.Pies, 550, 20, 5);
        var catalogue = new ProductCatalogue([pie]);
        var board = new StockBoard(catalogue);
        board.SetBooth(pie.Id, 3);

        var feed = new LiveStockFeed();
        feed.Publish(StockReport.Build(catalogue, board, DateTimeOffset.UtcNow));

        _server = new StockWebServer(feed, new StockWebOptions(Pin, Port: 0, Host: "127.0.0.1", MaxFailures: 3));
        await _server.StartAsync();

        var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
        _http = new HttpClient(handler) { BaseAddress = new Uri(_server.Address!) };
    }

    public async Task DisposeAsync()
    {
        _http.Dispose();
        await _server.DisposeAsync();
    }

    private Task<HttpResponseMessage> LoginAsync(string pin) =>
        _http.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string> { ["pin"] = pin }));

    private static string CookieFrom(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").First().Split(';')[0];

    private async Task<string> SignInAsync()
    {
        var response = await LoginAsync(Pin);
        return CookieFrom(response);
    }

    private static HttpRequestMessage WithCookie(string path, string cookie, HttpMethod? method = null)
    {
        var request = new HttpRequestMessage(method ?? HttpMethod.Get, path);
        request.Headers.Add("Cookie", cookie);
        return request;
    }

    [Fact]
    public async Task StockData_NeedsSignIn()
    {
        var response = await _http.GetAsync("/api/stock");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TheStockPage_SendsStrangersToTheLoginPage()
    {
        var response = await _http.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task WrongPin_IsRefused_AndGivesNoCookie()
    {
        var response = await LoginAsync("000000");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task RightPin_SignsIn_AndStockDataIsServed()
    {
        var cookie = await SignInAsync();

        var response = await _http.SendAsync(WithCookie("/api/stock", cookie));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Plain Pies", body);
        Assert.Contains("\"status\":\"low\"", body);
    }

    [Fact]
    public async Task TheSessionCookie_IsHttpOnlyAndStrict()
    {
        var response = await LoginAsync(Pin);
        var header = response.Headers.GetValues("Set-Cookie").First().ToLowerInvariant();

        Assert.Contains("httponly", header);
        Assert.Contains("samesite=strict", header);
    }

    [Fact]
    public async Task AfterTooManyWrongPins_EvenTheRightPinIsRefused()
    {
        await LoginAsync("1");
        await LoginAsync("2");
        var locked = await LoginAsync("3");
        var rightPin = await LoginAsync(Pin);

        Assert.Equal((HttpStatusCode)429, locked.StatusCode);
        Assert.Equal((HttpStatusCode)429, rightPin.StatusCode);
        Assert.False(rightPin.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var cookie = await SignInAsync();

        await _http.SendAsync(WithCookie("/logout", cookie, HttpMethod.Post));
        var after = await _http.SendAsync(WithCookie("/api/stock", cookie));

        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task Responses_CarrySecurityHeaders()
    {
        var response = await _http.GetAsync("/login");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Equal("no-store", response.Headers.GetValues("Cache-Control").Single());
    }

    [Fact]
    public async Task ThePostRequests_AreNotAcceptedAsGets()
    {
        var response = await _http.GetAsync("/logout");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
