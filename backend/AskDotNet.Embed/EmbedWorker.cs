using System.Text.Json;
using AskDotNet.Core.Records;
using AskDotNet.Embed.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AskDotNet.Embed;

public class EmbedWorker(IDatabaseService db, 
    IEmbeddingService embedder,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    ILogger<EmbedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var chunksPath = configuration["Ingest:ChunksPath"] ??
                             throw new InvalidOperationException("Ingest:ChunksPath is not set");
            var batchSize = int.Parse(configuration["Ingest:BatchSize"]!);
            
            var json = await File.ReadAllTextAsync(chunksPath, stoppingToken);
            var chunks = JsonSerializer.Deserialize<List<Chunk>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;

            logger.LogInformation("Loaded {Count} chunks from {Path}", chunks.Count, chunksPath);

            var existingIds = await db.GetExistingIdsAsync(chunks.Select(c => c.Id).ToArray());
            var newChunks = chunks.Where(c => !existingIds.Contains(c.Id)).ToList();

            logger.LogInformation("{New} new chunks to embed ({Skipped} already in DB)", newChunks.Count,
                existingIds.Count);

            foreach (var batch in newChunks.Chunk(batchSize))
            {
                stoppingToken.ThrowIfCancellationRequested();
                var embeddings = await embedder.EmbedBatchAsync(batch.Select(c => c.Content).ToList());
                for (var j = 0; j < batch.Length; j++)
                {
                    await db.InsertChunkAsync(batch[j], embeddings[j]);
                    
                }
                logger.LogInformation("Batch embedded = {Count} chunks", batch.Length);
            }
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Failed to embed chunks");
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}