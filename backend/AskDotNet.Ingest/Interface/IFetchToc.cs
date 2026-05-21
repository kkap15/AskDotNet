namespace AskDotNet.Ingest.Interface;

public interface IFetchToc
{
    public Task<List<string>> FetchUrlsAsync(CancellationToken cancellationToken = default);
}