using HackerNews.Api.Configuration;
using HackerNews.Api.Infrastructure;
using HackerNews.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HackerNews.Api.Application;

public class HackerNewsService(
    IHackerNewsClient hackerNewsClient,
    IMemoryCache cache,
    IOptions<HackerNewsSettings> settings,
    ILogger<HackerNewsService> logger) : IHackerNewsService
{
    private const string BestStoriesCacheKey = "best-story-ids";
    private readonly SemaphoreSlim _semaphore = new(settings.Value.MaxParallelRequests);

    public async Task<IReadOnlyCollection<StoryResponse>> GetBestStoriesAsync(
        int count, CancellationToken cancellationToken = default)
    {
        var storyIds = await GetBestStoryIdsAsync(cancellationToken);

        var topIds = storyIds.Take(count).ToList();

        var stories = await FetchStoriesInParallelAsync(topIds, cancellationToken);

        return stories
            .OrderByDescending(s => s.Score)
            .ToList();
    }

    private async Task<int[]> GetBestStoryIdsAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(BestStoriesCacheKey, out int[]? cachedIds))
            return cachedIds!;

        var ids = await hackerNewsClient.GetBestStoryIdsAsync(cancellationToken);

        cache.Set(BestStoriesCacheKey, ids,
            TimeSpan.FromMinutes(settings.Value.CacheExpirationMinutes));

        return ids;
    }

    private async Task<List<StoryResponse>> FetchStoriesInParallelAsync(
        List<int> storyIds, CancellationToken cancellationToken)
    {

        var tasks = storyIds.Select(async id =>
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return await GetStoryAsync(id, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);

        return results.Where(s => s is not null).ToList()!;
    }

    private async Task<StoryResponse?> GetStoryAsync(int storyId, CancellationToken cancellationToken)
    {
        var cacheKey = $"story-{storyId}";

        if (cache.TryGetValue(cacheKey, out StoryResponse? cachedStory))
            return cachedStory;

        var item = await hackerNewsClient.GetStoryAsync(storyId, cancellationToken);

        if (item is null)
        {
            logger.LogWarning("Story {StoryId} returned null from HN API", storyId);
            return null;
        }

        var story = MapToStoryResponse(item);

        cache.Set(cacheKey, story,
            TimeSpan.FromMinutes(settings.Value.CacheExpirationMinutes));

        return story;
    }

    private static StoryResponse MapToStoryResponse(HackerNewsItem item) =>
        new(
            Title: item.Title,
            Uri: item.Url,
            PostedBy: item.By,
            Time: DateTimeOffset.FromUnixTimeSeconds(item.Time),
            Score: item.Score,
            CommentCount: item.Descendants);
}
