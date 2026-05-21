using AskDotNet.Core.Records;

namespace AskDotNet.Ingest.Interface;

public interface IChunker
{
    public IReadOnlyList<Chunk> Chunk(Page page);
}