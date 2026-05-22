using System.ClientModel;
using AskDotNet.Eval;
using AskDotNet.Rag.Interface;
using AskDotNet.Rag.Service;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), false);
        config.AddUserSecrets<Program>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
    })
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;
        services.AddSingleton<AzureOpenAIClient>(_ => new AzureOpenAIClient(
            new Uri(cfg["AzureOpenAI:Endpoint"]!),
            new ApiKeyCredential(cfg["AzureOpenAI:ApiKey"]!)));

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp => sp
            .GetRequiredService<AzureOpenAIClient>()
            .GetEmbeddingClient(sp.GetRequiredService<IConfiguration>()["AzureOpenAI:DeploymentName"]!)
            .AsIEmbeddingGenerator());

        services.AddSingleton<IChatClient>(sp => sp.GetRequiredService<AzureOpenAIClient>()
            .GetChatClient(sp.GetRequiredService<IConfiguration>()["AzureOpenAI:ChatDeploymentName"]!)
            .AsIChatClient());

        services.AddSingleton<IRagService>(sp => new RagService(
            sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
            sp.GetRequiredService<IChatClient>(),
            sp.GetRequiredService<ILogger<RagService>>(),
            cfg["Postgres:ConnectionString"]!));

        services.AddHostedService<EvalWorker>();
    })
    .RunConsoleAsync();