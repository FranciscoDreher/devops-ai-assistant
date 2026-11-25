using System.Diagnostics;
using DevOpsAI.Rag.Api.Contracts;
using DevOpsAI.Rag.Application.Configuration;
using DevOpsAI.Rag.Application.Documents;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace DevOpsAI.Rag.Api.Endpoints;

public static class DocumentEndpoints
{
    private const long MultipartOverheadLimitBytes = 65_536;

    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPut("/api/documents/{documentId}", PutDocumentAsync)
            .WithName("ReplaceRunbookDocument")
            .WithSummary("Add or replace a runbook document")
            .WithDescription("Accepts one Markdown or TXT file in the multipart/form-data 'file' field.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<DocumentResponse>(StatusCodes.Status200OK)
            .Produces<DocumentResponse>(StatusCodes.Status201Created)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status413RequestEntityTooLarge)
            .Produces<ApiErrorResponse>(StatusCodes.Status415UnsupportedMediaType)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/api/documents", ListDocumentsAsync)
            .WithName("ListRunbookDocuments")
            .WithSummary("List runbook document metadata")
            .WithDescription("Returns document status and metadata without document content.")
            .Produces<DocumentListResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        endpoints.MapDelete("/api/documents/{documentId}", DeleteDocumentAsync)
            .WithName("DeleteRunbookDocument")
            .WithSummary("Delete a runbook document")
            .WithDescription("Deletes the document and its searchable data.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
            .Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<Results<
        Created<DocumentResponse>,
        Ok<DocumentResponse>,
        JsonHttpResult<ApiErrorResponse>>> PutDocumentAsync(
        HttpContext context,
        string documentId,
        HttpRequest request,
        ReplaceRunbookDocument replaceDocument,
        IOptions<RunbookDocumentOptions> documentOptions,
        CancellationToken cancellationToken)
    {
        if (request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) != true)
        {
            return Error(context, "INVALID_REQUEST", "A multipart document upload is required.", StatusCodes.Status400BadRequest);
        }

        var maximumRequestSize = documentOptions.Value.MaxDocumentSizeBytes > long.MaxValue - MultipartOverheadLimitBytes
            ? long.MaxValue
            : documentOptions.Value.MaxDocumentSizeBytes + MultipartOverheadLimitBytes;
        var requestSizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (requestSizeFeature is { IsReadOnly: false })
        {
            requestSizeFeature.MaxRequestBodySize = maximumRequestSize;
        }

        if (request.ContentLength > maximumRequestSize)
        {
            return Error(context, "DOCUMENT_TOO_LARGE", "The document exceeds the configured size limit.", StatusCodes.Status413RequestEntityTooLarge);
        }

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException)
        {
            return Error(context, "INVALID_REQUEST", "The multipart document upload is invalid.", StatusCodes.Status400BadRequest);
        }

        var file = form.Files.GetFile("file");
        if (form.Files.Count != 1 || file is null)
        {
            return Error(context, "INVALID_REQUEST", "Exactly one file field named 'file' is required.", StatusCodes.Status400BadRequest);
        }

        if (file.Length > documentOptions.Value.MaxDocumentSizeBytes || file.Length > int.MaxValue)
        {
            return Error(context, "DOCUMENT_TOO_LARGE", "The document exceeds the configured size limit.", StatusCodes.Status413RequestEntityTooLarge);
        }

        using var contentStream = new MemoryStream((int)file.Length);
        await file.CopyToAsync(contentStream, cancellationToken).ConfigureAwait(false);

        var result = await replaceDocument.ExecuteAsync(
            new ReplaceRunbookDocumentRequest(documentId, file.FileName, contentStream.ToArray()),
            cancellationToken).ConfigureAwait(false);

        if (result.Status == RunbookDocumentOperationStatus.ValidationError)
        {
            if (result.ValidationError is not { } validationError)
            {
                throw new InvalidOperationException("The document validation result did not contain an error code.");
            }

            return ValidationError(context, validationError);
        }

