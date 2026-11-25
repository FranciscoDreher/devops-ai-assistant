using DevOpsAI.Rag.Application.Chat;
using DevOpsAI.Rag.Application.Ports;

namespace DevOpsAI.Rag.Infrastructure.Prompts;

public sealed class FileRunbookPromptProvider : IRunbookPromptProvider
{
    private readonly string _promptRootPath;

    public FileRunbookPromptProvider(string promptRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptRootPath);
        _promptRootPath = promptRootPath;
    }

    public FileRunbookPromptProvider()
        : this(Path.Combine(AppContext.BaseDirectory, "docs", "prompts", "runbook-chatbot"))
    {
    }

    public async Task<RunbookPromptInstructions> GetPromptAsync(CancellationToken cancellationToken)
    {
        var systemPromptTask = ReadPromptAsync("system.md", cancellationToken);
        var answerFormatPromptTask = ReadPromptAsync("answer-format.md", cancellationToken);

        await Task.WhenAll(systemPromptTask, answerFormatPromptTask).ConfigureAwait(false);
        return new RunbookPromptInstructions(
            await systemPromptTask.ConfigureAwait(false),
            await answerFormatPromptTask.ConfigureAwait(false));
    }

    private async Task<string> ReadPromptAsync(string fileName, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_promptRootPath, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The versioned runbook prompt '{fileName}' was not found.", filePath);
        }

        var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidDataException($"The versioned runbook prompt '{fileName}' is empty.");
        }

        return content.Trim();
    }
}
