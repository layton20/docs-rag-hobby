namespace DocsRag.Query;

public sealed record RetrievedChunk(
    string SourcePath,
    string HeadingPath,
    string Content,
    double Similarity);