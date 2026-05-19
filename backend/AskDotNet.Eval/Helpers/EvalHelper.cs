using System.Text;
using System.Text.Json;
using AskDotNet.Core.Records;
using Microsoft.Extensions.AI;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace AskDotNet.Eval.Helpers;

public static class EvalHelper
{
    public static async Task<JudgeScore> JudgeAsync(string question, string expectedFact, string answer,
        IReadOnlyList<ChunkReference> sources, IChatClient chatClient, CancellationToken cancellationToken)
    {
        var sourceList = string.Join("\n", sources.Select(s =>
            $"- {s.SourceTitle} > {s.SectionHeading} ({s.Similarity:P0}): {s.SourceUrl}"));

        var jsonFormat = """
                         {
                            "grounded": true,
                            "addresses_question": true,
                            "score": 4,
                            "reasoning": "explanation here"
                         }
                         """;
        var prompt = $"""
                      You are an evaluation judge for a RAG (Retrieval-Augmented Generation) system.
                      
                      Evaluate the following answer based on these criteria:
                      1. Grounded: Is the answer based on the information that would appear in C# documentation? (true/false)
                      2. Addresses Question: Does the answer directly address the question asked? (true/false)
                      3. Score: Overall quality score from 1-5 (5 = excellent, 1 = poor)
                      4. Reasoning: Brief explanation of your scores
                      
                      Question: {question}
                      Expected key fact: {expectedFact}
                      Retrieved Sources: {sourceList}
                      Answer to evaluate: {answer}
                      
                      Respond ONLY with valid JSON in this exact format:
                      {jsonFormat}
                      """;

        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, prompt)
        };

        var response = await chatClient.GetResponseAsync(messages, null, cancellationToken);
        var responseText = response.Text.Trim();

        if (responseText.StartsWith("```"))
        {
            responseText = responseText
                .Split('\n')
                .Skip(1)
                .TakeWhile(l => !l.StartsWith("```"))
                .Aggregate((a, b) => $"{a}\n{b}");
        }

        try
        {
            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;
            return new JudgeScore(
                Grounded: root.GetProperty("grounded").GetBoolean(),
                AddressesQuestion: root.GetProperty("addresses_question").GetBoolean(),
                Score: root.GetProperty("score").GetInt32(),
                Reasoning: root.GetProperty("reasoning").GetString() ?? string.Empty);
        }
        catch
        {
            return new JudgeScore(false, false, 0, $"Parse failed: {responseText}");
        }
    }
    
    public static string GenerateReport(List<EvalResult> results)
    {
        var builder = new StringBuilder();

        builder.AppendLine("#  AskDotNet Evaluation Report");
        builder.AppendLine($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC");
        builder.AppendLine($"Questions evaluated: {results.Count}");
        builder.AppendLine();
        
        var retrievalRecall = results.Count(r => r.RetrievalHit) / (double)results.Count;
        var avgScore = results.Average(r => r.Score);
        var groundedPct = results.Count(r => r.Grounded) / (double)results.Count;
        var addressesPct = results.Count(r => r.AddressesQuestion) / (double)results.Count;
        
        builder.AppendLine("## Summary");
        builder.AppendLine($"| Metric | Value |");
        builder.AppendLine($"|--------|-------|");
        builder.AppendLine($"|  Retrieval Recall@K | {retrievalRecall:P0} |");
        builder.AppendLine($"|  Average Score | {avgScore:F1}/5 |");
        builder.AppendLine($"|  Grounded | {groundedPct:P0} |");
        builder.AppendLine($"|  Addresses Question | {addressesPct:P0} |");
        builder.AppendLine();
        
        builder.AppendLine("## Per Question Results");
        builder.AppendLine();

        for (var i = 0; i < results.Count; ++i)
        {
            var r = results[i];
            builder.AppendLine($"### {i + 1}. {r.Question}");
            builder.AppendLine($"- **Retrieval Hit:** {(r.RetrievalHit ? "✅" : "❌")}  ");
            builder.AppendLine($"- **Top Similarity:** {r.TopSimilarity:P0}   ");
            builder.AppendLine($"- **Grounded:** {(r.Grounded ? "✅" : "❌")}  ");
            builder.AppendLine($"- **Addresses Question:** {(r.AddressesQuestion ? "✅" : "❌")}  ");
            builder.AppendLine($"- **Score:** {r.Score}/5  ");
            builder.AppendLine($"- **Judge Reasoning:** {r.JudgeReason}  ");
            builder.AppendLine();
            builder.AppendLine("**Answer:**");
            builder.AppendLine($"> {r.Answer.Replace("\n", "\n> ")}");
            builder.AppendLine();
        }

        return builder.ToString();
    }
}