using System.Text.Json;
using AskDotNet.Core.Records;
using AskDotNet.Eval.Helpers;
using AskDotNet.Rag.Interface;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AskDotNet.Eval;

public sealed class EvalWorker(IRagService ragService,
    IChatClient chatClient,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    ILogger<EvalWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunEvalAsync(stoppingToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Failed to run evaluation worker");
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    private async Task RunEvalAsync(CancellationToken stoppingToken)
    {
        var goldenSetPath = configuration["Eval:GoldenSetPath"] 
            ?? throw new InvalidOperationException("Eval:GoldenSetPath is not set");
        var reportPath = configuration["Eval:ReportPath"] 
            ?? throw new InvalidOperationException("Eval:ReportPath is not set");
        var topK = int.Parse(configuration["Eval:TopK"] ?? "5");
        
        var json = await File.ReadAllTextAsync(goldenSetPath, stoppingToken);
        var goldenSet = JsonSerializer.Deserialize<List<GoldenSetEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize golden set");
        
        logger.LogInformation("Loaded {Count} golden set entries from {Path}", goldenSet.Count, goldenSetPath);

        var results = new List<EvalResult>();

        foreach (var entry in goldenSet)
        {
            logger.LogInformation("Processing question: {Question}", entry.Question);
            
            var retrievedSources = await ragService.RetrieveAsync(entry.Question, topK, stoppingToken);
            var retrievalHit =
                retrievedSources.Any(s => s.SourceUrl.TrimEnd('/') == entry.ExpectedSourceUrl.TrimEnd('/'));
            var topSimilarity = retrievedSources.FirstOrDefault()?.Similarity ?? 0;
            
            var ragResponse = await ragService.AskAsync(entry.Question, stoppingToken);

            var judgeResult = await EvalHelper.JudgeAsync(entry.Question, entry.ExpectedFact, ragResponse.Answer, retrievedSources,
                chatClient, stoppingToken);

            if (judgeResult.Reasoning.Contains("Parse failed:"))
            {
                logger.LogWarning("Failed to parse judge response: {Response}", judgeResult.Reasoning);
            }
            
            results.Add(new EvalResult(
                Question: entry.Question,
                RetrievalHit: retrievalHit,
                TopSimilarity: topSimilarity,
                Grounded: judgeResult.Grounded,
                AddressesQuestion: judgeResult.AddressesQuestion,
                Score: judgeResult.Score,
                Answer: ragResponse.Answer,
                JudgeReason: judgeResult.Reasoning));
            
            logger.LogInformation("Result:  RetrievalHit={Hit}, Score={Score}/5", retrievalHit, judgeResult.Score);
        }
        
        var report = EvalHelper.GenerateReport(results);
        await File.WriteAllTextAsync(reportPath, report, stoppingToken);
        logger.LogInformation("Report saved to {Path}", reportPath);
    }
}