# ScreenshotCenter .NET SDK

Official C# / .NET SDK for the [ScreenshotCenter](https://screenshotcenter.com) API.

## Requirements

- .NET 6.0 or later

## Installation

```bash
dotnet add package ScreenshotCenter
```

## Quick start

```csharp
using ScreenshotCenter;

var client = new ScreenshotCenterClient(Environment.GetEnvironmentVariable("SCREENSHOTCENTER_API_KEY")!);

var shot   = await client.Screenshot.CreateAsync("https://example.com");
var result = await client.WaitForAsync(shot.Id);
Console.WriteLine(result.Status); // "finished"
```

## Use cases

### Geo-targeting

```csharp
var shot = await client.Screenshot.CreateAsync("https://example.com",
    new Dictionary<string, string> { ["country"] = "fr", ["lang"] = "fr-FR" });
```

### PDF

```csharp
var shot = await client.Screenshot.CreateAsync("https://example.com",
    new Dictionary<string, string> { ["pdf"] = "true" });
var done = await client.WaitForAsync(shot.Id);
await client.Screenshot.SavePdfAsync(done.Id, "/tmp/page.pdf");
```

### Multiple shots

```csharp
var shot = await client.Screenshot.CreateAsync("https://example.com",
    new Dictionary<string, string> { ["shots"] = "5" });
var done = await client.WaitForAsync(shot.Id);
await client.Screenshot.SaveImageAsync(done.Id, "/tmp/shot3.png",
    new Dictionary<string, string> { ["shot"] = "3" });
```

### Batch

```csharp
// Requires batch worker service to be running
var batch  = await client.Batch.CreateAsync(
    new[] { "https://example.com", "https://example.org" }, "us");
var result = await client.Batch.WaitForAsync(batch.Id, intervalMs: 3000, timeoutMs: 120_000);
await client.Batch.SaveZipAsync(result.Id, "/tmp/batch.zip");
```

### Error handling

```csharp
try
{
    var result = await client.WaitForAsync(shot.Id, timeoutMs: 60_000);
}
catch (ScreenshotFailedException e)
{
    Console.Error.WriteLine($"Failed: {e.Reason}");
}
catch (TimeoutException e)
{
    Console.Error.WriteLine($"Timed out after {e.TimeoutMs}ms");
}
catch (ApiException e)
{
    Console.Error.WriteLine($"API error {e.Status}: {e.Message}");
}
```

## API reference

### `ScreenshotCenterClient(apiKey, baseUrl?, httpClient?)`

| Parameter | Default | Description |
|-----------|---------|-------------|
| `apiKey`  | — | Required |
| `baseUrl` | production | Override API base URL |
| `httpClient` | built-in | Injectable `HttpClient` for testing |

### `client.Screenshot`

| Method | Description |
|--------|-------------|
| `CreateAsync(url, parameters?)` | Create a screenshot |
| `InfoAsync(id)` | Get screenshot metadata |
| `ListAsync(parameters?)` | List screenshots |
| `SearchAsync(url, parameters?)` | Search by URL |
| `ThumbnailAsync(id, parameters?)` | Raw image bytes |
| `HtmlAsync(id)` | Raw HTML bytes |
| `PdfAsync(id)` | Raw PDF bytes |
| `VideoAsync(id)` | Raw video bytes |
| `DeleteAsync(id, data?)` | Delete a screenshot |
| `SaveImageAsync(id, path, parameters?)` | Save image to disk |
| `SaveHtmlAsync(id, path)` | Save HTML to disk |
| `SavePdfAsync(id, path)` | Save PDF to disk |
| `SaveVideoAsync(id, path)` | Save video to disk |

### `client.Batch`

| Method | Description |
|--------|-------------|
| `CreateAsync(urls, country, parameters?)` | Create a batch |
| `CreateFromStringAsync(content, country, parameters?)` | Create from newline-separated string |
| `InfoAsync(id)` | Get batch status |
| `ListAsync(parameters?)` | List batches |
| `DownloadAsync(id)` | Download ZIP bytes |
| `SaveZipAsync(id, path)` | Save ZIP to disk |
| `WaitForAsync(id, intervalMs?, timeoutMs?)` | Poll until done |

### `client.Account`

| Method | Description |
|--------|-------------|
| `InfoAsync()` | Get account info (balance, plan) |

### `client.WaitForAsync(id, intervalMs?, timeoutMs?)`

Poll a screenshot until `finished` or `error`.

## Testing

### Environment variables

| Variable | Description |
|----------|-------------|
| `SCREENSHOTCENTER_API_KEY` | Required for integration tests |
| `SCREENSHOTCENTER_BASE_URL` | Override base URL (default: production) |

### Running tests

```bash
# Unit tests only (default)
dotnet test

# Integration tests against a local instance
SCREENSHOTCENTER_API_KEY=your_key \
SCREENSHOTCENTER_BASE_URL=http://localhost:3000/api/v1 \
dotnet test --filter "Category=Integration"
```

## License

MIT — see [LICENSE](LICENSE).
