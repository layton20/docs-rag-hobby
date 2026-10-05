namespace DocsRag.Ingestion;

public sealed class MarkdownDocumentLoader
{
    private const string MarkdownSearchPattern = "*.md";

    public IReadOnlyList<SourceDocument> LoadFrom(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
        {
            throw new DirectoryNotFoundException($"Corpus directory not found: {rootDirectory}");
        }

        return Directory
            .EnumerateFiles(rootDirectory, MarkdownSearchPattern, SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .Select(filePath => new SourceDocument(
                RelativePath: Path.GetRelativePath(rootDirectory, filePath).Replace('\\', '/'),
                Content: File.ReadAllText(filePath)))
            .ToList();
    }
}