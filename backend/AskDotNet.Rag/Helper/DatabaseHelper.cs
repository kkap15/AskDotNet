using Npgsql;
using Pgvector;

namespace AskDotNet.Rag.Helper;

public sealed class DatabaseHelper : IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public DatabaseHelper(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.UseVector();
        _dataSource = builder.Build();
    }

    public async Task<List<(string SourceUrl, string SourceTitle, string SectionHeading, string Content, double
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
    
    public async ValueTask DisposeAsync() => await _dataSource.DisposeAsync();
}