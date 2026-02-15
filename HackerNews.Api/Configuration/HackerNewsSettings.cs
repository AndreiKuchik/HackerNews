namespace HackerNews.Api.Configuration;

public class HackerNewsSettings
{
    public const string SectionName = "HackerNews";

    public required string BaseUrl { get; init; }
    public required int MaxParallelRequests { get; init; }
    public required int CacheExpirationMinutes { get; init; }
}
