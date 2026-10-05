namespace DocsRag.Ingestion;

public sealed record DocumentChunk(
    string SourcePath,
    int ChunkIndex,
    string HeadingPath,
    string Content);