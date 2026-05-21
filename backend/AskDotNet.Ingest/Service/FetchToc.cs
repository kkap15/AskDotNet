using System.Text.Json;
using AskDotNet.Core.Records;
using AskDotNet.Ingest.Helpers;
using AskDotNet.Ingest.Interface;

namespace AskDotNet.Ingest.Service;

public class FetchToc(HttpClient httpClient) : IFetchToc
{
    private const string TocUrl = $"{TocTreeHelper.BaseUrl}toc.json";

    public async Task<List<string>> FetchUrlsAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetStringAsync(new Uri(TocUrl), cancellationToken);
        var json = JsonSerializer.Deserialize<TocRoot>(result, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        })!;

        var urls = new List<string>();
        
        foreach (var child in json.Items)
        {
            TocTreeHelper.WalkTree(child, urls);
        }
        
        return urls;
    }
}