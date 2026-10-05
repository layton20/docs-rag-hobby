namespace DocsRag.Ingestion;

public sealed record EmbeddedChunk(DocumentChunk Chunk, ReadOnlyMemory<float> Embedding);