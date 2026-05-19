namespace AskDotNet.Embed.Interface;

public interface IEmbeddingService
{
    public Task<float[][]> EmbedBatchAsync(List<string> texts);
}