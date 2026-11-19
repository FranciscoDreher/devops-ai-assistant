using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Domain.Documents;
using Microsoft.Extensions.Options;

namespace DevOpsAI.Rag.Application.Documents;

public sealed class RunbookDocumentChunker
{
    private readonly int _maximumChunkCharacters;
    private readonly int _overlapCharacters;

    public RunbookDocumentChunker(IOptions<RunbookChunkingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var chunkingOptions = options.Value;
        if (chunkingOptions.MaximumChunkCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The maximum chunk size must be greater than zero.");
        }

        if (chunkingOptions.OverlapCharacters < 0
            || chunkingOptions.OverlapCharacters >= chunkingOptions.MaximumChunkCharacters)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The chunk overlap must be non-negative and smaller than the maximum chunk size.");
        }

        _maximumChunkCharacters = chunkingOptions.MaximumChunkCharacters;
        _overlapCharacters = chunkingOptions.OverlapCharacters;
    }

    public IReadOnlyList<DocumentChunk> Chunk(KnowledgeDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Status != KnowledgeDocumentStatus.Active || document.Content is null)
        {
            throw new ArgumentException("Only active documents can be chunked.", nameof(document));
        }

        var markdown = Path.GetExtension(document.FileName)
            .Equals(".md", StringComparison.OrdinalIgnoreCase);
        var chunks = new List<DocumentChunk>();
        var sectionLines = new List<SourceLine>();
        string? heading = null;
        int? headingLineNumber = null;
        var inCodeFence = false;
        var codeFenceCharacter = '\0';
        var codeFenceLength = 0;
        var lineNumber = 0;

        using var reader = new StringReader(document.Content);
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;

            if (markdown && inCodeFence)
            {
                sectionLines.Add(new SourceLine(lineNumber, line));
                if (IsClosingCodeFence(line, codeFenceCharacter, codeFenceLength))
                {
                    inCodeFence = false;
                }

                continue;
            }

            if (markdown && TryGetCodeFence(line, out codeFenceCharacter, out codeFenceLength))
            {
                sectionLines.Add(new SourceLine(lineNumber, line));
                inCodeFence = true;
                continue;
            }

            if (markdown && TryGetHeading(line, out var nextHeading))
            {
                AddSectionChunks(document, heading, headingLineNumber, sectionLines, chunks);
                sectionLines.Clear();
                heading = nextHeading;
                headingLineNumber = lineNumber;
                continue;
            }

            sectionLines.Add(new SourceLine(lineNumber, line));
        }

        AddSectionChunks(document, heading, headingLineNumber, sectionLines, chunks);
        return chunks;
    }

    private void AddSectionChunks(
        KnowledgeDocument document,
        string? heading,
        int? headingLineNumber,
        IReadOnlyList<SourceLine> sourceLines,
        List<DocumentChunk> chunks)
    {
        var firstContentLine = 0;
        var lastContentLine = sourceLines.Count - 1;

        while (firstContentLine <= lastContentLine
            && string.IsNullOrWhiteSpace(sourceLines[firstContentLine].Text))
        {
            firstContentLine++;
        }

        while (lastContentLine >= firstContentLine
            && string.IsNullOrWhiteSpace(sourceLines[lastContentLine].Text))
        {
            lastContentLine--;
        }

        if (firstContentLine > lastContentLine)
        {
            if (!string.IsNullOrWhiteSpace(heading) && headingLineNumber is int sourceLine)
            {
                var ordinal = chunks.Count;
                chunks.Add(new DocumentChunk(
                    $"{document.Id}:v{document.Version}:chunk:{ordinal}",
                    document,
                    ordinal,
                    heading,
                    heading,
                    sourceLine,
                    sourceLine));
            }

            return;
        }

        var contentLines = sourceLines.Skip(firstContentLine).Take(lastContentLine - firstContentLine + 1).ToArray();
        var sectionText = string.Join('\n', contentLines.Select(sourceLine => sourceLine.Text));
        var lineStarts = GetLineStarts(contentLines);
        var start = 0;

        while (start < sectionText.Length)
        {
            var end = GetChunkEnd(sectionText, start);
            var rawChunkText = sectionText[start..end];
            var leadingWhitespace = rawChunkText.Length - rawChunkText.TrimStart().Length;
            var chunkText = rawChunkText.Trim();

            if (chunkText.Length > 0)
            {
                var chunkStart = start + leadingWhitespace;
                var chunkEnd = chunkStart + chunkText.Length - 1;
                var startLine = contentLines[FindLineIndex(lineStarts, chunkStart)].Number;
                var endLine = contentLines[FindLineIndex(lineStarts, chunkEnd)].Number;
                var ordinal = chunks.Count;

                chunks.Add(new DocumentChunk(
                    $"{document.Id}:v{document.Version}:chunk:{ordinal}",
                    document,
                    ordinal,
                    chunkText,
                    heading,
                    startLine,
                    endLine));

                if (end == sectionText.Length)
                {
                    break;
                }

                var nextStart = sectionText.AsSpan(end).StartsWith("\n\n", StringComparison.Ordinal)
                    ? end
                    : chunkStart + chunkText.Length - _overlapCharacters;
                while (nextStart < sectionText.Length && char.IsWhiteSpace(sectionText[nextStart]))
                {
                    nextStart++;
                }

                start = Math.Max(start + 1, nextStart);
            }
            else
            {
                if (end == sectionText.Length)
                {
                    break;
                }

                start = end;
            }
        }
    }

    private int GetChunkEnd(string text, int start)
    {
        var limit = Math.Min(start + _maximumChunkCharacters, text.Length);
        if (limit == text.Length)
        {
            return limit;
        }

        var paragraphBreak = text.LastIndexOf("\n\n", limit - 1, limit - start, StringComparison.Ordinal);
        if (paragraphBreak > start + (_maximumChunkCharacters / 2))
        {
            return paragraphBreak;
        }

        for (var index = limit - 1; index > start; index--)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                return index;
            }
        }

        return limit;
    }

    private static int[] GetLineStarts(IReadOnlyList<SourceLine> lines)
    {
        var lineStarts = new int[lines.Count];
        var offset = 0;
        for (var index = 0; index < lines.Count; index++)
        {
            lineStarts[index] = offset;
            offset += lines[index].Text.Length + (index < lines.Count - 1 ? 1 : 0);
        }

        return lineStarts;
    }

    private static int FindLineIndex(int[] lineStarts, int offset)
    {
        var lower = 0;
        var upper = lineStarts.Length - 1;

        while (lower <= upper)
        {
            var middle = lower + ((upper - lower) / 2);
            if (lineStarts[middle] == offset)
            {
                return middle;
            }

            if (lineStarts[middle] < offset)
            {
                lower = middle + 1;
            }
            else
            {
                upper = middle - 1;
            }
        }

        return upper;
    }

    private static bool TryGetHeading(string line, out string? heading)
    {
        var trimmedLine = line.TrimStart();
        var markerLength = 0;
        while (markerLength < trimmedLine.Length
            && markerLength < 6
            && trimmedLine[markerLength] == '#')
        {
            markerLength++;
        }

        if (markerLength == 0
            || (markerLength < trimmedLine.Length && !char.IsWhiteSpace(trimmedLine[markerLength])))
        {
            heading = null;
            return false;
        }

        var headingText = trimmedLine[markerLength..].Trim();
        var closingMarker = headingText.Length;
        while (closingMarker > 0 && headingText[closingMarker - 1] == '#')
        {
            closingMarker--;
        }

        heading = headingText[..closingMarker].Trim();
        return true;
    }

    private static bool TryGetCodeFence(string line, out char fenceCharacter, out int fenceLength)
    {
        var trimmedLine = line.TrimStart();
        fenceCharacter = trimmedLine.Length > 0 ? trimmedLine[0] : '\0';
        fenceLength = 0;

        if (fenceCharacter is not ('`' or '~'))
        {
            return false;
        }

        while (fenceLength < trimmedLine.Length && trimmedLine[fenceLength] == fenceCharacter)
        {
            fenceLength++;
        }

        return fenceLength >= 3;
    }

    private static bool IsClosingCodeFence(string line, char fenceCharacter, int minimumFenceLength)
    {
        var trimmedLine = line.TrimStart();
        var fenceLength = 0;
        while (fenceLength < trimmedLine.Length && trimmedLine[fenceLength] == fenceCharacter)
        {
            fenceLength++;
        }

        return fenceLength >= minimumFenceLength
            && string.IsNullOrWhiteSpace(trimmedLine[fenceLength..]);
    }

    private sealed record SourceLine(int Number, string Text);
}
