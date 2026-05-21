using AskDotNet.Core.Records;
using AskDotNet.Ingest.Interface;
using AskDotNet.Ingest.Utilities;
using Microsoft.Extensions.Logging;

namespace AskDotNet.Ingest.Service;

public sealed class Crawler(HttpClient client, ContentExtractor extractor, ILogger<Crawler> logger) : ICrawler
{
    public async Task<IReadOnlyList<Page>> CrawlAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default)
    {
        var pages = new List<Page>();
        var urlList = urls.ToList();
        var total = urlList.Count;
        for (var i = 0; i < urlList.Count; i++)
        {
            var url = urlList[i];
            logger.LogInformation("[{Current}/{Total}] Crawling {Url}", i + 1, total, url);
            try
            {
                var page = await FetchPageAsync(url, cancellationToken);
                if (page is null) continue;
                pages.Add(page);
                await Task.Delay(500, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Failed to fetch page: {Url}", url);
            }
        
        }
        
        return pages;
    }

    private async Task<Page?> FetchPageAsync(string url, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(url, cancellationToken);
        logger.LogDebug("Response: {StatusCode} {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to fetch page: {Url} ({StatusCode})", url, response.StatusCode);
            return null;
        }
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        return await extractor.ExtractAsync(url, html);
    }
}