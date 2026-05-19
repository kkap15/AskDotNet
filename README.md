# AskDotNet

A production-grade RAG (Retrieval-Augmented Generation) chatbot over Microsoft Learn C# documentation. Ask questions in natural language and get grounded, cited answers streamed in real time.

**Live:** [askdotnet.vercel.app](https://askdotnet.vercel.app)

---

## Architecture

```
Microsoft Learn C# Docs
         │
         ▼
┌─────────────────────┐
│  AskDotNet.Ingest   │  Crawls pages, extracts HTML → Markdown,
│  (Console App)      │  structure-aware chunking at H2/H3 boundaries
└─────────────────────┘
         │ chunks.json
         ▼
┌─────────────────────┐
│  AskDotNet.Embed    │  Batch embeds chunks via Azure OpenAI
│  (Console App)      │  text-embedding-3-small (1536 dimensions)
└─────────────────────┘
         │ vectors
         ▼
┌─────────────────────┐
│  PostgreSQL         │  pgvector HNSW index, cosine similarity
│  (Azure)            │  vector(1536) column
└─────────────────────┘
         │
         ▼
┌─────────────────────┐
│  AskDotNet.Rag      │  Embeds query → retrieves top-K chunks
│  (Class Library)    │  → grounded prompt → streams GPT-4o-mini
└─────────────────────┘
         │ SSE stream
         ▼
┌─────────────────────┐
│  AskDotNet.Web      │  ASP.NET Core minimal API
│  (Azure Container   │  POST /api/chat — Auth0 JWT, rate limiting
│   Apps)             │  Server-Sent Events streaming
└─────────────────────┘
         │ HTTPS
         ▼
┌─────────────────────┐
│  React 19 Frontend  │  Auth0 login, real-time SSE consumption,
│  (Vercel)           │  ReactMarkdown, clickable citations
└─────────────────────┘
```

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | React 19, Vite, TypeScript, Tailwind CSS, PWA (installable) |
| Backend | ASP.NET Core 10, Minimal APIs, SSE streaming |
| AI | Azure OpenAI (text-embedding-3-small, gpt-4o-mini) |
| Vector DB | PostgreSQL + pgvector (HNSW, cosine similarity) |
| Auth | Auth0 (PKCE flow, JWT bearer) |
| Rate Limiting | Token bucket, per-user |
| Hosting | Azure Container Apps (API) + Vercel (frontend) |
| Ingestion | Markdig AST, AngleSharp, ReverseMarkdown, Polly |
| Tokenizer | Microsoft.ML.Tokenizers (cl100k_base) |

---

## Project Structure

```
AskDotNet/
├── backend/
│   ├── AskDotNet.Core/          # Shared records: Chunk, Page, RagResponse, ChunkReference
│   ├── AskDotNet.Ingest/        # Crawler, ContentExtractor, Chunker
│   ├── AskDotNet.Embed/         # EmbeddingService, DatabaseService, EmbedWorker
│   ├── AskDotNet.Rag/           # RagService, DatabaseHelper, RagHelper
│   ├── AskDotNet.RagCli/        # CLI test harness
│   ├── AskDotNet.Web/           # ASP.NET Core API + Dockerfile
│   └── AskDotNet.Eval/          # Eval suite, LLM-as-judge, Markdown report
├── frontend/                    # React 19 + Vite + Auth0
├── data/
│   ├── output.json              # Chunked corpus (gitignored)
│   ├── golden-set.json          # 6 eval questions
│   └── eval-report.md           # Latest eval results
└── README.md
```

---

## Evaluation Results

Evaluated against 6 golden-set questions covering structs, enums, generics, nullable types, lambdas, and the C# 14 `field` keyword.

| Metric | Value |
|--------|-------|
| Retrieval Recall@5 | 83% (5/6) |
| Average Generation Score | 3.0/5 |
| Grounded | 67% (4/6) |
| Addresses Question | 67% (4/6) |

**Known issues:**
- Q2 (`field` keyword): chunk exists but similarity score (45%) falls below the 0.5 retrieval threshold
- Q4 (expression lambdas): retrieval miss — question wording retrieves LINQ pages instead of the lambda-expressions reference page

Full results: [`data/eval-report.md`](data/eval-report.md)

---

## How It Works

### 1. Ingestion (`AskDotNet.Ingest`)

Crawls Microsoft Learn C# documentation using `HttpClient` + `Polly` resilience. Pages are parsed with `AngleSharp`, converted to Markdown via `ReverseMarkdown`, then chunked using a Markdig AST walker:

- Primary boundary: H2 headings
- Fallback: H3 if section exceeds 800 tokens
- Token counting: exact `cl100k_base` tokenizer via `Microsoft.ML.Tokenizers`
- Output: `data/output.json` with `Chunk` records

### 2. Embedding (`AskDotNet.Embed`)

Batch-embeds chunks using Azure OpenAI `text-embedding-3-small` (1536 dimensions). Idempotent — checks existing IDs before embedding. Stores vectors in PostgreSQL with a pgvector HNSW index using cosine distance (`vector_cosine_ops`).

### 3. Retrieval + Generation (`AskDotNet.Rag`)

```
Question → embed → vector similarity search (top-K, cosine) → filter (≥0.5)
        → BuildContext → grounded prompt → GPT-4o-mini stream → IAsyncEnumerable<string>
```

Grounded prompt: the LLM is instructed to answer only from retrieved documentation excerpts and say "I don't have enough information" if the answer isn't in the sources.

### 4. Web API (`AskDotNet.Web`)

Single endpoint: `POST /api/chat`

- **Auth:** Auth0 JWT bearer validation
- **Rate limiting:** Token bucket, 10 requests/minute per user
- **Streaming:** `text/event-stream` SSE — one `data:` event per token, final `event: sources` with citation JSON
- **CORS:** Configured for Vercel frontend

### 5. Frontend (`frontend/`)

- Auth0 PKCE login flow
- `fetch` + `ReadableStream` SSE consumer (EventSource doesn't support POST)
- `ReactMarkdown` for formatted answers with code blocks
- Clickable citation sources with similarity scores
- PWA manifest + icons — installable on iOS and Android via "Add to Home Screen"

---

## Local Setup

### Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker
- PostgreSQL with pgvector extension
- Azure OpenAI resource with `text-embedding-3-small` and `gpt-4o-mini` deployments
- Auth0 account

### 1. Clone

```bash
git clone https://github.com/kkap15/AskDotNet.git
cd AskDotNet
```

### 2. Configure secrets

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://YOUR-RESOURCE.openai.azure.com/" --project backend/AskDotNet.Embed
dotnet user-secrets set "AzureOpenAI:ApiKey" "YOUR-API-KEY" --project backend/AskDotNet.Embed
dotnet user-secrets set "AzureOpenAI:DeploymentName" "text-embedding-3-small" --project backend/AskDotNet.Embed
dotnet user-secrets set "AzureOpenAI:ChatDeploymentName" "gpt-4o-mini" --project backend/AskDotNet.Embed
dotnet user-secrets set "Postgres:ConnectionString" "Host=...;Database=askdotnet;..." --project backend/AskDotNet.Embed
dotnet user-secrets set "Auth0:Domain" "YOUR-DOMAIN.auth0.com" --project backend/AskDotNet.Web
dotnet user-secrets set "Auth0:Audience" "https://askdotnet-api" --project backend/AskDotNet.Web
```

### 3. Set up PostgreSQL

```sql
CREATE DATABASE askdotnet;
\c askdotnet
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE chunks (
    id TEXT PRIMARY KEY,
    source_url TEXT NOT NULL,
    source_title TEXT NOT NULL,
    heading_path TEXT[] NOT NULL,
    section_heading TEXT NOT NULL,
    content TEXT NOT NULL,
    token_count INTEGER NOT NULL,
    embedding vector(1536)
);

CREATE INDEX ON chunks USING hnsw (embedding vector_cosine_ops);
```

### 4. Ingest + embed

```bash
dotnet run --project backend/AskDotNet.Ingest
dotnet run --project backend/AskDotNet.Embed
```

### 5. Run the API

```bash
dotnet run --project backend/AskDotNet.Web
```

### 6. Run the frontend

```bash
cd frontend
cp .env.example .env.local  # fill in your values
npm install
npm run dev
```

Open `http://localhost:5173`

### 7. Run the eval suite

```bash
dotnet run --project backend/AskDotNet.Eval
# Results written to data/eval-report.md
```

---

## CLI Test (no frontend needed)

```bash
dotnet run --project backend/AskDotNet.RagCli
```

```
AskDotNet — C# Documentation Assistant
Type a question, or press Enter to exit.

Question: What is a struct in C#?
Answer: A struct in C# is a value type...
Sources:
  [70%] C# structs > C# structs
        https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/structs
```

---

## CI / CD

Pushes to `main` trigger `.github/workflows/deploy.yml`, which:

1. **Builds and tests** the .NET solution (`dotnet build` + `dotnet test`)
2. **Builds a Docker image** for `AskDotNet.Web` and pushes it to Azure Container Registry (`askdotnetacr.azurecr.io`)
3. **Deploys the API** to Azure Container Apps (`askdotnet-web` in `askdotnet-rg`) via `az containerapp update`
4. **Deploys the frontend** to Vercel (`vercel --prod`) using `vercel.json` — build runs `cd frontend && npm run build`, output from `frontend/dist`

Required GitHub secrets: `AZURE_CREDENTIALS`, `VERCEL_TOKEN`, `VERCEL_ORG_ID`, `VERCEL_PROJECT_ID`.

---

## Key Design Decisions

**Structure-aware chunking over fixed-size splits** — H2 headings are semantic boundaries in documentation. Splitting at heading boundaries preserves context; fixed-size splits break mid-concept.

**`IAsyncEnumerable<string>` for streaming** — enables token-by-token forwarding from the LLM to the HTTP response without buffering the entire answer.

**SSE over WebSockets** — the chat interaction is unidirectional after question submission (server → client only). SSE is simpler, works over standard HTTP/2, and is natively supported by browsers.

**Raw Npgsql over EF Core** — pgvector's `<=>` cosine distance operator has no EF Core LINQ translation. Raw SQL gives full control over the similarity search query.

**LLM-as-judge evaluation** — a separate LLM call evaluates answer quality for grounded-ness and relevance, capturing meaning rather than keyword presence.

---

## License

MIT