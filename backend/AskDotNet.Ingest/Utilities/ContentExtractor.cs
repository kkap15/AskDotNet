using AngleSharp;
using AngleSharp.Html.Parser;
using AskDotNet.Core.Records;
using AskDotNet.Ingest.Helpers;
using AskDotNet.Ingest.Interface;

namespace AskDotNet.Ingest.Utilities;

public sealed class ContentExtractor : IContentExtractor
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
        var headingPath = ContentExtractorHelper.ExtractHeadingPathFromUrl(url);
        
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
        
        _ = article.QuerySelector("h1");
        var title = angleSharpDocument
            .QuerySelector("[data-main-column] div.content h1")?.TextContent.Trim() ?? string.Empty;

        var htmlContent = article.InnerHtml;
        
        var markdown = _converter.Convert(htmlContent);

        return new Page(url, title, markdown, headingPath, DateTimeOffset.UtcNow);
    }
}