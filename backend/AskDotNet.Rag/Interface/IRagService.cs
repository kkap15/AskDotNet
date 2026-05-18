using AskDotNet.Core.Records;

namespace AskDotNet.Rag.Interface;

public interface IRagService
{
    IAsyncEnumerable<string> AskStreamingAsync(string question,
        Func<IReadOnlyList<ChunkReference>, Task> onSourcesReady, CancellationToken cancellationToken = default);
    Task<RagResponse> AskAsync(string question, CancellationToken cancellationToken = default);
}