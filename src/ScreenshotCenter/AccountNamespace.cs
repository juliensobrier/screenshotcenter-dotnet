namespace ScreenshotCenter;

/// <summary>Account-related API methods.</summary>
public sealed class AccountNamespace
{
    private readonly ScreenshotCenterClient _client;

    internal AccountNamespace(ScreenshotCenterClient client)
        => _client = client;

    /// <summary>Returns account details including credit balance.</summary>
    public Task<Account> InfoAsync(CancellationToken ct = default)
        => _client.GetAsync<Account>("/account/info",
            new Dictionary<string, string>(), ct);
}
