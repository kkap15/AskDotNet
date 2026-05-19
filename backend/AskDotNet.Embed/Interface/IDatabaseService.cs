using AskDotNet.Core.Records;

namespace AskDotNet.Embed.Interface;

public interface IDatabaseService
{
    public Task<HashSet<string>> GetExistingIdsAsync(string[] ids);
    public Task InsertChunkAsync(Chunk chunk, float[] embedding);
}