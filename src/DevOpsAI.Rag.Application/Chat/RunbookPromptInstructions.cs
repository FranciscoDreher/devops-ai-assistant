namespace DevOpsAI.Rag.Application.Chat;

public sealed record RunbookPromptInstructions
{
    public RunbookPromptInstructions(string systemPrompt, string answerFormatPrompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemPrompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(answerFormatPrompt);

        SystemPrompt = systemPrompt.Trim();
        AnswerFormatPrompt = answerFormatPrompt.Trim();
    }

    public string SystemPrompt { get; }

    public string AnswerFormatPrompt { get; }
}
