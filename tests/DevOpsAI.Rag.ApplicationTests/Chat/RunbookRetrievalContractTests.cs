using DevOpsAI.Rag.Application.Chat;
using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Domain.Chat;
using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.ApplicationTests.Chat;

public sealed class RunbookRetrievalContractTests
{
    private static readonly float[] NonZeroVector = [1f];
    private static readonly float[] ZeroVector = [0f, 0f];
    private static readonly float[] NonFiniteVector = [float.NaN];

    [Fact]
    public void EmbeddingAndKnowledgeIndexPortsExposeProviderIndependentContracts()
    {
        var embeddingMethod = typeof(ITextEmbeddingGenerator)
            .GetMethod(nameof(ITextEmbeddingGenerator.GenerateEmbeddingAsync));
        var indexMethod = typeof(IRunbookKnowledgeIndex)
            .GetMethod(nameof(IRunbookKnowledgeIndex.IndexAsync));
        var searchMethod = typeof(IRunbookKnowledgeIndex)
            .GetMethod(nameof(IRunbookKnowledgeIndex.SearchAsync));

        Assert.NotNull(embeddingMethod);
        Assert.NotNull(indexMethod);
        Assert.NotNull(searchMethod);
        Assert.Equal(typeof(string), typeof(ITextEmbeddingGenerator).GetProperty(nameof(ITextEmbeddingGenerator.ModelId))?.PropertyType);
        Assert.Equal(typeof(Task<ReadOnlyMemory<float>>), embeddingMethod.ReturnType);
        Assert.Equal(typeof(CancellationToken), embeddingMethod.GetParameters()[1].ParameterType);
        Assert.Equal(typeof(Task), indexMethod.ReturnType);
        Assert.Equal(typeof(RunbookChunkEmbedding), indexMethod.GetParameters()[2].ParameterType.GenericTypeArguments[0]);
        Assert.Equal(typeof(CancellationToken), indexMethod.GetParameters()[3].ParameterType);
        Assert.Equal(typeof(Task<RunbookRetrievalResult>), searchMethod.ReturnType);
        Assert.Equal(typeof(CancellationToken), searchMethod.GetParameters()[2].ParameterType);
        Assert.DoesNotContain(
            typeof(ITextEmbeddingGenerator).GetMembers(),
            member => member.MemberType == System.Reflection.MemberTypes.Method
                && member.ToString()!.Contains("Ollama", StringComparison.Ordinal));
    }

    [Fact]
    public void RetrievalResultReportsSufficiencyAndKeepsCandidatesRanked()
    {
        var high = CreateCandidate("high", 0.9);
        var low = CreateCandidate("low", 0.7);

        var result = new RunbookRetrievalResult(
            [high, low],
            RunbookEvidenceSufficiency.Sufficient);

        Assert.Equal(RunbookEvidenceSufficiency.Sufficient, result.Sufficiency);
        Assert.Equal(["high", "low"], result.Candidates.Select(candidate => candidate.Chunk.Id));
    }

    [Fact]
    public void RetrievalResultRejectsSufficiencyThatDoesNotMatchTheCandidateSet()
    {
        Assert.Throws<ArgumentException>(() => new RunbookRetrievalResult(
            [],
            RunbookEvidenceSufficiency.Sufficient));
        Assert.Throws<ArgumentException>(() => new RunbookRetrievalResult(
            [CreateCandidate("candidate", 0.8)],
            RunbookEvidenceSufficiency.Insufficient));
    }

    [Fact]
    public void RetrievalResultRejectsCandidatesThatAreNotRankedDescending()
    {
        Assert.Throws<ArgumentException>(() => new RunbookRetrievalResult(
            [CreateCandidate("low", 0.5), CreateCandidate("high", 0.9)],
            RunbookEvidenceSufficiency.Sufficient));
    }

    [Fact]
    public void RetrievalOptionsRejectInvalidThresholdAndTopK()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RunbookRetrievalOptions
        {
            MinimumRelevanceScore = 1.1
        }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new RunbookRetrievalOptions
        {
            TopK = 0
        }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new RunbookRetrievalOptions
        {
            TopK = RunbookRetrievalOptions.MaximumTopK + 1
        }.Validate());

        new RunbookRetrievalOptions().Validate();
    }

    [Fact]
    public void ChunkEmbeddingRequiresAnIdentifierModelAndFiniteNonZeroVector()
    {
        Assert.Throws<ArgumentException>(() => new RunbookChunkEmbedding("", "model", NonZeroVector));
        Assert.Throws<ArgumentException>(() => new RunbookChunkEmbedding("chunk", "", NonZeroVector));
        Assert.Throws<ArgumentException>(() => new RunbookChunkEmbedding("chunk", "model", ZeroVector));
        Assert.Throws<ArgumentException>(() => new RunbookChunkEmbedding("chunk", "model", NonFiniteVector));
    }

    private static RetrievalCandidate CreateCandidate(string chunkId, double score)
    {
        var document = KnowledgeDocument.Create(
            "runbook",
            "runbook.md",
            "Procedure",
            DateTimeOffset.UnixEpoch);
        var chunk = new DocumentChunk(chunkId, document, 0, "Procedure", null, 1, 1);
        return new RetrievalCandidate(chunk, score);
    }
}
