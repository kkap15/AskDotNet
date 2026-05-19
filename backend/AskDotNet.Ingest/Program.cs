using System.Text.Json;
using AskDotNet.Ingest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;


var cts = new CancellationTokenSource();

Console.CancelKeyPress += (sender, eventArgs) =>
{
    Console.WriteLine("Stopping...");
    eventArgs.Cancel = true;
    cts.Cancel();
};

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddHttpClient<Crawler>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetTimeoutPolicy());

builder.Services.AddSingleton<ContentExtractor>();
builder.Services.AddSingleton(new Chunker(800, 100));
builder.Services.AddSingleton<Crawler>(sp => 
    new Crawler(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(Crawler)),
        sp.GetRequiredService<ContentExtractor>(),
        sp.GetRequiredService<ILogger<Crawler>>()));


using var host = builder.Build();

var crawler = host.Services.GetRequiredService<Crawler>();
var chunker = host.Services.GetRequiredService<Chunker>();

string[] seedUrls =
[
    "https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/",
    "https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics",
    "https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14",
    "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-value-types",
    "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum",
    "https://learn.microsoft.com/en-us/dotnet/csharp/linq/get-started/type-relationships-in-linq-query-operations",
    "https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/structs",
    "https://learn.microsoft.com/en-us/dotnet/csharp/linq/get-started/write-linq-queries",
    "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions"
];

var pages = await crawler.CrawlAsync(seedUrls, cts.Token);
var allChunks = pages.SelectMany(chunker.Chunk).ToList();

var outputPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../data/output.json"));
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await File.WriteAllTextAsync(
    outputPath,
    JsonSerializer.Serialize(allChunks, new JsonSerializerOptions { WriteIndented = true }),
    cts.Token);

Console.WriteLine($"Pages crawled: {pages.Count}");
Console.WriteLine($"Chunks produced: {allChunks.Count}");
Console.WriteLine($"Output written to: {outputPath}");

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt - 1))
                            + TimeSpan.FromMilliseconds(new Random().Next(100, 500)));

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy() => 
    Policy.TimeoutAsync<HttpResponseMessage>(10);