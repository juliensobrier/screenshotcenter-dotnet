using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace ScreenshotCenter;

/// <summary>
/// Official .NET client for the ScreenshotCenter API.
/// <example>
/// <code>
/// var client = new ScreenshotCenterClient("your_api_key");
/// var shot   = await client.Screenshot.CreateAsync("https://example.com");
/// var result = await client.WaitForAsync(shot.Id);
/// Console.WriteLine(result.Status); // "finished"
/// </code>
/// </example>
/// </summary>
public sealed class ScreenshotCenterClient : IDisposable
{
    public const string DefaultBaseUrl = "https://api.screenshotcenter.com/api/v1";

    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public ScreenshotNamespace Screenshot { get; }
    public BatchNamespace Batch { get; }
    public AccountNamespace Account { get; }

    public ScreenshotCenterClient(string apiKey, string? baseUrl = null, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("apiKey is required", nameof(apiKey));

        _apiKey         = apiKey;
        _baseUrl        = (baseUrl ?? DefaultBaseUrl).TrimEnd('/');
        _ownsHttpClient = httpClient is null;
        _httpClient     = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        Screenshot = new ScreenshotNamespace(this);
        Batch      = new BatchNamespace(this);
        Account    = new AccountNamespace(this);
    }

    /// <summary>Poll a screenshot until it reaches "finished" or "error".</summary>
    public async Task<Screenshot> WaitForAsync(long id,
        int intervalMs = 2000, int timeoutMs = 120_000,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            var s = await Screenshot.InfoAsync(id, cancellationToken).ConfigureAwait(false);
            if (s.Status == "finished") return s;
            if (s.Status == "error")
                throw new ScreenshotFailedException(id, s.Error);
            if (DateTime.UtcNow.AddMilliseconds(intervalMs) > deadline)
                throw new TimeoutException(id, timeoutMs);
            await Task.Delay(intervalMs, cancellationToken).ConfigureAwait(false);
        }
    }

    // ── Internal ────────────────────────────────────────────────────────────────

    internal async Task<T> GetAsync<T>(string endpoint, IReadOnlyDictionary<string, string> parameters,
        CancellationToken ct = default)
    {
        var url = BuildUrl(endpoint, parameters);
        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
        return await ParseJsonResponseAsync<T>(response, ct).ConfigureAwait(false);
    }

    internal async Task<byte[]> GetBytesAsync(string endpoint, IReadOnlyDictionary<string, string> parameters,
        CancellationToken ct = default)
    {
        var url = BuildUrl(endpoint, parameters);
        var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            await ThrowApiErrorAsync(response, ct).ConfigureAwait(false);
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }

    internal async Task<T> PostAsync<T>(string endpoint, HttpContent content,
        IReadOnlyDictionary<string, string>? parameters = null, CancellationToken ct = default)
    {
        var url = BuildUrl(endpoint, parameters);
        var response = await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);
        return await ParseJsonResponseAsync<T>(response, ct).ConfigureAwait(false);
    }

    private string BuildUrl(string endpoint, IReadOnlyDictionary<string, string>? parameters)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["key"] = _apiKey;
        if (parameters != null)
            foreach (var kv in parameters)
                query[kv.Key] = kv.Value;
        return $"{_baseUrl}{endpoint}?{query}";
    }

    private static async Task<T> ParseJsonResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var node = JsonNode.Parse(body);

        if (!response.IsSuccessStatusCode)
        {
            var msg    = node?["error"]?.GetValue<string>() ?? $"HTTP {(int)response.StatusCode}";
            var code   = node?["code"]?.GetValue<string>();
            var fields = ParseFields(node?["fields"]);
            throw new ApiException(msg, (int)response.StatusCode, code, fields);
        }

        if (node?["success"] is JsonNode successNode)
        {
            if (!successNode.GetValue<bool>())
            {
                var msg  = node["error"]?.GetValue<string>() ?? "API error";
                var code = node["code"]?.GetValue<string>();
                var flds = ParseFields(node["fields"]);
                throw new ApiException(msg, (int)response.StatusCode, code, flds);
            }
            var dataNode = node["data"];
            if (dataNode != null)
                return dataNode.Deserialize<T>() ?? throw new InvalidOperationException("Null data");
        }

        return JsonSerializer.Deserialize<T>(body)
            ?? throw new InvalidOperationException("Null response");
    }

    private static async Task ThrowApiErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var node = JsonNode.Parse(body);
        var msg  = node?["error"]?.GetValue<string>() ?? $"HTTP {(int)response.StatusCode}";
        var code = node?["code"]?.GetValue<string>();
        throw new ApiException(msg, (int)response.StatusCode, code);
    }

    private static IReadOnlyDictionary<string, string[]>? ParseFields(JsonNode? node)
    {
        if (node is not JsonObject obj) return null;
        var result = new Dictionary<string, string[]>();
        foreach (var kv in obj)
        {
            var arr = kv.Value?.AsArray().Select(v => v?.GetValue<string>() ?? "").ToArray() ?? Array.Empty<string>();
            result[kv.Key] = arr;
        }
        return result;
    }

    public void Dispose()
    {
        if (_ownsHttpClient) _httpClient.Dispose();
    }
}
