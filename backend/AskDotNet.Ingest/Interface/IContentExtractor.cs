using AskDotNet.Core.Records;

namespace AskDotNet.Ingest.Interface;

public interface IContentExtractor
{
    public Task<Page?> ExtractAsync(string url, string html);
}