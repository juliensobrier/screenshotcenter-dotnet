using System.Text.Json.Serialization;

namespace ScreenshotCenter;

public record Screenshot
{
    [JsonPropertyName("id")]          public long     Id          { get; init; }
    [JsonPropertyName("status")]      public string   Status      { get; init; } = "";
    [JsonPropertyName("url")]         public string   Url         { get; init; } = "";
    [JsonPropertyName("final_url")]   public string?  FinalUrl    { get; init; }
    [JsonPropertyName("error")]       public string?  Error       { get; init; }
    [JsonPropertyName("cost")]        public int      Cost        { get; init; }
    [JsonPropertyName("country")]     public string?  Country     { get; init; }
    [JsonPropertyName("has_html")]    public bool     HasHtml     { get; init; }
    [JsonPropertyName("has_pdf")]     public bool     HasPdf      { get; init; }
    [JsonPropertyName("has_video")]   public bool     HasVideo    { get; init; }
    [JsonPropertyName("shots")]       public int      Shots       { get; init; }
    [JsonPropertyName("created_at")]  public string?  CreatedAt   { get; init; }
    [JsonPropertyName("finished_at")] public string?  FinishedAt  { get; init; }
}

public record Batch
{
    [JsonPropertyName("id")]        public long   Id        { get; init; }
    [JsonPropertyName("status")]    public string Status    { get; init; } = "";
    [JsonPropertyName("count")]     public int    Count     { get; init; }
    [JsonPropertyName("processed")] public int    Processed { get; init; }
    [JsonPropertyName("failed")]    public int    Failed    { get; init; }
}

public record Account
{
    [JsonPropertyName("balance")] public double Balance { get; init; }
    [JsonPropertyName("plan")]    public string? Plan   { get; init; }
}
