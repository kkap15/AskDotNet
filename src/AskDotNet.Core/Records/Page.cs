namespace AskDotNet.Core.Records;

/// <summary>
/// A single page fetched from Microsoft Learn.
/// Produced by the crawler, consumed by the chunker.
/// Raw content is stored as Markdown (converted from HTML)
/// to preserve structure without carrying HTML noise.
/// </summary>

public sealed record Page(
    string Url,
    string Title,
    string RawMarkdown,
    IReadOnlyList<string> HeadingPath,
    DateTimeOffset FetchedAt   
);