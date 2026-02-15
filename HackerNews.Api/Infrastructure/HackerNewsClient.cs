using HackerNews.Api.Models;

namespace HackerNews.Api.Infrastructure;

public class HackerNewsClient(
    HttpClient httpClient,
    ILogger<HackerNewsClient> logger) : IHackerNewsClient
{
    public async Task<int[]> GetBestStoryIdsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Fetching best story IDs from Hacker News API");

        return await httpClient.GetFromJsonAsync<int[]>("beststories.json", cancellationToken)
               ?? [];
    }

    public async Task<HackerNewsItem?> GetStoryAsync(int storyId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<HackerNewsItem>(
                $"item/{storyId}.json", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch story {StoryId}", storyId);
            return null;
        }
    }
}
