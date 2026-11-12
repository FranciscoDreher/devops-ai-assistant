using System.Security.Cryptography;
using System.Text;

namespace DevOpsAI.Rag.Domain.Documents;

public sealed class KnowledgeDocument
{
    private KnowledgeDocument(
        string id,
        string fileName,
        string content,
        int version,
        string contentHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? deletedAtUtc)
    {
        Id = id;
        FileName = fileName;
        Content = content;
        Version = version;
        ContentHash = contentHash;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        DeletedAtUtc = deletedAtUtc;
    }

    public string Id { get; }

    public string FileName { get; }

    public string Content { get; }

    public string ContentHash { get; }

    public int Version { get; }

    public KnowledgeDocumentStatus Status => DeletedAtUtc is null
        ? KnowledgeDocumentStatus.Active
        : KnowledgeDocumentStatus.Deleted;

    public DateTimeOffset CreatedAtUtc { get; }

    public DateTimeOffset UpdatedAtUtc { get; }

    public DateTimeOffset? DeletedAtUtc { get; }

    public static KnowledgeDocument Create(
        string id,
        string fileName,
        string content,
        DateTimeOffset createdAtUtc)
    {
        var normalizedId = ValidateId(id);
        var normalizedFileName = ValidateFileName(fileName);
        ValidateContent(content, nameof(content));
        var timestamp = createdAtUtc.ToUniversalTime();

        return new KnowledgeDocument(
            normalizedId,
            normalizedFileName,
            content,
            version: 1,
            ComputeContentHash(content),
            timestamp,
            timestamp,
            deletedAtUtc: null);
    }

    public KnowledgeDocument Replace(string fileName, string content, DateTimeOffset updatedAtUtc)
    {
        var normalizedFileName = ValidateFileName(fileName);
        ValidateContent(content, nameof(content));
        var timestamp = updatedAtUtc.ToUniversalTime();

        if (timestamp < UpdatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updatedAtUtc),
                "The replacement timestamp cannot precede the document's latest state.");
        }

        return new KnowledgeDocument(
            Id,
            normalizedFileName,
            content,
            checked(Version + 1),
            ComputeContentHash(content),
            CreatedAtUtc,
            timestamp,
            deletedAtUtc: null);
    }

    public KnowledgeDocument Delete(DateTimeOffset deletedAtUtc)
    {
        if (DeletedAtUtc is not null)
        {
            throw new InvalidOperationException("The document is already deleted.");
        }

        var timestamp = deletedAtUtc.ToUniversalTime();

        if (timestamp < UpdatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deletedAtUtc),
                "The deletion timestamp cannot precede the document's latest state.");
        }

        return new KnowledgeDocument(
            Id,
            FileName,
            Content,
            Version,
            ContentHash,
            CreatedAtUtc,
            timestamp,
            timestamp);
    }

    private static string ValidateId(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var normalized = id.Trim();

        if (normalized is "." or ".."
            || normalized.Contains('/') || normalized.Contains('\\')
            || normalized.Any(char.IsControl))
        {
            throw new ArgumentException("The document ID must be a non-path identifier.", nameof(id));
        }

        return normalized;
    }

    private static string ValidateFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var normalized = fileName.Trim();

        if (normalized is "." or ".."
            || normalized.Contains('/') || normalized.Contains('\\')
            || normalized.Any(char.IsControl))
        {
            throw new ArgumentException("The filename must not contain a path.", nameof(fileName));
        }

        return normalized;
    }

    private static void ValidateContent(string content, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, parameterName);
    }

    private static string ComputeContentHash(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }
}
