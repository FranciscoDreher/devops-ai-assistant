namespace DevOpsAI.Rag.Application.Chat;

public sealed record RunbookPromptEvidence
{
    public RunbookPromptEvidence(string chunkId, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chunkId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        ChunkId = chunkId.Trim();
        Text = text;
    }

    public string ChunkId { get; }

    public string Text { get; }
}
