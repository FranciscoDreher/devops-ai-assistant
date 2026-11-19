using System.Text;
using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Application.Documents;
using DevOpsAI.Rag.Domain.Documents;
using Microsoft.Extensions.Options;

namespace DevOpsAI.Rag.ApplicationTests.Documents;

public sealed class RunbookDocumentChunkerTests
{
    private static readonly DateTimeOffset CurrentTime = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ChunkProducesDeterministicChunksWithHeadingAndSourceLocations()
    {
        var document = CreateDocument(
            "## Recovery\nRestart the cache.\n\nVerify service health.");
        var chunker = CreateChunker(maximumChunkCharacters: 100, overlapCharacters: 10);

        var firstResult = chunker.Chunk(document);
        var secondResult = chunker.Chunk(document);

        Assert.Equal(firstResult, secondResult);
        var chunk = Assert.Single(firstResult);
        Assert.Equal("runbook:v1:chunk:0", chunk.Id);
        Assert.Equal("Recovery", chunk.Heading);
        Assert.Equal("Restart the cache.\n\nVerify service health.", chunk.Text);
        Assert.Equal(2, chunk.StartLine);
        Assert.Equal(4, chunk.EndLine);
    }

    [Fact]
    public void ChunkSplitsAtHeadingsAndRetainsEachSectionHeading()
    {
        var document = CreateDocument(
            "# Recovery\nRestart the cache.\n## Verification\nCheck service health.");
        var chunker = CreateChunker(maximumChunkCharacters: 100, overlapCharacters: 10);

        var chunks = chunker.Chunk(document);

        Assert.Collection(
            chunks,
            recovery =>
            {
                Assert.Equal("Recovery", recovery.Heading);
                Assert.Equal("Restart the cache.", recovery.Text);
                Assert.Equal(2, recovery.StartLine);
                Assert.Equal(2, recovery.EndLine);
            },
            verification =>
            {
                Assert.Equal("Verification", verification.Heading);
                Assert.Equal("Check service health.", verification.Text);
                Assert.Equal(4, verification.StartLine);
                Assert.Equal(4, verification.EndLine);
            });
    }

    [Fact]
    public void ChunkPrefersParagraphBoundariesWhenSplittingSections()
    {
        var document = CreateDocument("First paragraph.\n\nSecond paragraph.");
        var chunker = CreateChunker(maximumChunkCharacters: 24, overlapCharacters: 4);

        var chunks = chunker.Chunk(document);

        Assert.Collection(
            chunks,
            first => Assert.Equal("First paragraph.", first.Text),
            second => Assert.Equal("Second paragraph.", second.Text));
    }

    [Fact]
    public void ChunkRespectsMaximumSizeAndOverlapsAdjacentChunks()
    {
        const int maximumChunkCharacters = 30;
        const int overlapCharacters = 8;
        var document = CreateDocument(
            "alpha bravo charlie delta echo foxtrot golf hotel india juliet kilo lima mike november");
        var chunker = CreateChunker(maximumChunkCharacters, overlapCharacters);

        var chunks = chunker.Chunk(document);

        Assert.True(chunks.Count > 1);
        Assert.All(chunks, chunk => Assert.InRange(chunk.Text.Length, 1, maximumChunkCharacters));
        for (var index = 1; index < chunks.Count; index++)
        {
            Assert.EndsWith(
                chunks[index].Text[..Math.Min(overlapCharacters, chunks[index].Text.Length)],
                chunks[index - 1].Text,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ChunkTreatsMarkdownCodeFenceContentAsPlainText()
    {
        var document = CreateDocument("```text\n# not a heading\nrestart-cache\n```");
        var chunker = CreateChunker(maximumChunkCharacters: 100, overlapCharacters: 10);

        var chunk = Assert.Single(chunker.Chunk(document));

        Assert.Null(chunk.Heading);
        Assert.Equal("```text\n# not a heading\nrestart-cache\n```", chunk.Text);
    }

    [Fact]
    public void ChunkDoesNotInterpretMarkdownSyntaxInTextFiles()
    {
        var document = CreateDocument("# plain text\nDo not treat this as a heading.", "runbook.txt");
        var chunker = CreateChunker(maximumChunkCharacters: 100, overlapCharacters: 10);

        var chunk = Assert.Single(chunker.Chunk(document));

        Assert.Null(chunk.Heading);
        Assert.Equal("# plain text\nDo not treat this as a heading.", chunk.Text);
    }

    [Fact]
    public void ChunkPreservesHeadingOnlyMarkdownDocuments()
    {
        var document = CreateDocument("# Recovery");
        var chunker = CreateChunker(maximumChunkCharacters: 100, overlapCharacters: 10);

        var chunk = Assert.Single(chunker.Chunk(document));

        Assert.Equal("Recovery", chunk.Text);
        Assert.Equal("Recovery", chunk.Heading);
        Assert.Equal(1, chunk.StartLine);
        Assert.Equal(1, chunk.EndLine);
    }

    [Fact]
    public void ChunkRejectsInvalidChunkingOptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateChunker(maximumChunkCharacters: 0, overlapCharacters: 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateChunker(maximumChunkCharacters: 10, overlapCharacters: 10));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateChunker(maximumChunkCharacters: 10, overlapCharacters: -1));
    }

    private static RunbookDocumentChunker CreateChunker(int maximumChunkCharacters, int overlapCharacters)
    {
        return new RunbookDocumentChunker(
            Options.Create(new RunbookChunkingOptions
            {
                MaximumChunkCharacters = maximumChunkCharacters,
                OverlapCharacters = overlapCharacters
            }));
    }

    private static KnowledgeDocument CreateDocument(string content, string fileName = "runbook.md")
    {
        return KnowledgeDocument.Create(
            "runbook",
            fileName,
            content,
            CurrentTime);
    }
}
