using System.Reflection;
using DocsRag.Query;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OpenAI;
using Pgvector.Npgsql;

const string EmbeddingModelName = "text-embedding-3-small";
const int TopK = 5;
const int PreviewCharacters = 200;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets(Assembly.GetExecutingAssembly())
    .AddEnvironmentVariables()
    .Build();

var openAiApiKey = configuration["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured. Set it with 'dotnet user-secrets set'.");

var postgresConnectionString = configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured. Set it with 'dotnet user-secrets set'.");

using IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OpenAIClient(openAiApiKey)
    .GetEmbeddingClient(EmbeddingModelName)
    .AsIEmbeddingGenerator();

var dataSourceBuilder = new NpgsqlDataSourceBuilder(postgresConnectionString);
dataSourceBuilder.UseVector();
await using var dataSource = dataSourceBuilder.Build();

var chunkSearcher = new ChunkSearcher(embeddingGenerator, dataSource);

while (true)
{
    Console.Write("\nQuestion (empty to quit): ");
    var question = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question))
    {
        break;
    }

    var results = await chunkSearcher.SearchAsync(question, TopK);

    foreach (var result in results)
    {
        var singleLineContent = result.Content.ReplaceLineEndings(" ");
        var preview = singleLineContent[..Math.Min(PreviewCharacters, singleLineContent.Length)];

        Console.WriteLine($"\n[{result.Similarity:F3}] {result.SourcePath} | {result.HeadingPath}");
        Console.WriteLine($"    {preview}");
    }
}