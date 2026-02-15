using FluentAssertions;
using HackerNews.Api.Application;
using HackerNews.Api.Configuration;
using HackerNews.Api.Infrastructure;
using HackerNews.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace HackerNews.UnitTests;

public class HackerNewsServiceTests
{
    private readonly Mock<IHackerNewsClient> _clientMock = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IOptions<HackerNewsSettings> _settings = Options.Create(new HackerNewsSettings
    {
        BaseUrl = "https://hacker-news.firebaseio.com/v0/",
        MaxParallelRequests = 5,
        CacheExpirationMinutes = 5
    });
    private readonly Mock<ILogger<HackerNewsService>> _loggerMock = new();

    private HackerNewsService CreateService() =>
        new(_clientMock.Object, _cache, _settings, _loggerMock.Object);

    private static HackerNewsItem CreateItem(int id, int score) =>
        new(id, $"Story {id}", $"https://example.com/{id}", "author", 1700000000, score, 10);

    [Fact]
    public async Task GetBestStoriesAsync_ReturnsSortedByScoreDescending()
    {
        _clientMock.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3]);
        _clientMock.Setup(c => c.GetStoryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateItem(1, 10));
        _clientMock.Setup(c => c.GetStoryAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateItem(2, 50));
        _clientMock.Setup(c => c.GetStoryAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateItem(3, 30));

        var service = CreateService();
        var result = await service.GetBestStoriesAsync(3);

        result.Should().HaveCount(3);
        result.Select(s => s.Score).Should().BeInDescendingOrder();
        result.First().Score.Should().Be(50);
    }

    [Fact]
    public async Task GetBestStoriesAsync_ReturnsRequestedCount()
    {
        _clientMock.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2, 3, 4, 5]);
        _clientMock.Setup(c => c.GetStoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => CreateItem(id, id * 10));

        var service = CreateService();
        var result = await service.GetBestStoriesAsync(2);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBestStoriesAsync_SkipsNullStories()
    {
        _clientMock.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([1, 2]);
        _clientMock.Setup(c => c.GetStoryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateItem(1, 100));
        _clientMock.Setup(c => c.GetStoryAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((HackerNewsItem?)null);

        var service = CreateService();
        var result = await service.GetBestStoriesAsync(2);

        result.Should().HaveCount(1);
        result.First().Score.Should().Be(100);
    }

    [Fact]
    public async Task GetBestStoriesAsync_UsesCacheOnSecondCall()
    {
        _clientMock.Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([1]);
        _clientMock.Setup(c => c.GetStoryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateItem(1, 42));

        var service = CreateService();
        await service.GetBestStoriesAsync(1);
        await service.GetBestStoriesAsync(1);

        _clientMock.Verify(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.Verify(c => c.GetStoryAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
