using System.Text;
using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Application.Documents;

namespace DevOpsAI.Rag.ApplicationTests.Documents;

public class RunbookDocumentValidatorTests
{
    [Theory]
    [InlineData("runbook.md")]
    [InlineData("runbook.MD")]
    [InlineData("notes.txt")]
    [InlineData("notes.TXT")]
    public void ValidateForMarkdownOrTextExtensionAcceptsUtf8Content(string fileName)
    {
        var validator = CreateValidator();

        var document = validator.Validate(
            " runbook-1 ",
            fileName,
            Encoding.UTF8.GetBytes("Procedures"));

        Assert.Equal("runbook-1", document.Id);
        Assert.Equal(fileName, document.FileName);
        Assert.Equal("Procedures", document.Content);
    }

    [Fact]
    public void ValidateWithUtf8BomStripsBomFromDecodedContent()
    {
        var validator = CreateValidator();
        var bytes = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes("Runbook content"))
            .ToArray();

        var document = validator.Validate("runbook", "runbook.md", bytes);

        Assert.Equal("Runbook content", document.Content);
    }

    [Fact]
    public void ValidateOverConfiguredByteLimitThrowsDocumentTooLarge()
    {
        var validator = CreateValidator(maxDocumentSizeBytes: 4);

        var exception = Assert.Throws<RunbookDocumentValidationException>(
            () => validator.Validate("runbook", "runbook.txt", Encoding.UTF8.GetBytes("12345")));

        Assert.Equal(RunbookDocumentValidationErrorCode.DocumentTooLarge, exception.ErrorCode);
    }

    [Fact]
    public void ValidateAtConfiguredByteLimitAcceptsDocument()
    {
        var validator = CreateValidator(maxDocumentSizeBytes: 4);

        var document = validator.Validate("runbook", "runbook.txt", Encoding.UTF8.GetBytes("1234"));

        Assert.Equal("1234", document.Content);
    }

    [Fact]
    public void ValidateForUnsupportedExtensionThrowsUnsupportedExtension()
    {
        var validator = CreateValidator();

        var exception = Assert.Throws<RunbookDocumentValidationException>(
            () => validator.Validate("runbook", "runbook.pdf", Encoding.UTF8.GetBytes("content")));

        Assert.Equal(RunbookDocumentValidationErrorCode.UnsupportedExtension, exception.ErrorCode);
    }

    [Fact]
    public void ValidateForMalformedUtf8ThrowsInvalidUtf8()
    {
        var validator = CreateValidator();

        var exception = Assert.Throws<RunbookDocumentValidationException>(
            () => validator.Validate("runbook", "runbook.txt", new byte[] { 0xc3, 0x28 }));

        Assert.Equal(RunbookDocumentValidationErrorCode.InvalidUtf8, exception.ErrorCode);
    }

    [Fact]
    public void ValidateForEmptyContentThrowsEmptyContent()
    {
        var validator = CreateValidator();

        var exception = Assert.Throws<RunbookDocumentValidationException>(
            () => validator.Validate("runbook", "runbook.txt", ReadOnlyMemory<byte>.Empty));

        Assert.Equal(RunbookDocumentValidationErrorCode.EmptyContent, exception.ErrorCode);
    }

    [Fact]
    public void ValidateForWhitespaceContentThrowsEmptyContent()
    {
        var validator = CreateValidator();

        var exception = Assert.Throws<RunbookDocumentValidationException>(
            () => validator.Validate("runbook", "runbook.txt", Encoding.UTF8.GetBytes(" \t\n")));

        Assert.Equal(RunbookDocumentValidationErrorCode.EmptyContent, exception.ErrorCode);
    }

    [Fact]
    public void ValidateForPathIdThrowsInvalidDocumentId()
    {
        var validator = CreateValidator();

        var exception = Assert.Throws<RunbookDocumentValidationException>(
            () => validator.Validate("../runbook", "runbook.md", Encoding.UTF8.GetBytes("content")));

        Assert.Equal(RunbookDocumentValidationErrorCode.InvalidDocumentId, exception.ErrorCode);
    }

    [Fact]
    public void ValidateForFileNameWithPathThrowsInvalidFileName()
    {
        var validator = CreateValidator();

        var exception = Assert.Throws<RunbookDocumentValidationException>(
            () => validator.Validate("runbook", "folder/runbook.md", Encoding.UTF8.GetBytes("content")));

        Assert.Equal(RunbookDocumentValidationErrorCode.InvalidFileName, exception.ErrorCode);
    }

    [Fact]
    public void ConstructorForNonPositiveMaximumSizeThrowsArgumentOutOfRangeException()
    {
        var options = new RunbookDocumentOptions { MaxDocumentSizeBytes = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => new RunbookDocumentValidator(options));
    }

    private static RunbookDocumentValidator CreateValidator(long maxDocumentSizeBytes = 1_048_576)
    {
        return new(new RunbookDocumentOptions { MaxDocumentSizeBytes = maxDocumentSizeBytes });
    }
}
