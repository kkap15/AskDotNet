using AskDotNet.Core.Records;

namespace AskDotNet.Ingest.Interface;

public interface ICrawler
{
    public Task<IReadOnlyList<Page>> CrawlAsync(IEnumerable<string> urls,
        CancellationToken cancellationToken = default);
}