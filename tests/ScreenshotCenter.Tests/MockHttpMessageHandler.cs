using System.Net;

namespace ScreenshotCenter.Tests;

/// <summary>
/// A queue-based mock HttpMessageHandler for unit testing.
/// Enqueue responses with <see cref="Enqueue"/> before making requests.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _queue = new();
    public List<HttpRequestMessage> Requests { get; } = new();

    public void Enqueue(HttpResponseMessage response) => _queue.Enqueue(response);

    public void EnqueueJson(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        _queue.Enqueue(response);
    }

    public void EnqueueSuccessJson(object data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { success = true, data });
        EnqueueJson(json);
    }

    public void EnqueueErrorJson(string error, HttpStatusCode status = HttpStatusCode.BadRequest,
        string? code = null)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { success = false, error, code });
        EnqueueJson(json, status);
    }

    public void EnqueueBinary(byte[] data, string contentType = "image/png")
    {
        _queue.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(data) { Headers = { ContentType = new(contentType) } }
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (_queue.Count == 0)
            throw new InvalidOperationException("No more mock responses enqueued");
        return Task.FromResult(_queue.Dequeue());
    }
}
