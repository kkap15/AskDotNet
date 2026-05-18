using System.Text.Json.Serialization;

namespace AskDotNet.Core.Records;

public sealed record ChatRequest([property: JsonPropertyName("question")] string Question);