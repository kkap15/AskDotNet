using AskDotNet.Core.Records;

namespace AskDotNet.Rag.Interface;

public interface IRagService
{
    Task<RagResponse> AskAsync(string question, CancellationToken cancellationToken = default);
}