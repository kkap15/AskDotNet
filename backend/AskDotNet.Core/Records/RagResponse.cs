namespace AskDotNet.Core.Records;

public sealed record RagResponse(string Answer, IReadOnlyList<ChunkReference> Sources);