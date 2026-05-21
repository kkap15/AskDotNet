using AskDotNet.Ingest;
using AskDotNet.Ingest.Service;
using AskDotNet.Ingest.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;


static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1))
                            + TimeSpan.FromMilliseconds(new Random().Next(100, 500)));

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() =>
    Policy.TimeoutAsync<HttpResponseMessage>(10);
    
await Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
    })
    .ConfigureServices((_, services) =>
    {
        services.AddHttpClient<Crawler>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());
        
        services.AddSingleton<ContentExtractor>();
        services.AddSingleton<Chunker>(_ => new Chunker(800, 100));
        services.AddSingleton<Crawler>(sp =>
            new Crawler(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(Crawler)),
                sp.GetRequiredService<ContentExtractor>(),
                sp.GetRequiredService<ILogger<Crawler>>()
            ));
        services.AddHttpClient<FetchToc>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetTimeoutPolicy());
        
        services.AddHostedService<IngestWorker>();
    })
    .RunConsoleAsync();