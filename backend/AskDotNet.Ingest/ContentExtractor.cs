using AngleSharp;
using AngleSharp.Html.Parser;
using AskDotNet.Core.Records;

namespace AskDotNet.Ingest;

public sealed class ContentExtractor
{
    private readonly ReverseMarkdown.Converter _converter = new();
    private readonly IHtmlParser _parser = new HtmlParser(new HtmlParserOptions(), BrowsingContext.New());

    public async Task<Page?> ExtractAsync(string url, string html)
    {
        // Remove noise elements
        var noiseSelectors = new[]
        {
            "[data-bi-name='feedback-section']",
            ".feedback-section",
            ".community-content",
            "section[data-bi-name='recommendations']",
            ".local-time",
            "local-time",
        };
        var angleSharpDocument = await _parser.ParseDocumentAsync(html);
        var headingPath = ExtractHeadingPathFromUrl(url);
        
        var debugBreadcrumb = angleSharpDocument
            .QuerySelector("#article-header-breadcrumbs-overflow-popover");
        Console.WriteLine($"Breadcrumb element found: {debugBreadcrumb is not null}");

        if (debugBreadcrumb is not null)
        {
            var links = debugBreadcrumb.QuerySelectorAll("a");
            Console.WriteLine($"Links found: {links.Length}");
            foreach (var link in links)
            {
                Console.WriteLine($"  title='{link.GetAttribute("title")}' text='{link.TextContent.Trim()}'");
            }
        }

        var contentDivs = angleSharpDocument.QuerySelectorAll("div.content");
        var article = contentDivs.LastOrDefault();

        if (article is null)
        {
            return null;
        }

        foreach (var selector in noiseSelectors)
        {
            foreach (var element in article.QuerySelectorAll(selector).ToList())
            {
                element.Remove();
            }
        }

        var titleElement = article.QuerySelector("h1");
        var title = angleSharpDocument
            .QuerySelector("[data-main-column] div.content h1")?.TextContent.Trim() ?? string.Empty;

        var htmlContent = article.InnerHtml;
        
        var markdown = _converter.Convert(htmlContent);

        return new Page(url, title, markdown, headingPath, DateTimeOffset.UtcNow);
    }

    private static IReadOnlyList<string> ExtractHeadingPathFromUrl(string url)
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