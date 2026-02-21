using System.Net.Http.Headers;
using System.Text;

namespace ScreenshotCenter;

/// <summary>Batch job API methods.</summary>
public sealed class BatchNamespace
{
    private readonly ScreenshotCenterClient _client;

    internal BatchNamespace(ScreenshotCenterClient client)
        => _client = client;

    /// <summary>Create a batch from a list of URLs. <paramref name="country"/> is required.</summary>
    public Task<Batch> CreateAsync(IEnumerable<string> urls, string country,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("country is required", nameof(country));

        var content = string.Join("\n", urls);
        return PostMultipartAsync(content, country, parameters, ct);
    }

    /// <summary>Create a batch from a newline-separated string of URLs.</summary>
    public Task<Batch> CreateFromStringAsync(string urlContent, string country,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("country is required", nameof(country));

        return PostMultipartAsync(urlContent, country, parameters, ct);
    }

    public Task<Batch> InfoAsync(long id, CancellationToken ct = default)
        => _client.GetAsync<Batch>("/batch/info",
            new Dictionary<string, string> { ["id"] = id.ToString() }, ct);

    public Task<Batch[]> ListAsync(IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken ct = default)
        => _client.GetAsync<Batch[]>("/batch/list",
            parameters ?? new Dictionary<string, string>(), ct);

    public Task<byte[]> DownloadAsync(long id, CancellationToken ct = default)
        => _client.GetBytesAsync("/batch/download",
            new Dictionary<string, string> { ["id"] = id.ToString() }, ct);

    public async Task SaveZipAsync(long id, string path, CancellationToken ct = default)
    {
        var data = await DownloadAsync(id, ct).ConfigureAwait(false);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        await File.WriteAllBytesAsync(path, data, ct).ConfigureAwait(false);
    }

    public async Task<Batch> WaitForAsync(long id,
        int intervalMs = 2000, int timeoutMs = 120_000,
        CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            var b = await InfoAsync(id, ct).ConfigureAwait(false);
            if (b.Status is "finished" or "error") return b;
            if (DateTime.UtcNow.AddMilliseconds(intervalMs) > deadline)
                throw new TimeoutException(id, timeoutMs);
            await Task.Delay(intervalMs, ct).ConfigureAwait(false);
        }
    }

    private Task<Batch> PostMultipartAsync(string content, string country,
        IReadOnlyDictionary<string, string>? extra, CancellationToken ct)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(country), "country");
        if (extra != null)
            foreach (var kv in extra)
                form.Add(new StringContent(kv.Value), kv.Key);
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
        form.Add(fileContent, "file", "urls.txt");
        return _client.PostAsync<Batch>("/batch/create", form, null, ct);
    }
}
