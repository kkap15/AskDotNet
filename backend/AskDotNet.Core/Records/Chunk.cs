namespace AskDotNet.Core.Records;

/// <summary>
/// A semantic unit of content ready for embedding.
/// Self-contained — carries all metadata needed for retrieval
/// and citation without joining back to the source Page.
/// Produced by the chunker, consumed by the embedding pipeline.
/// </summary>

public sealed record Chunk(
    string Id,
    string SourceUrl,
    string SourceTitle,
    IReadOnlyList<string> HeadingPath,
    string SectionHeading,
    string Content,
    int TokenCount
);