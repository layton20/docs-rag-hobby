using System.Text;
using System.Text.RegularExpressions;

namespace DocsRag.Ingestion;

public sealed partial class MarkdownChunker(int maximumChunkCharacters)
{
    private const string FrontMatterDelimiter = "---";
    private const string CodeFenceMarker = "```";
    private const string HeadingPathSeparator = " > ";
    private const string ParagraphSeparator = "\n\n";
    private const string LineSeparator = "\n";

    [GeneratedRegex(@"^(?<hashes>#{1,6})\s+(?<title>.+?)\s*$")]
    private static partial Regex HeadingPattern();

    public IReadOnlyList<DocumentChunk> Chunk(SourceDocument document)
    {
        var lines = document.Content.ReplaceLineEndings("\n").Split('\n');
        var bodyLines = SkipFrontMatter(lines);

        var chunks = new List<DocumentChunk>();
        var headingStack = new List<(int Level, string Title)>();
        var currentSectionLines = new List<string>();
        var isInsideCodeFence = false;

        void FlushCurrentSection()
        {
            var isHeadingOnly = currentSectionLines.All(
                line => string.IsNullOrWhiteSpace(line) || HeadingPattern().IsMatch(line));

            if (!isHeadingOnly)
            {
                var sectionText = string.Join('\n', currentSectionLines).Trim();
                var headingPath = string.Join(HeadingPathSeparator, headingStack.Select(heading => heading.Title));

                foreach (var piece in SplitToMaximumLength(sectionText))
                {
                    chunks.Add(new DocumentChunk(document.RelativePath, chunks.Count, headingPath, piece));
                }
            }

            currentSectionLines.Clear();
        }

        foreach (var line in bodyLines)
        {
            if (line.TrimStart().StartsWith(CodeFenceMarker))
            {
                isInsideCodeFence = !isInsideCodeFence;
            }

            var headingMatch = isInsideCodeFence ? Match.Empty : HeadingPattern().Match(line);
            if (headingMatch.Success)
            {
                FlushCurrentSection();

                var headingLevel = headingMatch.Groups["hashes"].Length;
                while (headingStack.Count > 0 && headingStack[^1].Level >= headingLevel)
                {
                    headingStack.RemoveAt(headingStack.Count - 1);
                }

                headingStack.Add((headingLevel, headingMatch.Groups["title"].Value));
            }

            currentSectionLines.Add(line);
        }

        FlushCurrentSection();
        return chunks;
    }

    private static IEnumerable<string> SkipFrontMatter(string[] lines)
    {
        if (lines.Length == 0 || lines[0].Trim() != FrontMatterDelimiter)
        {
            return lines;
        }

        var closingDelimiterIndex = Array.FindIndex(lines, 1, line => line.Trim() == FrontMatterDelimiter);
        return closingDelimiterIndex < 0 ? lines : lines.Skip(closingDelimiterIndex + 1);
    }

    private IEnumerable<string> SplitToMaximumLength(string sectionText)
    {
        if (sectionText.Length <= maximumChunkCharacters)
        {
            return [sectionText];
        }

        return PackIntoPieces(SplitIntoBlocks(sectionText), ParagraphSeparator);
    }

    private IEnumerable<string> SplitIntoBlocks(string sectionText)
    {
        foreach (var paragraph in sectionText.Split(ParagraphSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            if (paragraph.Length <= maximumChunkCharacters)
            {
                yield return paragraph;
                continue;
            }

            foreach (var lineGroup in PackIntoPieces(paragraph.Split('\n'), LineSeparator))
            {
                yield return lineGroup;
            }
        }
    }

    private IEnumerable<string> PackIntoPieces(IEnumerable<string> units, string separator)
    {
        var currentPiece = new StringBuilder();

        foreach (var unit in units)
        {
            var wouldExceedLimit = currentPiece.Length > 0
                && currentPiece.Length + separator.Length + unit.Length > maximumChunkCharacters;

            if (wouldExceedLimit)
            {
                yield return currentPiece.ToString();
                currentPiece.Clear();
            }

            if (currentPiece.Length > 0)
            {
                currentPiece.Append(separator);
            }

            currentPiece.Append(unit);
        }

        if (currentPiece.Length > 0)
        {
            yield return currentPiece.ToString();
        }
    }
}