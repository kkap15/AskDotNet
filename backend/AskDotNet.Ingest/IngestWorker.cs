using System.Text.Json;
using AskDotNet.Ingest.Service;
using AskDotNet.Ingest.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AskDotNet.Ingest;

public class IngestWorker(Crawler crawler,
    FetchToc fetchToc,
    Chunker chunker,
    IHostApplicationLifetime lifetime,
    ILogger<IngestWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunIngestWorker(stoppingToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Failed to run Ingest worker");
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    private async Task RunIngestWorker(CancellationToken stoppingToken)
    {
        var tocUrls = await fetchToc.FetchUrlsAsync(stoppingToken);
        var pages = await crawler.CrawlAsync(tocUrls, stoppingToken);
        var allChunks = pages.SelectMany(chunker.Chunk).ToList();
        
        var outputPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../data/output.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        
        logger.LogInformation("Pages crawled: {PagesCount}", pages.Count);
        logger.LogInformation("Chunks produced: {PagesCount}", allChunks.Count);
        
        await File.WriteAllTextAsync(
            outputPath,
            JsonSerializer.Serialize(allChunks, new JsonSerializerOptions
            {
                WriteIndented = true
            }), stoppingToken);
        logger.LogInformation("Output written to: {OutputPath}", outputPath);
    }
}