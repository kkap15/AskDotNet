using AskDotNet.Core.Records;

namespace AskDotNet.Ingest.Helpers;

public static class TocTreeHelper
{
    public const string BaseUrl = "https://learn.microsoft.com/en-us/dotnet/csharp/";
    
    public static void WalkTree(TocNode node, List<string> urls)
    {
        if (!string.IsNullOrWhiteSpace(node.Href))
        {
            var href = node.Href.TrimEnd('/');
            urls.Add($"{BaseUrl}{href}");
        }

        if (node.Children is null) return;
        foreach (var child in node.Children)
        {
            WalkTree(child, urls);
        }
    }
}