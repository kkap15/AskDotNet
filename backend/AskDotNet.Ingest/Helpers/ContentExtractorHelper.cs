namespace AskDotNet.Ingest.Helpers;

public static class ContentExtractorHelper
{
    public static IReadOnlyList<string> ExtractHeadingPathFromUrl(string url)
    {
        // Maps known path segments to display names
        var segmentNames = new Dictionary<string, string>
        {
            ["dotnet"] = ".NET",
            ["csharp"] = "C#",
            ["fundamentals"] = "Fundamentals",
            ["language-reference"] = "Language Reference",
            ["types"] = "Types",
            ["statements"] = "Statements",
            ["operators"] = "Operators",
            ["keywords"] = "Keywords",
            ["builtin-types"] = "Built-in Types",
            ["program-structure"] = "Program Structure",
        };

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return Array.Empty<string>();
        }

        return uri.Segments
            .Select(s => s.Trim('/'))
            .Where(s => !string.IsNullOrWhiteSpace(s)
                        && s != "en-us"
                        && s != "learn.microsoft.com")
            .Select(s => segmentNames.TryGetValue(s, out var name) ? name : s)
            .ToList();
    }
}