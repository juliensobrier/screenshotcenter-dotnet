using System.Net;
using System.Web;
using Xunit;

namespace ScreenshotCenter.Tests;

public class ClientTests
{
    // ── Fixtures ─────────────────────────────────────────────────────────────

    private static readonly object ShotData = new
    {
        id = 1001L, status = "finished", url = "https://example.com",
        final_url = "https://example.com/", error = (string?)null, cost = 1,
        country = "us", has_html = false, has_pdf = false, has_video = false, shots = 1
    };

    private static readonly object BatchData = new
    {
        id = 2001L, status = "finished", count = 3, processed = 3, failed = 0
    };

    private static readonly object AccountData = new { balance = 500.0, plan = "pro" };

    private (ScreenshotCenterClient client, MockHttpMessageHandler mock) MakeClient()
    {
        var handler = new MockHttpMessageHandler();
        var http    = new HttpClient(handler);
        var client  = new ScreenshotCenterClient("test-key",
            "https://api.screenshotcenter.com/api/v1", http);
        return (client, handler);
    }

    private string? GetQueryParam(MockHttpMessageHandler mock, string name)
    {
        var uri = mock.Requests.Last().RequestUri!;
        return HttpUtility.ParseQueryString(uri.Query)[name];
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_EmptyApiKey_Throws()
        => Assert.Throws<ArgumentException>(() => new ScreenshotCenterClient(""));

    [Fact]
    public void Constructor_WhitespaceApiKey_Throws()
        => Assert.Throws<ArgumentException>(() => new ScreenshotCenterClient("   "));

    [Fact]
    public void Constructor_SetsDefaultBaseUrl()
    {
        Assert.Contains("api.screenshotcenter.com", ScreenshotCenterClient.DefaultBaseUrl);
    }

    [Fact]
    public void Constructor_NamespacesNotNull()
    {
        using var c = new ScreenshotCenterClient("key");
        Assert.NotNull(c.Screenshot);
        Assert.NotNull(c.Batch);
        Assert.NotNull(c.Account);
    }

    // ── Screenshot.Create ─────────────────────────────────────────────────────

    [Fact]
    public async Task Screenshot_Create_ReturnsScreenshot()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(ShotData);
        var result = await c.Screenshot.CreateAsync("https://example.com");
        Assert.Equal(1001L, result.Id);
        Assert.Equal("finished", result.Status);
    }

