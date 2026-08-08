# Embedding Models & Providers for Production (as of Aug 2026)

**Scope:** cheapest + most stable embedding APIs and self-hostable options for production.
**Method:** every number below was cross-checked against the provider's official pricing page on 2026-08-08 where reachable. Where a page was unreachable or a figure could not be confirmed, the value is explicitly marked **unverified** — nothing was invented.

> Prices are **per 1M input tokens** unless otherwise noted.

---

## 1. Head-to-head (hosted since embeddings are input-only, price = input price)

| Provider / Model | $ / 1M tokens (input) | Dim | Context | Notes / source |
|---|---|---|---|---|
| **OpenAI** `text-embedding-3-small` | **$0.02** | 512–1,536 | 8,191 | Source: OpenAI platform pricing page |
| **OpenAI** `text-embedding-3-large` | $0.13 | 256–3,072 | 8,191 | Source: OpenAI platform pricing page |
| **OpenAI** `text-embedding-ada-002` (legacy) | $0.10 | 1,536 | 8,191 | Source: OpenAI platform pricing page |
| **Voyage** `voyage-4-lite` | **$0.02** | 384 | — | Source: Voyage docs pricing |
| **Voyage** `voyage-4` | $0.06 | — | — | Source: Voyage docs pricing |
| **Voyage** `voyage-4-large` | $0.12 | — | — | Source: Voyage docs pricing |
| **Voyage** `voyage-3-large` | $0.18 | — | — | "Older models" table, Voyage docs |
| **Voyage** `voyage-3` | $0.06 | 1,024 | 32k | "Older models" table, Voyage docs |
| **Voyage** `voyage-3-lite` | $0.02 | — | — | "Older models" table, Voyage docs |
| **Voyage** `voyage-3-small` | *unverified* | — | — | Not listed on current page (Voyage moved to voyage-3.5 / voyage-4 lines) |
| **Google** Gemini Embedding (`text-embedding-001`) | **$0.15** online / $0.12 batch | 768 / 3,072 | 2,048 | Source: Google Vertex AI pricing; $0.00015/1k tokens |
| **Google** Gemini Embedding 2 (text) | $0.20 online / $0.10 batch | — | — | Source: Google Vertex AI pricing |
| **Mistral** `mistral-embed` | **$0.10** | 768 | 512–4k | Source: Mistral API pricing |
| **Mistral** `codestral-embed` | $0.15 | — | — | Source: Mistral API pricing |
| **Jina** `jina-embeddings-v5-text-*` | *unverified $/token* | 1024 (small) / 768 (nano) | 32k / 8k | Token top-up model; price/token not shown on page. Free trial tier exists. Source: jina.ai/embeddings + docs |
| **Cohere** `embed-english-v3` / `embed-multilingual-v3` | *unverified* | — | — | per-token API pricing no longer on public page; now sold via Model Vault / Bedrock. Source: Cohere pricing |
| **Amazon Bedrock** Amazon Titan Text Embeddings v2 | *unverified* | 1,024/1,536 | 8k | On-demand not rendered on fetched AWS page. Source: Bedrock pricing |
| **Amazon Bedrock** Cohere Embed 3 (Provisioned) | $7.12 / hr (no commit) | — | — | Source: Bedrock pricing (per-hour, not per-token) |

Note: Anthropic offers **no** embedding model and is excluded.

---

## 2. Provider-by-provider

### OpenAI
- `text-embedding-3-small` **$0.02/M**; `-large` **$0.13/M**; `ada-002` **$0.10/M** (legacy, fewer revisions - consider migrating to v3-small).
  *Source: platform.openai.com/docs/pricing (Specialized Models → Embedding)*
- Very mature, reliable, widely integrated. Default rate limits vary by account (not published on the pricing page) — plan around TPM/RPM tiers for bulk jobs.
- Best choice when you already use OpenAI and want the lowest operational overhead.

### Voyage AI
- Flagship now **voyage-4 family** (updated ~Jul 2026): `voyage-4` $0.06/M, `voyage-4-lite` $0.02/M, `voyage-4-large` $0.12/M.
  *Source: docs.voyageai.com/docs/pricing*
- First **200M tokens free** for the voyage-4 family (per account) — the most generous free credit of any hosted embedding vendor.
- Older `voyage-3-large` $0.18/M, `voyage-3` $0.06/M, `voyage-3-lite` $0.02/M still listed under "Older models". `voyage-3-small` is **no longer on the page**.
- Strong retrieval/rerank quality; good for RAG-heavy workloads. Batch endpoint = **33% discount**.

### Google Gemini
- Gemini Embedding (`text-embedding-001`) **$0.15/M online, $0.12/M batch**. Output dimensions 768 (economy) or 3072 (quality), 2048-token input ceiling.
  *Source: cloud.google.com/vertex-ai/generative-ai/pricing*
- Gemini Embedding 2 (multimodal) text = $0.20/M online / $0.10/M batch.
- Note the **2048-token input cap** = you must chunk anyway; fine for short-passage RAG.

### Cohere
- Public per-token Embed pricing is **no longer listed** on cohere.com/pricing (page now sells North/Compass + **Model Vault**: Embed 4 Small $4/hr, $2,500/mo).
  *Source: cohere.com/pricing*
- On Bedrock: Cohere Embed 3 Provisioned Throughput $7.12/hr (no commitment).
  *Source: aws.amazon.com/bedrock/pricing*
- `embed-multilingual-v3.0` historical ~$0.10/M is **unverified** against a live page — do not rely on it without checking the Cohere dashboard.

