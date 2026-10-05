using Npgsql;
using Pgvector;

namespace DocsRag.Ingestion;

public sealed class ChunkRepository(NpgsqlDataSource dataSource)
{
    private const string TruncateChunksSql = "TRUNCATE document_chunks";

    private const string InsertChunkSql = """
        INSERT INTO document_chunks (source_path, chunk_index, heading_path, content, embedding)
        VALUES (@sourcePath, @chunkIndex, @headingPath, @content, @embedding)
        """;

    public async Task ReplaceAllAsync(
        IReadOnlyList<EmbeddedChunk> embeddedChunks,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var truncateCommand = new NpgsqlCommand(TruncateChunksSql, connection, transaction))
        {
            await truncateCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var embeddedChunk in embeddedChunks)
        {
            await using var insertCommand = new NpgsqlCommand(InsertChunkSql, connection, transaction);
            insertCommand.Parameters.AddWithValue("sourcePath", embeddedChunk.Chunk.SourcePath);
            insertCommand.Parameters.AddWithValue("chunkIndex", embeddedChunk.Chunk.ChunkIndex);
            insertCommand.Parameters.AddWithValue("headingPath", embeddedChunk.Chunk.HeadingPath);
            insertCommand.Parameters.AddWithValue("content", embeddedChunk.Chunk.Content);
            insertCommand.Parameters.AddWithValue("embedding", new Vector(embeddedChunk.Embedding));

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}