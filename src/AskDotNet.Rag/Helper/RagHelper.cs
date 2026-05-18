using System.Text;
using Microsoft.Extensions.AI;

namespace AskDotNet.Rag.Helper;

public static class RagHelper
{
    public static string BuildContext(
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

    public static async Task<string> GenerateAnswerAsync(string question, string context, IChatClient chatClient, 
        CancellationToken ct)
    {
        var messages = BuildMessages(question, context);
        var response = await chatClient.GetResponseAsync(messages, null, ct);

        return response.Text;
    }
    
    public static List<ChatMessage> BuildMessages(string question, string context)
    {
        const string systemPrompt = """
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

        return messages;
    }
}