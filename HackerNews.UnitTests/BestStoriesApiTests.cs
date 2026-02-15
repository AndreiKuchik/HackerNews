using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HackerNews.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HackerNews.UnitTests;

public class BestStoriesApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBestStories_WithValidN_Returns200()
    {
        var response = await _client.GetAsync("/api/beststories/5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stories = await response.Content.ReadFromJsonAsync<List<StoryResponse>>();
        stories.Should().NotBeNull();
        stories!.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetBestStories_WithInvalidN_Returns4xx(int n)
    {
        var response = await _client.GetAsync($"/api/beststories/{n}");

        response.IsSuccessStatusCode.Should().BeFalse();
    }
}
