using AskDotNet.Rag.Interface;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AskDotNet.RagCli;

public sealed class RagWorker(IRagService ragService,
    IHostApplicationLifetime appLifetime,
    ILogger<RagWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await RunRagWorker();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Failed to run RAG worker");
        }
        finally
        {
            appLifetime.StopApplication();
        }
    }
    
    private async Task RunRagWorker()
    {
        Console.WriteLine("AskDotNet — C# Documentation Assistant");
        Console.WriteLine("Type a question, or press Enter to exit.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Question: ");
            var question = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(question)) break;

            var response = await ragService.AskAsync(question);
            
            Console.WriteLine();
            Console.WriteLine("Answer: ");
            Console.WriteLine(response.Answer);
            Console.WriteLine();
            Console.WriteLine("Sources: ");
            foreach (var source in response.Sources)
            {
                Console.WriteLine($"   [{source.Similarity:P0}] {source.SourceTitle} > {source.SectionHeading}");
                Console.WriteLine($"      {source.SourceUrl}");
            }
            Console.WriteLine();
        }
    }
}