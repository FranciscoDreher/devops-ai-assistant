using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.UnitTests;

public class DocumentChunkTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ChunkRetainsItsDocumentAndSourceLocation()
    {
        var document = KnowledgeDocument.Create("runbook", "runbook.md", "# Recovery\nRestart the cache.", CreatedAtUtc);

        var chunk = new DocumentChunk("chunk-1", document, 0, "Restart the cache.", "Recovery", 2, 2);

        Assert.Same(document, chunk.Document);
        Assert.Equal("chunk-1", chunk.Id);
        Assert.Equal(0, chunk.Ordinal);
        Assert.Equal("Restart the cache.", chunk.Text);
        Assert.Equal("Recovery", chunk.Heading);
        Assert.Equal(2, chunk.StartLine);
        Assert.Equal(2, chunk.EndLine);
    }

    [Fact]
    public void ChunkRejectsNullDocument()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DocumentChunk("chunk-1", null!, 0, "Text", null, 1, 1));
    }

    [Theory]
    [InlineData("", "Text", 0, 1, 1)]
    [InlineData("chunk-1", " ", 0, 1, 1)]
    [InlineData("chunk-1", "Text", -1, 1, 1)]
    [InlineData("chunk-1", "Text", 0, 0, 1)]
    [InlineData("chunk-1", "Text", 0, 3, 2)]
    [InlineData("chunk-1", "Text", 0, 1, 2)]
    public void ChunkRejectsInvalidIdTextOrdinalOrLines(
        string id,
        string text,
        int ordinal,
        int startLine,
        int endLine)
    {
        var document = KnowledgeDocument.Create("runbook", "runbook.md", "Text", CreatedAtUtc);

        Assert.ThrowsAny<ArgumentException>(() =>
            new DocumentChunk(id, document, ordinal, text, null, startLine, endLine));
    }

    [Fact]
    public void ChunkRejectsDeletedDocument()
    {
        var document = KnowledgeDocument.Create("runbook", "runbook.md", "Text", CreatedAtUtc)
            .Delete(CreatedAtUtc.AddMinutes(1));

        Assert.Null(document.Content);
        Assert.Throws<ArgumentException>(() =>
            new DocumentChunk("chunk-1", document, 0, "Text", null, 1, 1));
    }

    [Fact]
    public void ChunkCountsCrLfAndCrLineEndingsInDocumentContent()
    {
        var document = KnowledgeDocument.Create(
            "runbook",
            "runbook.md",
            "First\r\nSecond\rThird",
            CreatedAtUtc);

        var chunk = new DocumentChunk("chunk-1", document, 0, "Third", null, 3, 3);

        Assert.Equal(3, chunk.StartLine);
        Assert.Equal(3, chunk.EndLine);
    }
}
