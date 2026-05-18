using AskDotNet.Embed;
using AskDotNet.Embed.Interface;
using AskDotNet.Embed.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((_, config) =>
    {
        config.AddJsonFile("appsettings.json");
        config.AddUserSecrets<Program>(optional: true);
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureServices((ctx, services) =>
    {
        var cfg = ctx.Configuration;

        services.AddSingleton<IDatabaseService>(_ =>
        {
            return new DatabaseService(cfg["Postgres:ConnectionString"]
                                ?? throw new InvalidOperationException("Postgres:ConnectionString is not set"));
        });
        services.AddSingleton<IEmbeddingService>(_ =>
        {
            return new EmbeddingService(
                cfg["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not set"),
                cfg["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not set"),
                cfg["AzureOpenAI:DeploymentName"] ??
                throw new InvalidOperationException("AzureOpenAI:DeploymentName is not set"));
        });

        services.AddHostedService<EmbedWorker>();
    })
    .RunConsoleAsync();