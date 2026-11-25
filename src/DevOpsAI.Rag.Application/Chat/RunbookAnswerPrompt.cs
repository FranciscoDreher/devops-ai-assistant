using System.Collections.Immutable;

namespace DevOpsAI.Rag.Application.Chat;

public sealed class RunbookAnswerPrompt
{
    public RunbookAnswerPrompt(
        RunbookPromptInstructions instructions,
        string question,
        IReadOnlyCollection<RunbookPromptEvidence> evidence)
    {
        ArgumentNullException.ThrowIfNull(instructions);
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentNullException.ThrowIfNull(evidence);

        var evidenceSnapshot = evidence.ToImmutableArray();
        if (evidenceSnapshot.IsDefaultOrEmpty)
        {
            throw new ArgumentException("Answer generation requires retrieved evidence.", nameof(evidence));
        }

        if (evidenceSnapshot.Any(item => item is null))
        {
            throw new ArgumentException("Prompt evidence cannot contain null values.", nameof(evidence));
        }

        if (evidenceSnapshot.Select(item => item.ChunkId).Distinct(StringComparer.Ordinal).Count()
            != evidenceSnapshot.Length)
        {
            throw new ArgumentException("Prompt evidence chunk IDs must be unique.", nameof(evidence));
        }

        Instructions = instructions;
        Question = question.Trim();
        Evidence = evidenceSnapshot;
    }

    public RunbookPromptInstructions Instructions { get; }

    public string Question { get; }

    public ImmutableArray<RunbookPromptEvidence> Evidence { get; }
}
