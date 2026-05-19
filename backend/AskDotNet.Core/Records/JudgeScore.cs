namespace AskDotNet.Core.Records;

public sealed record JudgeScore(bool Grounded, bool AddressesQuestion, int Score, string Reasoning);