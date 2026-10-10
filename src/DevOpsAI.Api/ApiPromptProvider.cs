using DevOpsAI.Application;
using DevOpsAI.Domain;

namespace DevOpsAI.Api;

internal sealed class ApiPromptProvider : IPromptProvider
{
    private readonly string _promptRootPath;

    public ApiPromptProvider()
        : this(ResolvePromptRootPath())
    {
    }

    internal ApiPromptProvider(string promptRootPath)
    {
        if (string.IsNullOrWhiteSpace(promptRootPath))
        {
            throw new ArgumentException("Prompt root path is required.", nameof(promptRootPath));
        }

        _promptRootPath = promptRootPath;
    }

    public async Task<string> GetSystemPromptAsync(CancellationToken cancellationToken)
    {
        return await ReadPromptFileAsync(Path.Combine(_promptRootPath, "system.md"), "system", cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<string> GetAudiencePromptAsync(IncidentAudience audience, CancellationToken cancellationToken)
    {
        var fileName = audience switch
        {
            IncidentAudience.Leadership => "leadership.md",
            IncidentAudience.Customer => "customer.md",
            _ => throw new ArgumentOutOfRangeException(nameof(audience), audience, "Unsupported incident audience.")
        };

        return await ReadPromptFileAsync(Path.Combine(_promptRootPath, fileName), fileName, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string ResolvePromptRootPath()
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var current = new DirectoryInfo(Path.GetFullPath(root));
            while (current is not null)
            {
                visited.Add(current.FullName);
                current = current.Parent;
            }
        }

        foreach (var directory in visited)
        {
            var candidate = Path.Combine(directory, "docs", "prompts", "incident-polisher");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("The prompt assets directory 'docs/prompts/incident-polisher' was not found.");
    }

    private static async Task<string> ReadPromptFileAsync(string filePath, string promptName, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException($"Prompt file '{promptName}' was not found at '{filePath}'.");
        }

        var content = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        var trimmed = content.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException($"Prompt file '{promptName}' is empty.");
        }

        return trimmed;
    }
}
