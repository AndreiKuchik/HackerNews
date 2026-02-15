using HackerNews.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace HackerNews.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BestStoriesController(IHackerNewsService hackerNewsService) : ControllerBase
{
    [HttpGet("{n:int}")]
    public async Task<IActionResult> GetBestStories(int n, CancellationToken cancellationToken)
    {
        if (n < 1)
            return Problem("Parameter 'n' must be a positive integer.", statusCode: 400);

        var stories = await hackerNewsService.GetBestStoriesAsync(n, cancellationToken);

        return Ok(stories);
    }
}
