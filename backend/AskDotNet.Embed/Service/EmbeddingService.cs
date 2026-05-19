using System.ClientModel;
using AskDotNet.Embed.Interface;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

namespace AskDotNet.Embed.Service;

public sealed class EmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    public EmbeddingService(string endpoint, string apiKey, string deploymentName)
    {
        var openAiClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
        _generator = openAiClient.GetEmbeddingClient(deploymentName).AsIEmbeddingGenerator();
    }
    
    public async Task<float[][]> EmbedBatchAsync(List<string> texts)
    {
        var results = await _generator.GenerateAsync(texts);
        
        return results.Select(e => e.Vector.ToArray()).ToArray();
    }
}