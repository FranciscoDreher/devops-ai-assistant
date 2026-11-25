using DevOpsAI.Rag.Application.Ports;
using DevOpsAI.Rag.Domain.Chat;

namespace DevOpsAI.Rag.Application.Chat;

public sealed class GenerateRunbookAnswer
{
    public const string InsufficientEvidenceAnswer =
        "There isn't enough information in the indexed runbooks to answer that.";

    private readonly IRunbookPromptProvider _promptProvider;
    private readonly IRunbookAnswerGenerator _answerGenerator;

    public GenerateRunbookAnswer(
        IRunbookPromptProvider promptProvider,
        IRunbookAnswerGenerator answerGenerator)
    {
        ArgumentNullException.ThrowIfNull(promptProvider);
        ArgumentNullException.ThrowIfNull(answerGenerator);

        _promptProvider = promptProvider;
        _answerGenerator = answerGenerator;
    }

    public async Task<RunbookAnswerResult> ExecuteAsync(
        string question,
        RunbookRetrievalResult retrievalResult,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentNullException.ThrowIfNull(retrievalResult);
        cancellationToken.ThrowIfCancellationRequested();

        if (retrievalResult.Sufficiency == RunbookEvidenceSufficiency.Insufficient)
        {
            return RunbookAnswerResult.InsufficientEvidence(InsufficientEvidenceAnswer);
        }

        var instructions = await _promptProvider.GetPromptAsync(cancellationToken).ConfigureAwait(false);
        var prompt = new RunbookAnswerPrompt(
            instructions,
            question,
            retrievalResult.Candidates
                .Select(candidate => new RunbookPromptEvidence(candidate.Chunk.Id, candidate.Chunk.Text))
                .ToArray());
        var generatedAnswer = await _answerGenerator
            .GenerateAsync(prompt, cancellationToken)
            .ConfigureAwait(false);

        if (generatedAnswer.Status == RunbookAnswerStatus.InsufficientEvidence)
        {
            return RunbookAnswerResult.InsufficientEvidence(InsufficientEvidenceAnswer);
        }

        var citations = generatedAnswer.CitedChunkIds
            .Select(chunkId => FindRetrievedChunk(retrievalResult, chunkId))
            .Select(candidate => new CitationReference(candidate.Chunk))
            .ToArray();
        var groundedAnswer = new GroundedAnswer(generatedAnswer.Answer, retrievalResult.Candidates, citations);

        return RunbookAnswerResult.Answered(groundedAnswer);
    }

    private static RetrievalCandidate FindRetrievedChunk(RunbookRetrievalResult retrievalResult, string chunkId)
    {
        foreach (var candidate in retrievalResult.Candidates)
        {
            if (string.Equals(candidate.Chunk.Id, chunkId, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        throw new InvalidDataException("The generated answer referenced a chunk that was not retrieved.");
    }
}
