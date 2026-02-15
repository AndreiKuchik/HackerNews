using HackerNews.Api.Models;

namespace HackerNews.Api.Infrastructure;

public interface IHackerNewsClient
{
    Task<int[]> GetBestStoryIdsAsync(CancellationToken cancellationToken = default);
    Task<HackerNewsItem?> GetStoryAsync(int storyId, CancellationToken cancellationToken = default);
}
