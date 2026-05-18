using System.Data;
using AskDotNet.Core.Records;
using AskDotNet.Embed.Interface;
using Npgsql;

namespace AskDotNet.Embed.Service;

public sealed class DatabaseService(string connectionString) : IDatabaseService, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource = BuildDataSource(connectionString);

    private static NpgsqlDataSource BuildDataSource(string connectionString)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.UseVector();
        return builder.Build();
    }
    
    public async Task<HashSet<string>> GetExistingIdsAsync(string[] ids)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        
        await using var command = new NpgsqlCommand("SELECT id FROM chunks WHERE id = ANY(@ids)", connection);
        command.Parameters.AddWithValue("ids", ids);
        
        await using var reader = await command.ExecuteReaderAsync();
        var result = new HashSet<string>();
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetString(0));
        }
        
        return result;
    }

    public async Task InsertChunkAsync(Chunk chunk, float[] embedding)
    {
        var pgVector = new Pgvector.Vector(embedding);
        await using var connection = await _dataSource.OpenConnectionAsync();
        const string query = @"INSERT INTO chunks (id, source_url, source_title, heading_path, section_heading, content, token_count, embedding)
Values (@id, @source_url, @source_title, @heading_path, @section_heading, @content, @token_count, @pgvector)
ON CONFLICT (id) DO NOTHING;";
        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddWithValue("id", chunk.Id);
        command.Parameters.AddWithValue("source_url", chunk.SourceUrl);
        command.Parameters.AddWithValue("source_title", chunk.SourceTitle);
        command.Parameters.AddWithValue("heading_path", chunk.HeadingPath.ToArray());
        command.Parameters.AddWithValue("section_heading", chunk.SectionHeading);
        command.Parameters.AddWithValue("content", chunk.Content);
        command.Parameters.AddWithValue("token_count", chunk.TokenCount);
        command.Parameters.AddWithValue("pgvector", pgVector);
        
        await command.ExecuteNonQueryAsync();
    }
    
    public async ValueTask DisposeAsync() => await _dataSource.DisposeAsync();
}