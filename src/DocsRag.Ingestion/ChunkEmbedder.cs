using Microsoft.Extensions.AI;

namespace DocsRag.Ingestion;

public sealed class ChunkEmbedder(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
{
    private const int ChunksPerRequest = 100;

    public async Task<IReadOnlyList<EmbeddedChunk>> EmbedAsync(
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        var embeddedChunks = new List<EmbeddedChunk>(chunks.Count);

        foreach (var batch in chunks.Chunk(ChunksPerRequest))
        {
            var textsToEmbed = batch.Select(BuildEmbeddingText).ToList();
            var embeddings = await embeddingGenerator.GenerateAsync(textsToEmbed, cancellationToken: cancellationToken);

            for (var index = 0; index < batch.Length; index++)
            {
                embeddedChunks.Add(new EmbeddedChunk(batch[index], embeddings[index].Vector));
            }
        }

        return embeddedChunks;
    }

    private static string BuildEmbeddingText(DocumentChunk chunk) =>
        $"{chunk.HeadingPath}\n\n{chunk.Content}";
}