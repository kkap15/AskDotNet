# AskDotNet

A .NET documentation ingestion pipeline for RAG. Crawls Microsoft Learn pages, extracts clean content, and produces token-aware chunks ready for embedding and retrieval.

## What it does

1. **Crawls** Microsoft Learn documentation pages with rate limiting and retry logic
2. **Extracts** the main content — strips navigation, feedback sections, and other noise — and converts to Markdown
3. **Chunks** the Markdown by heading structure (H2/H3), keeping each chunk within a configurable token budget
4. **Outputs** a JSON file of chunks with full metadata, ready to embed

## Architecture

| Component | File | Responsibility |
|---|---|---|
| `Crawler` | `src/AskDotNet.Ingest/Crawler.cs` | HTTP fetching with Polly retries (3×) and 500 ms delay between requests |
| `ContentExtractor` | `src/AskDotNet.Ingest/ContentExtractor.cs` | AngleSharp + ReverseMarkdown — removes noise, extracts title and structured content |
| `Chunker` | `src/AskDotNet.Ingest/Chunker.cs` | Splits Markdown by H2/H3, enforces 100–800 token limits using cl100k_base tokenizer |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Setup & run

```bash
git clone <repo-url>
cd AskDotNet
dotnet run --project src/AskDotNet.Ingest
```

Output is written to `data/output.json`.

## Output format

Each entry in `output.json` is a `Chunk`:

```json
{
  "id": "abc123",
  "url": "https://learn.microsoft.com/en-us/dotnet/csharp/...",
  "title": "Page title",
  "headingPath": ".NET / C# / Fundamentals",
  "sectionHeading": "Value types",
  "content": "...",
  "tokenCount": 312
}
```

## Configuration

- **Seed URLs** — edit the list in `src/AskDotNet.Ingest/Program.cs`
- **Token limits** — `MinTokens` and `MaxTokens` constants in `src/AskDotNet.Ingest/Chunker.cs` (defaults: 100 / 800)

## Tech stack

| Library | Purpose |
|---|---|
| AngleSharp | HTML parsing |
| ReverseMarkdown | HTML → Markdown conversion |
| Markdig | Markdown processing |
| Polly | HTTP resilience and retries |
| Microsoft.ML.Tokenizers | cl100k_base token counting |
| Spectre.Console | Rich console output |
| Microsoft.Playwright | Browser automation (for JS-rendered pages) |
