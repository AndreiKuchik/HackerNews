using HackerNews.Api.Configuration;
using HackerNews.Api.Infrastructure;

namespace HackerNews.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHackerNewsClient(
        this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(HackerNewsSettings.SectionName)
                           .Get<HackerNewsSettings>()
                       ?? throw new InvalidOperationException(
                           $"Missing '{HackerNewsSettings.SectionName}' configuration section.");

        services.AddHttpClient<IHackerNewsClient, HackerNewsClient>(client =>
            {
                client.BaseAddress = new Uri(settings.BaseUrl);
            })
            .AddStandardResilienceHandler();

        return services;
    }
}