### Mistral
- `mistral-embed` **$0.10/M** input; `codestral-embed` (code) **$0.15/M**.
  *Source: mistral.ai/pricing/api/*
- `mistral-embed` outputs 768-dim, designed for 512–4k token texts. Competitive mid-tier pricing.

### Amazon Bedrock
- Titan **Text Embeddings v2** on-demand price was not rendered on the fetched page → **unverified**. Multi-provider access (Amazon Titan, Cohere, Mistral, Jina via SageMaker, etc.) in one console — good if you're already AWS-native and want single billing.

### Jina AI
- Latest: **jina-embeddings-v5-text-small** (677M, 1024-dim, **32K context**, Qwen3 backbone) and **v5-text-nano** (239M, 768-dim, 8K). Also `jina-embeddings-v4` (multimodal, 2048-dim).
  *Source: jina.ai/embeddings + docs.jina.ai*
- Billing is **token top-up** rather than a published $/1M figure → $/1M **unverified** from the page. Historically among the cheapest ($0.02/M class), but confirm in-dashboard before committing.
- Rate limits (verified): free / trial 100 RPM & 100K TPM; with free key 500 RPM & 2M TPM; premium 5,000 RPM & 50M TPM.
  *Source: jina.ai/embeddings (rate-limit table)*

---

## 3. Self-hosted / open-source options

Self-hosting gives **effectively $0/1M marginal token cost** past fixed infra; it wins at scale. All below are open weights (Apache/MIT) runnable via sentence-transformers / vLLM / TEI (Text Embeddings Inference).

| Model | Size | Dim | Context | Notes |
|---|---|---|---|---|
| **BAAI/bge-m3** | 568M | 1024 | 8192 | Multi-lingual SOTA, dense+sparse+multi-vector; opensource flagship |
| **Snowflake Arctic Embed** (`snowflake-arctic-embed`) | 109M–22M (L/M/S) | 768/384 | 512 | Tiny, cheap, tuned for RAG; excellent size/quality tradeoff |
| **Qwen3-Embedding-8B** | 8B | 4096 | 32K | Newest SOTA open embed (Matryoshka); bigger = better, more GPU |
| **GTE** (Alibaba) | <0.35B | 768+ | 8192 | Strong multilingual, lightweight |
| **sentence-transformers** (all-MiniLM-L6-v2 etc.) | 22M–90M | 384–768 | 512 | Baseline `all-MiniLM-L6-v2` = free, tiny, 384-dim |
| **jina-embeddings-v3 / v5** | 570M / 677M | 1024 | 8k / 32k | Open weights also self-hostable |

**Hosting cost reality check:** a modest CPU/GPU node (e.g. ~$10–$30/mo) serves `all-MiniLM` or `arctic-embed-s` at ~1M+ tokens/minute with batching. Break-even vs a $0.02–0.10/M API is typically reached at **low millions of tokens/day**. If you only embed a few thousand documents/month, the hosted API (even at $0.10/M) is cheaper once you account for the ops cost of self-hosting.

---

## 4. Recommendation

### (a) Hosted API — lowest cost + stability
**OpenAI `text-embedding-3-small` at $0.02/M.** Cheapest fully-managed, highest-production-proven tier; 512-dim is plenty for most RAG, and you can reduce to 256-dim via Matryoshka if storage is a concern. Runner-up: **Voyage `voyage-4-lite` $0.02/M** (200M free tokens, arguably better retrieval quality per $), pick it if you don't already depend on the OpenAI platform.

### (b) Hosted API — best quality
**Voyage `voyage-4-large` ($0.12/M) or Google Gemini Embedding 2 / 3072-dim.** Voyage is the retrieval-quality leader (built for RAG/rerank), and its cruise models are priced reasonably. If you want a second, top-tier open-weight-quality option under one big cloud, Gemini Embedding 2 (3072-dim) is the Google choice. For most applications, though, the quality delta over a good $0.02–0.06/M model is small — spend the difference on a reranker instead.

### (c) Self-hosted — lowest cost at scale
**Snowflake `snowflake-arctic-embed-s/m` (or `all-MiniLM-L6-v2` for zero-arg minimum) via sentence-transformers / TEI.** Tiny models, negligible GPU, marginal cost ≈ $0/M past fixed infra. If you need multilingual + long context at scale, **BAAI `bge-m3`**. If outright SOTA matters and you have a GPU, **`Qwen3-Embedding-8B`** (~4096-dim) but that's likely overkill for cost-first production.

**Practical guidance:** start production on OpenAI `text-embedding-3-small` (or Voyage lite) for speed-to-market and stability, then move embeddings to a self-hosted `arctic-embed`/`bge-m3` once sustained volume justifies the infra. In all cases, **store the 512/1024-dim vector, add a reranker for retrieval quality, and test recall on your own data** — MTEB leaderboards don't predict your domain.

---

## Bottom line
- **Cheapest hosted, most stable:** OpenAI `text-embedding-3-small` — **$0.02/M** (508x cheaper per token than most reasoning LLMs; verified).
- **Best quality hosted:** Voyage `voyage-4-large` ($0.12/M) / Gemini Embedding 2 (3072-dim, $0.20/M).
- **Lowest cost at scale:** self-host **`snowflake-arctic-embed`** or **`bge-m3`** (~$0/M marginal past fixed infra); `Qwen3-Embedding-8B` if you need peak open-source quality on a GPU.
- **Most generous free tier:** Voyage voyage-4 family — **200M free tokens** per account.
- **Verified:** OpenAI (all 3), Voyage (3-large/3/3-lite + 4 family), Gemini 001 & Embedding 2, Mistral (embed + codestral-embed), Jina (models/dims/rate-limits; not $/token), Cohere Model Vault, Bedrock Cohere PT.
- **Unverified / not on live page:** Voyage `voyage-3-small`, Cohere embed-v3 $/token, Amazon Titan Text Embeddings v2 on-demand $/token, Jina $/1M (token-top-up model). Verify these in-dashboard before relying on them.
