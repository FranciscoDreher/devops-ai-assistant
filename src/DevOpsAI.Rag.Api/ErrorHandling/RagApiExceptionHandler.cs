using System.Diagnostics;
using System.Net;
using DevOpsAI.Rag.Api.Contracts;
using Microsoft.AspNetCore.Diagnostics;

namespace DevOpsAI.Rag.Api.ErrorHandling;

public sealed class RagApiExceptionHandler(ILogger<RagApiExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, string, string, Exception?> ApiFailure =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(1, nameof(ApiFailure)),
            "RAG API request failed with {ErrorCode}. TraceId: {TraceId}");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (httpContext.Response.HasStarted
            || (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested))
        {
            return false;
        }

        var (statusCode, code, message) = exception switch
        {
            HttpRequestException => (
                StatusCodes.Status503ServiceUnavailable,
                "DEPENDENCY_UNAVAILABLE",
                "A required dependency is temporarily unavailable."),
            TimeoutException or OperationCanceledException => (
                StatusCodes.Status503ServiceUnavailable,
                "DEPENDENCY_TIMEOUT",
                "A required dependency did not respond in time."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "INTERNAL_ERROR",
                "The request could not be completed due to a technical error.")
        };

        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
        ApiFailure(logger, code, traceId, null);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ApiErrorResponse(code, message, traceId),
            cancellationToken).ConfigureAwait(false);

        return true;
    }
}
