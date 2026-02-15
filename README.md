# Hacker News Best Stories API

ASP.NET Core API that returns the top *n* best stories from Hacker News, sorted by score descending.

## Architecture

The project uses Clean Architecture principles (Application, Infrastructure, Controllers (Presentation)) but keeps everything in a single project — splitting into separate assemblies felt like overkill for this scope, though it would be easy to do if the project grows.

```
Controller 
    |
-> IHackerNewsService <-> IMemoryCache 
    | 
-> IHackerNewsClient <-> HttpClient (Polly)
    |
-> Hacker News API         
```

- **Controller** - validates input, delegates to service
- **Application layer** (`HackerNewsService`) - orchestrates parallel fetching with `SemaphoreSlim` throttling, caches stories in `IMemoryCache`
- **Infrastructure layer** (`HackerNewsClient`) - typed `HttpClient` calling the HN Firebase API

## Features

- Parallel story fetching with configurable concurrency (`MaxParallelRequests`)
- In-memory caching with configurable TTL (`CacheExpirationMinutes`)
- Polly resilience (retry, circuit breaker, timeout) via `AddStandardResilienceHandler`
- Structured logging with Serilog
- Global exception handling returning Problem Details
- Swagger UI at `/swagger` (Development only)
- Unit and integration tests (xUnit, Moq, FluentAssertions)

## Configuration

Settings live in `appsettings.json` under `HackerNews`:

| Setting | Default | Description |
|---|---|---|
| `BaseUrl` | `https://hacker-news.firebaseio.com/v0/` | HN API base URL |
| `MaxParallelRequests` | `25` | Max concurrent requests to HN API |
| `CacheExpirationMinutes` | `5` | Story cache TTL in minutes |

## How to Run

### IDE (Rider / Visual Studio)

1. Open `HackerNews.sln`
2. Set `HackerNews.Api` as the startup project
3. Run (F5)
4. Navigate to `https://localhost:7174/api/beststories/5` or `http://localhost:5108/api/beststories/5`
OR
5. Swagger UI is available at `https://localhost:7174/swagger` or `http://localhost:5108/swagger`

### Docker Compose

```bash
docker compose up --build
```

Navigate to `http://localhost:8080/api/beststories/5`

Swagger UI: `http://localhost:8080/swagger`

### Run Tests

```bash
dotnet test
```

## API

```
GET /api/beststories/{n}
```

Returns the first *n* best stories sorted by score descending. Parameter `n` must be a positive integer.

Example response:

```json
[
  {
    "title": "Story title",
    "uri": "https://example.com",
    "postedBy": "author",
    "time": "2024-01-01T00:00:00+00:00",
    "score": 100,
    "commentCount": 42
  }
]
```

## Trade-offs & Things I'd Change

- **Integration tests need internet** - `BestStoriesApiTests` hits the real HN API (no mocked HTTP client), so it can be slow or flaky. I kept it this way on purpose as a smoke test; the actual logic is covered by `HackerNewsServiceTests` which runs fully offline with mocks.

- **No max cap on `n`** - the controller checks that `n >= 1` but doesn't cap it. In practice this is fine since the HN API only returns around 200 best story IDs anyway, so the response is naturally bounded. Didn't want to hardcode an arbitrary limit that might drift from the actual API behavior.
