using DevOpsAI.Rag.Domain.Chat;
using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.UnitTests;

public class GroundedAnswerTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CandidateRejectsScoresOutsideNormalizedRange()
    {
        var chunk = CreateChunk();

        Assert.Throws<ArgumentOutOfRangeException>(() => new RetrievalCandidate(chunk, -0.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetrievalCandidate(chunk, 1.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetrievalCandidate(chunk, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RetrievalCandidate(chunk, double.PositiveInfinity));
    }

    [Fact]
    public void CitationCopiesTrustedChunkMetadata()
    {
        var chunk = CreateChunk();

        var citation = new CitationReference(chunk);

        Assert.Equal(chunk.Document.Id, citation.DocumentId);
        Assert.Equal(chunk.Document.Version, citation.DocumentVersion);
        Assert.Equal(chunk.Id, citation.ChunkId);
        Assert.Equal(chunk.Heading, citation.Heading);
        Assert.Equal(chunk.StartLine, citation.StartLine);
        Assert.Equal(chunk.EndLine, citation.EndLine);
    }

    [Fact]
    public void GroundedAnswerAcceptsCitationsFromRetrievedCandidates()
    {
        var chunk = CreateChunk();
        var candidate = new RetrievalCandidate(chunk, 0.8);
        var citation = new CitationReference(chunk);

        var answer = new GroundedAnswer("Restart the cache.", [candidate], [citation]);

        Assert.Equal("Restart the cache.", answer.Text);
        Assert.Single(answer.RetrievedCandidates);
        Assert.Single(answer.Citations);
    }

    [Fact]
    public void GroundedAnswerRejectsEmptyTextOrMissingReferences()
    {
        var candidate = new RetrievalCandidate(CreateChunk(), 0.8);
        var citation = new CitationReference(candidate.Chunk);

        Assert.Throws<ArgumentException>(() => new GroundedAnswer(" ", [candidate], [citation]));
        Assert.Throws<ArgumentException>(() => new GroundedAnswer("Answer", [candidate], []));
        Assert.Throws<ArgumentException>(() => new GroundedAnswer("Answer", [], [citation]));
    }

    [Fact]
    public void GroundedAnswerRejectsReferencesNotPresentInRetrievedResults()
    {
        var retrievedChunk = CreateChunk();
        var otherDocument = KnowledgeDocument.Create("other-runbook", "other.md", "Other text.", CreatedAtUtc);
        var otherChunk = new DocumentChunk("other-chunk", otherDocument, 0, "Other text.", null, 1, 1);

        var answer = () => new GroundedAnswer(
            "Answer",
            [new RetrievalCandidate(retrievedChunk, 0.8)],
            [new CitationReference(otherChunk)]);

        Assert.Throws<ArgumentException>(answer);
    }

    [Fact]
    public void GroundedAnswerRejectsCitationFromAnotherDocumentVersion()
    {
        var original = CreateChunk();
        var updatedDocument = original.Document.Replace(
            original.Document.FileName,
            "Updated content.",
            CreatedAtUtc.AddMinutes(1));
        var updatedChunk = new DocumentChunk("chunk-1", updatedDocument, 0, "Updated content.", null, 1, 1);

        var answer = () => new GroundedAnswer(
            "Answer",
            [new RetrievalCandidate(updatedChunk, 0.8)],
            [new CitationReference(original)]);

        Assert.Throws<ArgumentException>(answer);
    }

    [Fact]
    public void GroundedAnswerCopiesInputCollections()
    {
        var chunk = CreateChunk();
        var candidates = new List<RetrievalCandidate> { new(chunk, 0.8) };
        var citations = new List<CitationReference> { new(chunk) };

        var answer = new GroundedAnswer("Answer", candidates, citations);
        candidates.Clear();
        citations.Clear();

        Assert.Single(answer.RetrievedCandidates);
        Assert.Single(answer.Citations);
    }

    private static DocumentChunk CreateChunk()
    {
        var document = KnowledgeDocument.Create("runbook", "runbook.md", "Restart the cache.", CreatedAtUtc);
        return new DocumentChunk("chunk-1", document, 0, "Restart the cache.", "Recovery", 1, 1);
    }
}
