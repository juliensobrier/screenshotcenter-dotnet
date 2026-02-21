namespace ScreenshotCenter;

/// <summary>Thrown when the API returns a non-2xx status or success=false.</summary>
public class ApiException : Exception
{
    public int Status { get; }
    public string? Code { get; }
    public IReadOnlyDictionary<string, string[]> Fields { get; }

    public ApiException(string message, int status, string? code = null,
        IReadOnlyDictionary<string, string[]>? fields = null)
        : base(message)
    {
        Status = status;
        Code   = code;
        Fields = fields ?? new Dictionary<string, string[]>();
    }
}

/// <summary>Thrown by WaitForAsync when polling exceeds the timeout.</summary>
public class TimeoutException : Exception
{
    public long ScreenshotId { get; }
    public int TimeoutMs { get; }

    public TimeoutException(long screenshotId, int timeoutMs)
        : base($"Screenshot {screenshotId} did not complete within {timeoutMs}ms")
    {
        ScreenshotId = screenshotId;
        TimeoutMs    = timeoutMs;
    }
}

/// <summary>Thrown by WaitForAsync when the screenshot status is "error".</summary>
public class ScreenshotFailedException : Exception
{
    public long ScreenshotId { get; }
    public string? Reason { get; }

    public ScreenshotFailedException(long screenshotId, string? reason = null)
        : base($"Screenshot {screenshotId} failed: {reason ?? "unknown error"}")
    {
        ScreenshotId = screenshotId;
        Reason       = reason;
    }
}
