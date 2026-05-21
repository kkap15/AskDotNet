using System.Text.Json.Serialization;

namespace AskDotNet.Core.Records;

public sealed record TocNode(
    [property: JsonPropertyName("href")] string? Href,
    [property: JsonPropertyName("toc_title")] string? TocTitle,
    [property: JsonPropertyName("children")] IReadOnlyList<TocNode>? Children);
    
public sealed record TocRoot(
    [property: JsonPropertyName("items")] List<TocNode> Items);