    [Fact]
    public async Task Screenshot_Create_SendsUrlAndKey()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(ShotData);
        await c.Screenshot.CreateAsync("https://example.com");
        Assert.Equal("test-key", GetQueryParam(mock, "key"));
        Assert.NotNull(GetQueryParam(mock, "url"));
    }

    [Fact]
    public async Task Screenshot_Create_PassesOptionalParams()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(ShotData);
        await c.Screenshot.CreateAsync("https://example.com",
            new Dictionary<string, string> { ["country"] = "fr", ["shots"] = "3" });
        Assert.Equal("fr", GetQueryParam(mock, "country"));
        Assert.Equal("3", GetQueryParam(mock, "shots"));
    }

    [Fact]
    public async Task Screenshot_Create_PassesFutureParams()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(ShotData);
        await c.Screenshot.CreateAsync("https://example.com",
            new Dictionary<string, string> { ["future_param"] = "xyz" });
        Assert.Equal("xyz", GetQueryParam(mock, "future_param"));
    }

    [Fact]
    public async Task Screenshot_Create_EmptyUrl_Throws()
    {
        var (c, _) = MakeClient();
        await Assert.ThrowsAsync<ArgumentException>(() => c.Screenshot.CreateAsync(""));
    }

    [Fact]
    public async Task Screenshot_Create_ApiError_401()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueErrorJson("Unauthorized", HttpStatusCode.Unauthorized);
        var ex = await Assert.ThrowsAsync<ApiException>(() => c.Screenshot.CreateAsync("https://example.com"));
        Assert.Equal(401, ex.Status);
    }

    [Fact]
    public async Task Screenshot_Create_ApiError_HasCode()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueErrorJson("Validation failed", HttpStatusCode.UnprocessableEntity, "INVALID_PARAMS");
        var ex = await Assert.ThrowsAsync<ApiException>(() => c.Screenshot.CreateAsync("bad-url"));
        Assert.Equal("INVALID_PARAMS", ex.Code);
    }

    // ── Screenshot.Info ───────────────────────────────────────────────────────

    [Fact]
    public async Task Screenshot_Info_ReturnsScreenshot()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(ShotData);
        var result = await c.Screenshot.InfoAsync(1001);
        Assert.Equal(1001L, result.Id);
    }

    [Fact]
    public async Task Screenshot_Info_SendsIdParam()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(ShotData);
        await c.Screenshot.InfoAsync(1001);
        Assert.Equal("1001", GetQueryParam(mock, "id"));
    }

    [Fact]
    public async Task Screenshot_Info_404_Throws()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueErrorJson("Not found", HttpStatusCode.NotFound);
        var ex = await Assert.ThrowsAsync<ApiException>(() => c.Screenshot.InfoAsync(999));
        Assert.Equal(404, ex.Status);
    }

    // ── Screenshot.List ───────────────────────────────────────────────────────

    [Fact]
    public async Task Screenshot_List_ReturnsArray()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(new[] { ShotData });
        var result = await c.Screenshot.ListAsync();
        Assert.Single(result);
    }

    [Fact]
    public async Task Screenshot_List_PassesPaginationParams()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(Array.Empty<object>());
        await c.Screenshot.ListAsync(new Dictionary<string, string> { ["limit"] = "5", ["offset"] = "10" });
        Assert.Equal("5", GetQueryParam(mock, "limit"));
        Assert.Equal("10", GetQueryParam(mock, "offset"));
    }

    // ── Screenshot.Search ─────────────────────────────────────────────────────

    [Fact]
    public async Task Screenshot_Search_ReturnsArray()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(new[] { ShotData });
        var result = await c.Screenshot.SearchAsync("example.com");
        Assert.Single(result);
    }

    [Fact]
    public async Task Screenshot_Search_SendsUrlParam()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(Array.Empty<object>());
        await c.Screenshot.SearchAsync("example.com");
        Assert.Equal("example.com", GetQueryParam(mock, "url"));
    }

    [Fact]
    public async Task Screenshot_Search_EmptyUrl_Throws()
    {
        var (c, _) = MakeClient();
        await Assert.ThrowsAsync<ArgumentException>(() => c.Screenshot.SearchAsync(""));
    }

    // ── Screenshot.Thumbnail ──────────────────────────────────────────────────

    [Fact]
    public async Task Screenshot_Thumbnail_ReturnsBytes()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueBinary(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        var result = await c.Screenshot.ThumbnailAsync(1001);
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public async Task Screenshot_Thumbnail_PassesOptions()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueBinary(new byte[] { 0x00 });
        await c.Screenshot.ThumbnailAsync(1001,
            new Dictionary<string, string> { ["shot"] = "2", ["width"] = "400" });
        Assert.Equal("2", GetQueryParam(mock, "shot"));
        Assert.Equal("400", GetQueryParam(mock, "width"));
    }

    // ── Save helpers ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Screenshot_SaveImage_WritesFile()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueBinary(System.Text.Encoding.UTF8.GetBytes("PNG-CONTENT"));
        var path = Path.GetTempFileName();
        await c.Screenshot.SaveImageAsync(1001, path);
        Assert.Equal("PNG-CONTENT", await File.ReadAllTextAsync(path));
        File.Delete(path);
    }

    [Fact]
    public async Task Screenshot_SavePdf_WritesFile()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueBinary(System.Text.Encoding.UTF8.GetBytes("%PDF-1.4"));
        var path = Path.GetTempFileName() + ".pdf";
        await c.Screenshot.SavePdfAsync(1001, path);
        Assert.Equal("%PDF-1.4", await File.ReadAllTextAsync(path));
        File.Delete(path);
    }

    // ── WaitFor ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task WaitFor_ResolvesWhenFinished()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(ShotData);
        var result = await c.WaitForAsync(1001);
        Assert.Equal("finished", result.Status);
    }

    [Fact]
    public async Task WaitFor_PollsUntilFinished()
    {
        var (c, mock) = MakeClient();
        var proc = new { id = 1001L, status = "processing", url = "https://example.com",
            cost = 1, has_html = false, has_pdf = false, has_video = false, shots = 1 };
        mock.EnqueueSuccessJson(proc);
        mock.EnqueueSuccessJson(proc);
        mock.EnqueueSuccessJson(ShotData);
        var result = await c.WaitForAsync(1001, intervalMs: 1, timeoutMs: 30_000);
        Assert.Equal("finished", result.Status);
    }

    [Fact]
    public async Task WaitFor_RaisesScreenshotFailedException()
    {
        var (c, mock) = MakeClient();
        var err = new { id = 1001L, status = "error", url = "https://example.com", error = "DNS failure",
            cost = 1, has_html = false, has_pdf = false, has_video = false, shots = 1 };
        mock.EnqueueSuccessJson(err);
        var ex = await Assert.ThrowsAsync<ScreenshotFailedException>(() => c.WaitForAsync(1001));
        Assert.Equal(1001L, ex.ScreenshotId);
        Assert.Equal("DNS failure", ex.Reason);
    }

    [Fact]
    public async Task WaitFor_RaisesTimeoutException()
    {
        var (c, mock) = MakeClient();
        var proc = new { id = 1001L, status = "processing", url = "https://example.com",
            cost = 1, has_html = false, has_pdf = false, has_video = false, shots = 1 };
        for (int i = 0; i < 20; i++) mock.EnqueueSuccessJson(proc);
        await Assert.ThrowsAsync<TimeoutException>(() => c.WaitForAsync(1001, intervalMs: 1, timeoutMs: 1));
    }

    // ── Batch.Create ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Batch_Create_ReturnssBatch()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(BatchData);
        var result = await c.Batch.CreateAsync(new[] { "https://example.com", "https://example.org" }, "us");
        Assert.Equal(2001L, result.Id);
    }

    [Fact]
    public async Task Batch_Create_UsesPostMethod()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(BatchData);
        await c.Batch.CreateAsync(new[] { "https://example.com" }, "us");
        Assert.Equal("POST", mock.Requests.Last().Method.Method);
    }

    [Fact]
    public async Task Batch_Create_EmptyCountry_Throws()
    {
        var (c, _) = MakeClient();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            c.Batch.CreateAsync(new[] { "https://example.com" }, ""));
    }

    // ── Batch.WaitFor ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Batch_WaitFor_ResolvesOnFinished()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(BatchData);
        var result = await c.Batch.WaitForAsync(2001);
        Assert.Equal("finished", result.Status);
    }

    [Fact]
    public async Task Batch_WaitFor_ResolvesOnError()
    {
        var (c, mock) = MakeClient();
        var err = new { id = 2001L, status = "error", count = 3, processed = 1, failed = 2 };
        mock.EnqueueSuccessJson(err);
        var result = await c.Batch.WaitForAsync(2001);
        Assert.Equal("error", result.Status);
    }

    [Fact]
    public async Task Batch_WaitFor_RaisesTimeout()
    {
        var (c, mock) = MakeClient();
        var proc = new { id = 2001L, status = "processing", count = 3, processed = 0, failed = 0 };
        for (int i = 0; i < 20; i++) mock.EnqueueSuccessJson(proc);
        await Assert.ThrowsAsync<TimeoutException>(() =>
            c.Batch.WaitForAsync(2001, intervalMs: 1, timeoutMs: 1));
    }

    // ── Account.Info ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Account_Info_ReturnsBalance()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(AccountData);
        var result = await c.Account.InfoAsync();
        Assert.Equal(500.0, result.Balance, precision: 1);
    }

    [Fact]
    public async Task Account_Info_SendsKey()
    {
        var (c, mock) = MakeClient();
        mock.EnqueueSuccessJson(AccountData);
        await c.Account.InfoAsync();
        Assert.Equal("test-key", GetQueryParam(mock, "key"));
    }

    // ── Error types ───────────────────────────────────────────────────────────

    [Fact]
    public void ApiException_Properties()
    {
        var e = new ApiException("Bad request", 400, "INVALID_PARAMS");
        Assert.Equal(400, e.Status);
        Assert.Equal("INVALID_PARAMS", e.Code);
        Assert.Equal("Bad request", e.Message);
    }

    [Fact]
    public void TimeoutException_Properties()
    {
        var e = new TimeoutException(1001, 30_000);
        Assert.Equal(1001L, e.ScreenshotId);
        Assert.Equal(30_000, e.TimeoutMs);
        Assert.Contains("1001", e.Message);
    }

    [Fact]
    public void ScreenshotFailedException_Properties()
    {
        var e = new ScreenshotFailedException(1001, "DNS failure");
        Assert.Equal(1001L, e.ScreenshotId);
        Assert.Equal("DNS failure", e.Reason);
        Assert.Contains("DNS failure", e.Message);
    }
}
