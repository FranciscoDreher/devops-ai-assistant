using DevOpsAI.Rag.Domain.Documents;

namespace DevOpsAI.Rag.UnitTests;

public class KnowledgeDocumentTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateComputesHashAndStartsAtVersionOne()
    {
        var document = KnowledgeDocument.Create(
            "cache-recovery",
            "cache-recovery.md",
            "# Recovery\nRestart the cache.",
            CreatedAtUtc);

        Assert.Equal("cache-recovery", document.Id);
        Assert.Equal("cache-recovery.md", document.FileName);
        Assert.Equal("sha256:15e1d47cfc9df8451046ba101423614ece2fdcabec4f53489e090d9204f06aac", document.ContentHash);
        Assert.Equal(1, document.Version);
        Assert.Equal(KnowledgeDocumentStatus.Active, document.Status);
        Assert.Equal(CreatedAtUtc, document.CreatedAtUtc);
        Assert.Equal(CreatedAtUtc, document.UpdatedAtUtc);
        Assert.Null(document.DeletedAtUtc);
    }

    [Theory]
    [InlineData("", "runbook.md", "content")]
    [InlineData("runbook", "", "content")]
    [InlineData("runbook", "runbook.md", " \r\n ")]
    public void CreateRejectsInvalidIdentityFilenameOrContent(string id, string fileName, string content)
    {
        Assert.Throws<ArgumentException>(() => KnowledgeDocument.Create(id, fileName, content, CreatedAtUtc));
    }

    [Theory]
    [InlineData("folder/runbook.md")]
    [InlineData("folder\\runbook.md")]
    public void CreateRejectsPathsAsFilenames(string fileName)
    {
        Assert.Throws<ArgumentException>(() =>
            KnowledgeDocument.Create("runbook", fileName, "content", CreatedAtUtc));
    }

    [Fact]
    public void ReplaceCreatesNewVersionWithoutChangingPriorSnapshot()
    {
        var original = KnowledgeDocument.Create("runbook", "old.md", "Old procedure.", CreatedAtUtc);
        var replacedAtUtc = CreatedAtUtc.AddMinutes(5);

        var replacement = original.Replace("new.md", "New procedure.", replacedAtUtc);

        Assert.Equal(1, original.Version);
        Assert.Equal("Old procedure.", original.Content);
        Assert.Equal("old.md", original.FileName);
        Assert.Equal(2, replacement.Version);
        Assert.Equal("New procedure.", replacement.Content);
        Assert.Equal("new.md", replacement.FileName);
        Assert.Equal(original.CreatedAtUtc, replacement.CreatedAtUtc);
        Assert.Equal(replacedAtUtc, replacement.UpdatedAtUtc);
        Assert.NotEqual(original.ContentHash, replacement.ContentHash);
    }

    [Fact]
    public void DeleteRetainsContentAndHashForTraceability()
    {
        var document = KnowledgeDocument.Create("runbook", "runbook.md", "Procedure.", CreatedAtUtc);
        var deletedAtUtc = CreatedAtUtc.AddMinutes(10);

        var deleted = document.Delete(deletedAtUtc);

        Assert.Equal(KnowledgeDocumentStatus.Active, document.Status);
        Assert.Equal(KnowledgeDocumentStatus.Deleted, deleted.Status);
        Assert.Equal(document.Content, deleted.Content);
        Assert.Equal(document.ContentHash, deleted.ContentHash);
        Assert.Equal(document.Version, deleted.Version);
        Assert.Equal(deletedAtUtc, deleted.DeletedAtUtc);
    }

    [Fact]
    public void ReplaceRejectsTimestampBeforeLastUpdate()
    {
        var document = KnowledgeDocument.Create("runbook", "runbook.md", "Procedure.", CreatedAtUtc);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            document.Replace("runbook.md", "Updated procedure.", CreatedAtUtc.AddTicks(-1)));
    }

    [Fact]
    public void DeleteRejectsRepeatedDeletion()
    {
        var deleted = KnowledgeDocument.Create("runbook", "runbook.md", "Procedure.", CreatedAtUtc)
            .Delete(CreatedAtUtc.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => deleted.Delete(CreatedAtUtc.AddMinutes(2)));
    }
}
