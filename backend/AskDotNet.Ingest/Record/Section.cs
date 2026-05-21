using Markdig.Syntax;

namespace AskDotNet.Ingest.Record;

internal record Section(string Heading, List<Block> Blocks);