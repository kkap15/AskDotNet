using System.ClientModel;
using System.Text.Json;
using System.Threading.RateLimiting;
using AskDotNet.Core.Records;
using AskDotNet.Rag.Interface;
using AskDotNet.Rag.Service;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace AskDotNet.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile("appsettings.json").AddUserSecrets<Program>();

        builder.Logging.ClearProviders().AddConsole().AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
        
        var cfg = builder.Configuration;
        builder.Services.AddSingleton<AzureOpenAIClient>(_ => new AzureOpenAIClient
            (new Uri(cfg["AzureOpenAI:Endpoint"]!), new ApiKeyCredential(cfg["AzureOpenAI:ApiKey"]!)));
        builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp => sp
            .GetRequiredService<AzureOpenAIClient>()
            .GetEmbeddingClient(sp.GetRequiredService<IConfiguration>()["AzureOpenAI:DeploymentName"]!)
            .AsIEmbeddingGenerator());
        builder.Services.AddSingleton<IChatClient>(sp => sp.GetRequiredService<AzureOpenAIClient>()
            .GetChatClient(sp.GetRequiredService<IConfiguration>()["AzureOpenAI:ChatDeploymentName"]!)
            .AsIChatClient());
        builder.Services.AddSingleton<IRagService>(sp => new RagService(
            sp.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(),
            sp.GetRequiredService<IChatClient>(),
            sp.GetRequiredService<ILogger<RagService>>(),
            cfg["Postgres:ConnectionString"]!));
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://{cfg["Auth0:Domain"]}/";
                options.Audience = cfg["Auth0:Audience"];
            });
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy<string>("adaptive", context =>
            {
                var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
                var key = isAuthenticated
                    ? $"auth:{context.User.Identity!.Name}"
                    : $"guest:{context.Connection.RemoteIpAddress}";

                return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = isAuthenticated ? 10 : 3,
                    ReplenishmentPeriod = isAuthenticated
                        ? TimeSpan.FromMinutes(1)
                        : TimeSpan.FromHours(1),
                    TokensPerPeriod = isAuthenticated ? 10 : 3,
                    AutoReplenishment = true
                });
            });
        });
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("frontend", policy =>
                {
                    policy.WithOrigins("http://localhost:5173",
                            "https://askdotnet.vercel.app",
                            "https://askdotnet.moviegasm.xyz")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                }
            );
        });
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
        });

        // Add services to the container.
        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();
        var app = builder.Build();

        //app.UseHttpsRedirection();
        app.UseCors("frontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapPost("/api/chat",
                async (IRagService rag, [FromBody] ChatRequest request, IHttpContextAccessor accessor, CancellationToken ct) =>
                {
                    var context = accessor.HttpContext!;
                    
                    context.Response.ContentType = "text/event-stream";
                    context.Response.Headers.CacheControl = "no-cache";
                    context.Response.Headers.Connection = "keep-alive";

                    IReadOnlyList<ChunkReference> sources = [];
                    await foreach (var token in rag.AskStreamingAsync(
                                       request.Question,
                                       s =>
                                       {
                                           sources = s;
                                           return Task.CompletedTask;
                                       },
                                       ct))
                    {
                        await context.Response.WriteAsync($"data: {token}\n\n", ct);
                        await context.Response.Body.FlushAsync(ct);
                    }

                    var sourcesJson = JsonSerializer.Serialize(sources);
                    await context.Response.WriteAsync($"event: sources\ndata: {sourcesJson}\n\n", ct);
                    await context.Response.Body.FlushAsync(ct);
                })
            .RequireRateLimiting("adaptive");
        app.Run();
    }
}