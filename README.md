# docs-rag-hobby

A hobby project where I learn RAG (Retrieval-Augmented Generation) by building it properly, step by step, in C#.

Ask a question → it finds the most relevant bits of the Npgsql docs → an LLM answers using only those bits (with sources). No vibes-based answers.

## why this exists

I've built apps that call LLMs. Now I want to understand what happens when the model needs to answer from *your* data. RAG is how most real AI features in industry work, so I'm learning it with the tools people actually use, not gimmicks.

## how it works

```
INGESTION (run once)
docs → split into chunks → turn chunks into embeddings → store in Postgres

QUERY (every question)
question → embedding → find closest chunks → give them to the LLM → answer + sources
```

## stack

| What | Tool |
|---|---|
| Language | C# / .NET |
| Vector database | PostgreSQL + pgvector (via Docker) |
| Embeddings + chat model | OpenAI |
| AI abstractions | Microsoft.Extensions.AI |
| DB access | Npgsql |
| Evals (later) | Promptfoo |

## roadmap

- [ ] 1. Postgres + pgvector running in Docker
- [ ] 2. Ingestion: load docs, chunk, embed, store
- [ ] 3. Retrieval only: question in, top matching chunks out
- [ ] 4. Generation: grounded answers with citations + "I don't know"
- [ ] 5. Evals: golden question set, measure retrieval quality
- [ ] 6. Level up: chunking experiments, HNSW index, hybrid search, reranking

## running it

Setup instructions coming once step 1 is done.

You'll need Docker, the .NET SDK, and an OpenAI API key (set as an environment variable, never committed).

## notes

This is a learning project, built in small steps. Expect the early commits to be rough and the git history to show the journey.
