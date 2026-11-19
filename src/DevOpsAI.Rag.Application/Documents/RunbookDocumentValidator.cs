using System.Text;
using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Domain.Documents;
using Microsoft.Extensions.Options;

namespace DevOpsAI.Rag.Application.Documents;

public sealed class RunbookDocumentValidator
{
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly long _maxDocumentSizeBytes;

    public RunbookDocumentValidator(IOptions<RunbookDocumentOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Value.MaxDocumentSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "The maximum document size must be greater than zero.");
        }

        _maxDocumentSizeBytes = options.Value.MaxDocumentSizeBytes;
    }

    public ValidatedRunbookDocument Validate(
        string documentId,
        string fileName,
        ReadOnlyMemory<byte> contentBytes)
    {
        string normalizedId;

        try
        {
            normalizedId = KnowledgeDocument.NormalizeId(documentId);
        }
        catch (ArgumentException)
        {
            throw new RunbookDocumentValidationException(
                RunbookDocumentValidationErrorCode.InvalidDocumentId);
        }

        string normalizedFileName;

        try
        {
            normalizedFileName = KnowledgeDocument.NormalizeFileName(fileName);
        }
        catch (ArgumentException)
        {
            throw new RunbookDocumentValidationException(
                RunbookDocumentValidationErrorCode.InvalidFileName);
        }

        var extension = Path.GetExtension(normalizedFileName);
        if (!extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            throw new RunbookDocumentValidationException(
                RunbookDocumentValidationErrorCode.UnsupportedExtension);
        }

        if (contentBytes.Length > _maxDocumentSizeBytes)
        {
            throw new RunbookDocumentValidationException(
                RunbookDocumentValidationErrorCode.DocumentTooLarge);
        }

        string content;

        try
        {
            content = StrictUtf8.GetString(contentBytes.Span);
        }
        catch (DecoderFallbackException)
        {
            throw new RunbookDocumentValidationException(
                RunbookDocumentValidationErrorCode.InvalidUtf8);
        }

        if (content.StartsWith('\uFEFF'))
        {
            content = content[1..];
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new RunbookDocumentValidationException(
                RunbookDocumentValidationErrorCode.EmptyContent);
        }

        return new ValidatedRunbookDocument(normalizedId, normalizedFileName, content);
    }
}
