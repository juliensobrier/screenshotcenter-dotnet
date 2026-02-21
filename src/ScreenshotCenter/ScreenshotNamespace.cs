namespace ScreenshotCenter;

/// <summary>Screenshot-related API methods.</summary>
public sealed class ScreenshotNamespace
{
    private readonly ScreenshotCenterClient _client;

    internal ScreenshotNamespace(ScreenshotCenterClient client)
        => _client = client;

    /// <summary>Create a new screenshot. <paramref name="url"/> is required.</summary>
    public Task<Screenshot> CreateAsync(string url,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("url is required", nameof(url));

        var p = new Dictionary<string, string>(parameters ?? new Dictionary<string, string>())
            { ["url"] = url };
        return _client.GetAsync<Screenshot>("/screenshot/create", p, ct);
    }

    public Task<Screenshot> InfoAsync(long id, CancellationToken ct = default)
        => _client.GetAsync<Screenshot>("/screenshot/info",
            new Dictionary<string, string> { ["id"] = id.ToString() }, ct);

    public Task<Screenshot[]> ListAsync(IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default)
        => _client.GetAsync<Screenshot[]>("/screenshot/list",
            parameters ?? new Dictionary<string, string>(), ct);

    public Task<Screenshot[]> SearchAsync(string url,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("url is required", nameof(url));

        var p = new Dictionary<string, string>(parameters ?? new Dictionary<string, string>())
            { ["url"] = url };
        return _client.GetAsync<Screenshot[]>("/screenshot/search", p, ct);
    }

    public Task<byte[]> ThumbnailAsync(long id,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default)
    {
        var p = new Dictionary<string, string>(parameters ?? new Dictionary<string, string>())
            { ["id"] = id.ToString() };
        return _client.GetBytesAsync("/screenshot/thumbnail", p, ct);
    }

    public Task<byte[]> HtmlAsync(long id, CancellationToken ct = default)
        => _client.GetBytesAsync("/screenshot/html",
            new Dictionary<string, string> { ["id"] = id.ToString() }, ct);

    public Task<byte[]> PdfAsync(long id, CancellationToken ct = default)
        => _client.GetBytesAsync("/screenshot/pdf",
            new Dictionary<string, string> { ["id"] = id.ToString() }, ct);

    public Task<byte[]> VideoAsync(long id, CancellationToken ct = default)
        => _client.GetBytesAsync("/screenshot/video",
            new Dictionary<string, string> { ["id"] = id.ToString() }, ct);

    public Task DeleteAsync(long id, string data = "all", CancellationToken ct = default)
        => _client.GetAsync<object>("/screenshot/delete",
            new Dictionary<string, string> { ["id"] = id.ToString(), ["data"] = data }, ct);

    // ── File-save helpers ─────────────────────────────────────────────────────

    public async Task SaveImageAsync(long id, string path,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default)
        => await WriteFileAsync(path, await ThumbnailAsync(id, parameters, ct).ConfigureAwait(false), ct)
            .ConfigureAwait(false);

    public async Task SavePdfAsync(long id, string path, CancellationToken ct = default)
        => await WriteFileAsync(path, await PdfAsync(id, ct).ConfigureAwait(false), ct).ConfigureAwait(false);

    public async Task SaveHtmlAsync(long id, string path, CancellationToken ct = default)
        => await WriteFileAsync(path, await HtmlAsync(id, ct).ConfigureAwait(false), ct).ConfigureAwait(false);

    public async Task SaveVideoAsync(long id, string path, CancellationToken ct = default)
        => await WriteFileAsync(path, await VideoAsync(id, ct).ConfigureAwait(false), ct).ConfigureAwait(false);

    private static async Task WriteFileAsync(string path, byte[] data, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        await File.WriteAllBytesAsync(path, data, ct).ConfigureAwait(false);
    }
}
