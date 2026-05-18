using System.Text;
using AskDotNet.Core.Records;
using AskDotNet.Rag.Interface;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Npgsql;
using Pgvector;

namespace AskDotNet.Rag.Service;

public sealed class RagService : IRagService, IAsyncDisposable
{
    private readonly IChatClient _chatClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<RagService> _logger;

    public RagService(IEmbeddingGenerator<string, Embedding<float>> generator, IChatClient chatClient,
        ILogger<RagService> logger, string connectionString)
    {
        _generator = generator;
        _chatClient = chatClient;
        _logger = logger;
        _dataSource = BuildDataSource(connectionString);
    }

    public async Task<RagResponse> AskAsync(string question, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing question: {Question}", question);
        var questionEmbedding = await _generator.GenerateAsync(new[] { question }, cancellationToken: ct);
        var vector = new Pgvector.Vector(questionEmbedding[0].Vector.ToArray());

        var chunks = (await RetrieveChunksAsync(vector, 10, ct))
            .Where(c => c.Similarity > 0.5)
            .ToList();

        if (chunks.Count == 0)
        {
            _logger.LogWarning("No chunks found for question: {Question}", question);
            return new RagResponse("I couldn't find any relevant information for your question.", []);
        }

        var context = BuildContext(chunks);
        var answer = await GenerateAnswerAsync(question, context, ct);
        
        var sources = chunks
            .Select(c => new ChunkReference(c.SourceUrl, c.SourceTitle, c.SectionHeading, c.Similarity))
            .ToList();
        
        return new RagResponse(answer, sources);
    }

    private static NpgsqlDataSource BuildDataSource(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.UseVector();
        return builder.Build();
    }

    private async Task<List<(string SourceUrl, string SourceTitle, string SectionHeading, string Content, double
            Similarity)>>
        RetrieveChunksAsync(Vector queryVector, int topK, CancellationToken ct)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(ct);
        var query = @"
SELECT source_url, source_title, section_heading, content, 1 - (embedding <=> @queryEmbedding) AS similarity
FROM chunks
ORDER BY embedding <=> @queryEmbedding
LIMIT @topK";

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("queryEmbedding", queryVector);
        command.Parameters.AddWithValue("topK", topK);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result =
            new List<(string SourceUrl, string SourceTitle, string SectionHeading, string Content, double Similarity
                )>();
        while (await reader.ReadAsync(ct))
        {
            result.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
                reader.GetDouble(4)));
        }

        return result;
    }

    private static string BuildContext(
        List<(string SourceUrl, string SourceTitle, string SectionHeading, string Content, double Similarity)> chunks)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < chunks.Count; i++)
        {
            var c = chunks[i];
            builder.AppendLine($"[{i + 1}] {c.SourceTitle} - {c.SectionHeading}");
            builder.AppendLine($"Source: {c.SourceUrl}");
            builder.AppendLine(c.Content);
            builder.AppendLine();
        }
        return builder.ToString();
    }

    private async Task<string> GenerateAnswerAsync(string question, string context, CancellationToken ct)
    {
        var systemPrompt = """
                           You are a helpful C# documentation assistant. Answer the questions based ONLY on the provided documentation excerpts.
                           If the answer is not in the provided excerpts, say "I don't have enough information to answer that."
                           Always be concise and accurate. Reference specific sections when relevant.
                           """;

        var userPrompt = $"""
                          Documentation excerpts:
                          {context}

                          Question: {question}
                          """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt)
        };

        var response = await _chatClient.GetResponseAsync(messages, null, ct);

        return response.Text;
    }

public async ValueTask DisposeAsync() => await _dataSource.DisposeAsync();
}