        if (result.Document is not { } resultDocument)
        {
            throw new InvalidOperationException("The document replacement did not return document metadata.");
        }

        var document = MapDocument(resultDocument);
        return result.Status switch
        {
            RunbookDocumentOperationStatus.Created => TypedResults.Created(
                $"/api/documents/{Uri.EscapeDataString(document.DocumentId)}",
                document),
            RunbookDocumentOperationStatus.Replaced => TypedResults.Ok(document),
            _ => throw new InvalidOperationException("The document replacement returned an unsupported result.")
        };
    }

    private static async Task<Ok<DocumentListResponse>> ListDocumentsAsync(
        ListRunbookDocuments listDocuments,
        CancellationToken cancellationToken)
    {
        var result = await listDocuments.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        var documents = result.Documents.Select(MapDocument).ToArray();
        return TypedResults.Ok(new DocumentListResponse(documents));
    }

    private static async Task<Results<
        NoContent,
        JsonHttpResult<ApiErrorResponse>>> DeleteDocumentAsync(
        HttpContext context,
        string documentId,
        DeleteRunbookDocument deleteDocument,
        CancellationToken cancellationToken)
    {
        var result = await deleteDocument.ExecuteAsync(
            new DeleteRunbookDocumentRequest(documentId),
            cancellationToken).ConfigureAwait(false);

        return result.Status switch
        {
            RunbookDocumentOperationStatus.Deleted => TypedResults.NoContent(),
            RunbookDocumentOperationStatus.NotFound => Error(
                context,
                "DOCUMENT_NOT_FOUND",
                "The requested document was not found.",
                StatusCodes.Status404NotFound),
            RunbookDocumentOperationStatus.ValidationError => result.ValidationError is { } validationError
                ? ValidationError(context, validationError)
                : throw new InvalidOperationException("The document validation result did not contain an error code."),
            _ => throw new InvalidOperationException("The document deletion returned an unsupported result.")
        };
    }

    private static DocumentResponse MapDocument(RunbookDocumentSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var status = summary.Status.ToString() switch
        {
            "Active" => "indexed",
            "Deleted" => "deleted",
            _ => throw new InvalidOperationException("The document status is not supported by the API.")
        };

        return new DocumentResponse(
            summary.DocumentId,
            summary.FileName,
            summary.ContentHash,
            summary.Version,
            status,
            summary.ChunkCount,
            summary.CreatedAtUtc,
            summary.UpdatedAtUtc,
            summary.DeletedAtUtc);
    }

    private static JsonHttpResult<ApiErrorResponse> ValidationError(
        HttpContext context,
        RunbookDocumentValidationErrorCode errorCode)
    {
        return errorCode switch
        {
            RunbookDocumentValidationErrorCode.UnsupportedExtension => Error(
                context,
                "UNSUPPORTED_DOCUMENT_FORMAT",
                "Only Markdown and TXT documents are supported.",
                StatusCodes.Status415UnsupportedMediaType),
            RunbookDocumentValidationErrorCode.DocumentTooLarge => Error(
                context,
                "DOCUMENT_TOO_LARGE",
                "The document exceeds the configured size limit.",
                StatusCodes.Status413RequestEntityTooLarge),
            RunbookDocumentValidationErrorCode.InvalidUtf8 or RunbookDocumentValidationErrorCode.EmptyContent => Error(
                context,
                "INVALID_DOCUMENT_CONTENT",
                "The document content is empty or is not valid UTF-8.",
                StatusCodes.Status400BadRequest),
            _ => Error(
                context,
                "VALIDATION_ERROR",
                "The document identifier or filename is invalid.",
                StatusCodes.Status400BadRequest)
        };
    }

    private static JsonHttpResult<ApiErrorResponse> Error(
        HttpContext context,
        string code,
        string message,
        int statusCode)
    {
        return TypedResults.Json(
            new ApiErrorResponse(code, message, GetTraceId(context)),
            statusCode: statusCode);
    }

    private static string GetTraceId(HttpContext context)
    {
        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }
}
