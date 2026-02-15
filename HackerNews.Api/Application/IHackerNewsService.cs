using HackerNews.Api.Models;

namespace HackerNews.Api.Application;

public interface IHackerNewsService
{
    Task<IReadOnlyCollection<StoryResponse>> GetBestStoriesAsync(int count, CancellationToken cancellationToken = default);
}
