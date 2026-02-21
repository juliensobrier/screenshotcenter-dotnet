using Xunit;

namespace ScreenshotCenter.Tests;

/// <summary>
/// Integration tests — only run when SCREENSHOTCENTER_API_KEY is set.
///
/// Unit tests only (default):
///   dotnet test
///
/// Integration tests against a local instance:
///   SCREENSHOTCENTER_API_KEY=your_key \
///   SCREENSHOTCENTER_BASE_URL=http://localhost:3000/api/v1 \
///   dotnet test --filter "Category=Integration"
/// </summary>
[Trait("Category", "Integration")]
public class IntegrationTests : IAsyncDisposable
{
    private readonly ScreenshotCenterClient? _client;
    private readonly List<long> _createdIds = new();

    public IntegrationTests()
    {
        var apiKey = Environment.GetEnvironmentVariable("SCREENSHOTCENTER_API_KEY");
        if (string.IsNullOrEmpty(apiKey)) return;

        var baseUrl = Environment.GetEnvironmentVariable("SCREENSHOTCENTER_BASE_URL")
            ?? ScreenshotCenterClient.DefaultBaseUrl;
        _client = new ScreenshotCenterClient(apiKey, baseUrl);
    }

    private void SkipIfNotLive()
    {
        if (_client is null) throw new Xunit.SkipException("SCREENSHOTCENTER_API_KEY not set");
    }

    private async Task<Screenshot> CreateAndWaitAsync(string url)
    {
        var shot = await _client!.Screenshot.CreateAsync(url);
        _createdIds.Add(shot.Id);
        return await _client.WaitForAsync(shot.Id, intervalMs: 3000, timeoutMs: 110_000);
    }

    [Fact]
    public async Task Account_Info()
    {
        SkipIfNotLive();
        var info = await _client!.Account.InfoAsync();
        Assert.True(info.Balance >= 0);
    }

    [Fact]
    public async Task Screenshot_CreateAndWait()
    {
        SkipIfNotLive();
        var result = await CreateAndWaitAsync("https://example.com");
        Assert.Equal("finished", result.Status);
        Assert.NotEmpty(result.Url);
    }

    [Fact]
    public async Task Screenshot_Info()
    {
        SkipIfNotLive();
        var shot = await _client!.Screenshot.CreateAsync("https://example.com");
        _createdIds.Add(shot.Id);
        var info = await _client.Screenshot.InfoAsync(shot.Id);
        Assert.Equal(shot.Id, info.Id);
    }

    [Fact]
    public async Task Screenshot_List()
    {
        SkipIfNotLive();
        var list = await _client!.Screenshot.ListAsync(
            new Dictionary<string, string> { ["limit"] = "5" });
        Assert.NotNull(list);
    }

    [Fact]
    public async Task Screenshot_SaveImage()
    {
        SkipIfNotLive();
        var done = await CreateAndWaitAsync("https://example.com");
        var path = Path.GetTempFileName() + ".png";
        await _client!.Screenshot.SaveImageAsync(done.Id, path);
        Assert.True(new FileInfo(path).Length > 0);
        File.Delete(path);
    }

    [Fact]
    public async Task InvalidApiKey_Throws()
    {
        SkipIfNotLive();
        var baseUrl = Environment.GetEnvironmentVariable("SCREENSHOTCENTER_BASE_URL")
            ?? ScreenshotCenterClient.DefaultBaseUrl;
        using var bad = new ScreenshotCenterClient("invalid-key", baseUrl);
        await Assert.ThrowsAsync<ApiException>(() => bad.Account.InfoAsync());
    }

    [Fact]
    public async Task Batch_CreateAndWait()
    {
        // Requires batch worker service to be running
        SkipIfNotLive();
        var batch = await _client!.Batch.CreateAsync(
            new[] { "https://example.com", "https://example.org" }, "us");
        Assert.True(batch.Id > 0);
        var result = await _client.Batch.WaitForAsync(batch.Id, intervalMs: 3000, timeoutMs: 110_000);
        Assert.Contains(result.Status, new[] { "finished", "error" });
    }

    public async ValueTask DisposeAsync()
    {
        if (_client is null) return;
        foreach (var id in _createdIds)
        {
            try { await _client.Screenshot.DeleteAsync(id); } catch { /* best-effort */ }
        }
        _client.Dispose();
    }
}
