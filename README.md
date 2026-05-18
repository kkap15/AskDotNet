# AskDotNet

A .NET pipeline for building a Q&A assistant over Microsoft Learn documentation. Crawls pages, extracts clean content, produces token-aware chunks, stores vector embeddings in PostgreSQL, and answers natural-language questions via a streaming REST API or interactive CLI — with source citations powered by Azure OpenAI.

## What it does

1. **Crawls** Microsoft Learn documentation pages with rate limiting and retry logic
2. **Extracts** the main content — strips navigation, feedback sections, and other noise — and converts to Markdown
3. **Chunks** the Markdown by heading structure (H2/H3), keeping each chunk within a configurable token budget
4. **Outputs** a JSON file of chunks with full metadata
5. **Embeds** each chunk using Azure OpenAI (`text-embedding-3-small`), batching 100 chunks per API call
6. **Stores** embeddings in PostgreSQL (pgvector) with idempotent upsert — safe to re-run
7. **Answers** natural-language questions by embedding the query, retrieving the top-10 similar chunks (cosine similarity > 0.5), and streaming a response with `gpt-4o-mini` — available via REST API or interactive CLI

## Architecture

| Component | File | Responsibility |
|---|---|---|
| `Crawler` | `src/AskDotNet.Ingest/Crawler.cs` | HTTP fetching with Polly retries (3×) and 500 ms delay between requests |
| `ContentExtractor` | `src/AskDotNet.Ingest/ContentExtractor.cs` | AngleSharp + ReverseMarkdown — removes noise, extracts title and structured content |
| `Chunker` | `src/AskDotNet.Ingest/Chunker.cs` | Splits Markdown by H2/H3, enforces 100–800 token limits using cl100k_base tokenizer |
| `EmbedWorker` | `src/AskDotnet.Embed/EmbedWorker.cs` | Orchestrates embed pipeline; reads JSON, deduplicates by ID, batches embedding calls |
| `EmbeddingService` | `src/AskDotnet.Embed/Service/EmbeddingService.cs` | Azure OpenAI client; generates float[] vectors in batch |
| `DatabaseService` | `src/AskDotnet.Embed/Service/DatabaseService.cs` | Npgsql + pgvector; idempotent chunk + embedding insertion |
| `RagService` | `src/AskDotNet.Rag/Service/RagService.cs` | Embeds query, retrieves top-10 chunks by cosine similarity, generates answer via gpt-4o-mini |
| `RagWorker` | `src/AskDotNet.RagCli/RagWorker.cs` | Interactive CLI loop; formats answers with source citations and similarity scores |
| Web API | `src/AskDotNet.Web/Program.cs` | ASP.NET Core minimal API; `POST /api/chat` streams tokens via SSE with rate limiting (10 req/s) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL ≥ 15 with the [pgvector extension](https://github.com/pgvector/pgvector)
- Azure OpenAI resource with a `text-embedding-3-small` deployment and a `gpt-4o-mini` deployment

## Database setup

```sql
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE chunks (
  id TEXT PRIMARY KEY,
  source_url TEXT,
  source_title TEXT,
  heading_path TEXT[],
  section_heading TEXT,
  content TEXT,
  token_count INT,
  embedding vector(1536)
);
```

## Setup & run

```bash
git clone <repo-url>
cd AskDotNet

# Phase 1: Ingest — crawl, extract, chunk
dotnet run --project src/AskDotNet.Ingest
# → writes data/output.json

# Phase 2: Embed — generate and store vectors
dotnet run --project src/AskDotnet.Embed
# → reads data/output.json, inserts chunks + embeddings into PostgreSQL

# Phase 3a: Web API
dotnet run --project src/AskDotNet.Web
# → POST http://localhost:5253/api/chat  (streaming SSE)

# Phase 3b: Interactive CLI (alternative)
dotnet run --project src/AskDotNet.RagCli
# → prompts for questions, prints answers with source citations
```

## Configuration

**Ingest** (`src/AskDotNet.Ingest/Program.cs` and `Chunker.cs`):
- **Seed URLs** — edit the list in `Program.cs`
- **Token limits** — `MinTokens` / `MaxTokens` in `Chunker.cs` (defaults: 100 / 800)

**Embed** — set user secrets from the `src/AskDotnet.Embed` directory:

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Database=askdotnet;Username=...;Password=..."
```

Additional settings in `src/AskDotnet.Embed/appsettings.json`:

| Key | Default | Description |
|---|---|---|
| `AzureOpenAI:DeploymentName` | `text-embedding-3-small` | Embedding model deployment name |
| `Ingest:ChunksPath` | `../../../../../data/output.json` | Path to ingest output |
| `Ingest:BatchSize` | `100` | Chunks per embedding API call |

**Web API** — set user secrets from the `src/AskDotNet.Web` directory:

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
dotnet user-secrets set "Postgres:ConnectionString" "Host=localhost;Database=askdotnet;Username=...;Password=..."
# For Azure Database for PostgreSQL add SSL flags:
# "Host=...;Database=askdotnet;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true"

# Optional — only needed when JWT auth is enabled (.RequireAuthorization()):
dotnet user-secrets set "Auth0:Domain" "<your-auth0-tenant>.us.auth0.com"
dotnet user-secrets set "Auth0:Audience" "https://<your-api-identifier>"
```

Additional settings in `src/AskDotNet.Web/appsettings.json`:

| Key | Default | Description |
|---|---|---|
| `AzureOpenAI:DeploymentName` | `text-embedding-3-small` | Embedding model deployment name |
| `AzureOpenAI:ChatDeploymentName` | `gpt-4o-mini` | Answer generation model deployment name |

**RAG CLI** — set user secrets from the `src/AskDotNet.RagCli` directory:

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://<your-resource>.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "<your-key>"
dotnet user-secrets set "Postgres:ConnectionString" "Host=localhost;Database=askdotnet;Username=...;Password=..."
```

Additional settings in `src/AskDotNet.RagCli/appsettings.json`:

| Key | Default | Description |
|---|---|---|
| `AzureOpenAI:DeploymentName` | `text-embedding-3-small` | Embedding model deployment name |
| `AzureOpenAI:ChatDeploymentName` | `gpt-4o-mini` | Answer generation model deployment name |

## Output format

Each entry in `output.json` is a `Chunk`:

```json
{
  "id": "abc123",
  "sourceUrl": "https://learn.microsoft.com/en-us/dotnet/csharp/...",
  "sourceTitle": "Page title",
  "headingPath": [".NET", "C#", "Fundamentals"],
  "sectionHeading": "Value types",
  "content": "...",
  "tokenCount": 312
}
```

## API

**`POST /api/chat`**

Request:
```json
{ "question": "What is the difference between a struct and a class in C#?" }
```

Response (`text/event-stream`):
```
data: A struct is a value type...

data:  while a class is a reference type...

event: sources
data: [{"sourceUrl":"https://learn.microsoft.com/...","sourceTitle":"...","sectionHeading":"...","similarity":0.87}]
```

Individual answer tokens are streamed as `data:` events. A final `event: sources` event delivers the cited chunks with similarity scores.

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
| Azure.AI.OpenAI | Azure OpenAI embedding client |
| Microsoft.Extensions.AI | AI abstraction layer |
| Npgsql + pgvector | PostgreSQL adapter with vector support |
| ASP.NET Core (Minimal APIs) | REST API with SSE streaming and rate limiting |
| Microsoft.AspNetCore.Authentication.JwtBearer | JWT auth (configured, disabled by default) |
