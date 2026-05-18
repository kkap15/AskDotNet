using System.Runtime.CompilerServices;
using AskDotNet.Core.Records;
using AskDotNet.Rag.Helper;
using AskDotNet.Rag.Interface;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace AskDotNet.Rag.Service;

public sealed class RagService : IRagService, IAsyncDisposable
{
    private readonly IChatClient _chatClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly ILogger<RagService> _logger;
    private readonly DatabaseHelper _dbHelper;

    public RagService(IEmbeddingGenerator<string, Embedding<float>> generator, IChatClient chatClient,
        ILogger<RagService> logger, string connectionString)
    {
        _generator = generator;
        _chatClient = chatClient;
        _logger = logger;
        _dbHelper = new DatabaseHelper(connectionString);
    }

    public async Task<RagResponse> AskAsync(string question, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing question: {Question}", question);
        var questionEmbedding = await _generator.GenerateAsync(new[] { question }, cancellationToken: ct);
        var vector = new Pgvector.Vector(questionEmbedding[0].Vector.ToArray());

        var chunks = (await _dbHelper.RetrieveChunksAsync(vector, 10, ct))
            .Where(c => c.Similarity > 0.5)
            .ToList();

        if (chunks.Count == 0)
        {
            _logger.LogWarning("No chunks found for question: {Question}", question);
            return new RagResponse("I couldn't find any relevant information for your question.", []);
        }

        var context = RagHelper.BuildContext(chunks);
        var answer = await RagHelper.GenerateAnswerAsync(question, context, _chatClient, ct);
        
        var sources = chunks
            .Select(c => new ChunkReference(c.SourceUrl, c.SourceTitle, c.SectionHeading, c.Similarity))
            .ToList();
        
        return new RagResponse(answer, sources);
    }

    public async IAsyncEnumerable<string> AskStreamingAsync(string question,
        Func<IReadOnlyList<ChunkReference>, Task> onSourceReady, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var questionEmbeddings = await _generator.GenerateAsync(new[] { question }, cancellationToken: cancellationToken);
        var vector = new Vector(questionEmbeddings[0].Vector.ToArray());
        var chunks = (await _dbHelper.RetrieveChunksAsync(vector, 10, cancellationToken))
            .Where(c => c.Similarity > 0.5)
            .ToList();

        if (chunks.Count is 0)
        {
            await onSourceReady([]);
            yield return "I couldn't find relevant information for your question.";
            yield break;
        }
        
        var sources = chunks
            .Select(c => new ChunkReference(c.SourceUrl, c.SourceTitle, c.SectionHeading, c.Similarity))
            .ToList();
        await onSourceReady(sources);
        
        var context = RagHelper.BuildContext(chunks);
        var messages = RagHelper.BuildMessages(question, context);

        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages,
                           cancellationToken: cancellationToken))
        {
            var text = update.Text;
            if (text is not null)
            {
                yield return text;
            }
        }
    }
    
    public async ValueTask DisposeAsync() => await _dbHelper.DisposeAsync();
}