using Microsoft.Extensions.AI;
using Npgsql;
using Pgvector;

namespace DocsRag.Query;

public sealed class ChunkSearcher(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    NpgsqlDataSource dataSource)
{
    private const string SearchSql = """
        SELECT source_path, heading_path, content, 1 - (embedding <=> @queryEmbedding) AS similarity
        FROM document_chunks
        ORDER BY embedding <=> @queryEmbedding
        LIMIT @topK
        """;

    public async Task<IReadOnlyList<RetrievedChunk>> SearchAsync(
        string question,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var questionVector = await embeddingGenerator.GenerateVectorAsync(question, cancellationToken: cancellationToken);

        await using var command = dataSource.CreateCommand(SearchSql);
        command.Parameters.AddWithValue("queryEmbedding", new Vector(questionVector));
        command.Parameters.AddWithValue("topK", topK);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var retrievedChunks = new List<RetrievedChunk>();
        while (await reader.ReadAsync(cancellationToken))
        {
            retrievedChunks.Add(new RetrievedChunk(
                SourcePath: reader.GetString(0),
                HeadingPath: reader.GetString(1),
                Content: reader.GetString(2),
                Similarity: reader.GetDouble(3)));
        }

        return retrievedChunks;
    }
}