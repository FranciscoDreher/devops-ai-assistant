using DevOpsAI.Rag.Infrastructure.Prompts;

namespace DevOpsAI.Rag.UnitTests.Prompts;

public sealed class FileRunbookPromptProviderTests
{
    [Fact]
    public async Task GetPromptAsyncLoadsBothVersionedPromptFiles()
    {
        var promptPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(promptPath);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(promptPath, "system.md"), "  Ground the answer.  ");
            await File.WriteAllTextAsync(Path.Combine(promptPath, "answer-format.md"), "  Return JSON.  ");
            var provider = new FileRunbookPromptProvider(promptPath);

            var result = await provider.GetPromptAsync(CancellationToken.None);

            Assert.Equal("Ground the answer.", result.SystemPrompt);
            Assert.Equal("Return JSON.", result.AnswerFormatPrompt);
        }
        finally
        {
            Directory.Delete(promptPath, recursive: true);
        }
    }

    [Fact]
    public async Task GetPromptAsyncWhenPromptFileIsMissingThrowsFileNotFoundException()
    {
        var promptPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(promptPath);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(promptPath, "system.md"), "Ground the answer.");
            var provider = new FileRunbookPromptProvider(promptPath);

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                provider.GetPromptAsync(CancellationToken.None));
        }
        finally
        {
            Directory.Delete(promptPath, recursive: true);
        }
    }
}
