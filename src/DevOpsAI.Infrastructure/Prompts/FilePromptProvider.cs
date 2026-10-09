using DevOpsAI.Application;
using DevOpsAI.Domain;

namespace DevOpsAI.Infrastructure.Prompts;

public sealed class FilePromptProvider : IPromptProvider
{
    private readonly string _promptRootPath;

    public FilePromptProvider()
        : this(ResolvePromptRootPath())
    {
    }

    public FilePromptProvider(string promptRootPath)
    {
        if (string.IsNullOrWhiteSpace(promptRootPath))
        {
            throw new ArgumentException("Prompt root path is required.", nameof(promptRootPath));
        }

        _promptRootPath = promptRootPath;
    }

    public async Task<string> GetSystemPromptAsync(CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(_promptRootPath, "system.md");
        return await ReadPromptFileAsync(filePath, "system", cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> GetAudiencePromptAsync(
        IncidentAudience audience,
        CancellationToken cancellationToken)
    {
        var fileName = audience switch
        {
            IncidentAudience.Leadership => "leadership.md",
            IncidentAudience.Customer => "customer.md",
            _ => throw new ArgumentOutOfRangeException(nameof(audience), audience, "Unsupported incident audience.")
        };

        var filePath = Path.Combine(_promptRootPath, fileName);
        return await ReadPromptFileAsync(filePath, fileName, cancellationToken).ConfigureAwait(false);
    }

    private static string ResolvePromptRootPath()
    {
        var inspectedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var currentDirectory = new DirectoryInfo(Path.GetFullPath(root));
            while (currentDirectory is not null)
            {
                inspectedDirectories.Add(currentDirectory.FullName);
                currentDirectory = currentDirectory.Parent;
            }
        }

        foreach (var directory in inspectedDirectories)
        {
            var candidate = Path.Combine(directory, "docs", "prompts", "incident-polisher");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException(
            "The prompt assets directory 'docs/prompts/incident-polisher' was not found.");
    }

    private static async Task<string> ReadPromptFileAsync(
        string filePath,
        string promptName,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"Prompt file '{promptName}' was not found at '{filePath}'.");
        }

        var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        var trimmedContent = content.Trim();

        if (string.IsNullOrWhiteSpace(trimmedContent))
        {
            throw new InvalidOperationException($"Prompt file '{promptName}' is empty.");
        }

        return trimmedContent;
    }
}
