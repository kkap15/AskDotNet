namespace AskDotNet.Core.Records;

public sealed record EvalResult(
    string Question,
    bool RetrievalHit,
    double TopSimilarity,
    bool Grounded,
    bool AddressesQuestion,
    int Score,
    string Answer,
    string JudgeReason
);